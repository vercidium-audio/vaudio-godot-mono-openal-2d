namespace vaudio_godot_mono_openal;

/// <summary>
/// Renders an emitter's <see cref="vaudio.Emitter.VisualisationCallback"/> ray-bounce results as
/// fading diamond sprites in the 2D canvas. Add it as a direct child of a <see cref="VAEmitter"/>,
/// or of a <see cref="VARaytracedSource"/> such as <see cref="VASource"/>.
///
/// Adding this node while the game is already running only runs its script in the editor's own
/// tool-script preview context - Godot Mono does not instantiate a new C# node inside the live
/// running game process (upstream Godot C# hot-reload limitation). Restart the game to see a
/// newly-added VAVisualisation render.
/// </summary>
[Tool]
[GlobalClass]
public partial class VAVisualisation : Node2D
{
    const string ShaderCode = """
        shader_type canvas_item;
        render_mode blend_mix, unshaded;

        uniform float current_time;
        uniform float fade_in_ms;
        uniform float fade_out_ms;
        uniform float duration_ms;
        uniform vec4 base_color : source_color;

        varying float spawn_time;

        void vertex() {
            spawn_time = INSTANCE_CUSTOM.x;
        }

        void fragment() {
            float elapsed_ms = (current_time - spawn_time) * 1000.0;
            if (elapsed_ms < 0.0 || elapsed_ms > duration_ms) discard;

            float fade_in = fade_in_ms > 0.0 ? clamp(elapsed_ms / fade_in_ms, 0.0, 1.0) : 1.0;
            float fade_out = fade_out_ms > 0.0 ? clamp((duration_ms - elapsed_ms) / fade_out_ms, 0.0, 1.0) : 1.0;

            COLOR.rgb = base_color.rgb;
            COLOR.a = base_color.a * min(fade_in, fade_out);
        }
        """;

    // The VAEmitter node whose vaudio.Emitter this node drives + renders. Either this node's direct
    // VAEmitter parent, or the internal emitter of a VARaytracedSource parent (VASource etc.).
    VAEmitter emitter;

    MultiMesh multimesh;
    MultiMeshInstance2D multimeshInstance;
    ShaderMaterial shaderMaterial;

    // Ring-buffer write cursor into multimesh's instances - each callback invocation appends
    // RayCount * BounceCount new instances here, wrapping once the buffer is full. The buffer is
    // sized (see RequiredInstanceCount) to hold every batch still fading at once, so by the time
    // the cursor wraps back around, that instance has already faded out.
    int nextInstance = 0;

    bool waitingForParent = false;

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint())
            return;

        FindEmitter();

        if (emitter == null)
        {
            waitingForParent = true;
            GetTree().NodeAdded += RetryFindEmitter;
        }
    }

    public override void _ExitTree()
    {
        if (waitingForParent)
        {
            GetTree().NodeAdded -= RetryFindEmitter;
            waitingForParent = false;
        }

        if (emitter?.emitter != null)
            emitter.emitter.VisualisationCallback = null;

        emitter = null;
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        CreateMultimesh();
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        shaderMaterial?.SetShaderParameter("current_time", Time.GetTicksMsec() / 1000.0);
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (GetParent() is not VAEmitter and not VARaytracedSource)
            return ["This node must be a direct child of a VAEmitter (or a VARaytracedSource such as VASource) to render its visualisation rays."];

        return [];
    }

    void FindEmitter()
    {
        var candidate = GetParent() switch
        {
            VAEmitter parentEmitter => parentEmitter,
            VARaytracedSource parentSource => parentSource.RaytracedEmitter,
            _ => null,
        };

        if (candidate?.emitter == null)
        {
            emitter = null;
            return;
        }

        emitter = candidate;

        ApplyPropertiesToEmitter();

        emitter.emitter.VisualisationCallback = OnVisualisationData;
    }

    void RetryFindEmitter(Node node)
    {
        FindEmitter();

        if (emitter == null)
        {
            if (GetParent() is not VAEmitter and not VARaytracedSource)
                LogWarning($"[vaudio-godot-mono-openal-2d] {Name} must be a direct child of a VAEmitter or VASource to render its visualisation rays.");

            return;
        }

        GetTree().NodeAdded -= RetryFindEmitter;
        waitingForParent = false;
    }

    void ApplyPropertiesToEmitter()
    {
        emitter.emitter.VisualisationRayCount = _RayCount;
        emitter.emitter.VisualisationBounceCount = _BounceCount;
        emitter.emitter.VisualisationUpdateFrequency = _UpdateFrequencyMs;
    }

    // Room for every batch still fading at once, not just the latest one, or a new callback's
    // writes stomp on still-visible instances from a few callbacks ago.
    int RequiredInstanceCount()
    {
        int batchSize = Math.Max(1, _RayCount * _BounceCount);
        int batchesInFlight = (_DurationMs / Math.Max(1, _UpdateFrequencyMs)) + 2;

        return batchSize * batchesInFlight;
    }

    static ArrayMesh BuildDiamondMesh()
    {
        // Unit diamond in the canvas XY plane, +Y is "up" - rotated per-instance to point its up
        // axis along the ray hit normal (see OnVisualisationData).
        Vector3 top = new(0, 1, 0);
        Vector3 right = new(1, 0, 0);
        Vector3 bottom = new(0, -1, 0);
        Vector3 left = new(-1, 0, 0);

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        st.AddVertex(top);
        st.AddVertex(right);
        st.AddVertex(bottom);

        st.AddVertex(top);
        st.AddVertex(bottom);
        st.AddVertex(left);

        return st.Commit();
    }

    void CreateMultimesh()
    {
        var shader = new Shader { Code = ShaderCode };

        shaderMaterial = new ShaderMaterial { Shader = shader };
        ApplyShaderUniforms();

        multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseCustomData = true,
            Mesh = BuildDiamondMesh(),
            InstanceCount = RequiredInstanceCount(),
            VisibleInstanceCount = 0,
        };

        multimeshInstance = new MultiMeshInstance2D
        {
            Multimesh = multimesh,
            Material = shaderMaterial,

            // VisualisationData positions are already in world/canvas space - TopLevel makes this
            // node's own transform (left at identity) the effective global transform, so those
            // positions can be written straight into instance transforms instead of being
            // re-relativised to a moving parent every callback.
            TopLevel = true,
            Transform = Transform2D.Identity,
        };

        AddChild(multimeshInstance);
    }

    void ApplyShaderUniforms()
    {
        if (shaderMaterial == null)
            return;

        shaderMaterial.SetShaderParameter("fade_in_ms", (float)_FadeInMs);
        shaderMaterial.SetShaderParameter("fade_out_ms", (float)_FadeOutMs);
        shaderMaterial.SetShaderParameter("duration_ms", (float)_DurationMs);
        shaderMaterial.SetShaderParameter("base_color", _Color);
    }

    // Called from vaudio's main-thread update - writes one MultiMesh instance per ray bounce,
    // wrapping the ring-buffer cursor rather than doing any per-instance cleanup.
    void OnVisualisationData(vaudio.VisualisationData[] data)
    {
        if (multimesh == null || data.Length == 0)
            return;

        int capacity = multimesh.InstanceCount;
        int needed = Math.Max(data.Length, RequiredInstanceCount());

        if (needed > capacity)
        {
            capacity = needed;
            multimesh.InstanceCount = capacity;
            nextInstance = 0;
        }

        double now = Time.GetTicksMsec() / 1000.0;
        Vector2 emitterPosition = emitter?.GlobalPosition ?? Vector2.Zero;
        float maxDistanceSquared = _MaxDistance * _MaxDistance;

        for (int i = 0; i < data.Length; i++)
        {
            Vector2 position = FromVAudio(data[i].position);

            // Skip bounces too far from the emitter to be worth drawing.
            if (_MaxDistance > 0.0f && position.DistanceSquaredTo(emitterPosition) > maxDistanceSquared)
                continue;

            Vector2 normal = FromVAudio(data[i].normal);
            normal = normal.LengthSquared() > 0.00001f ? normal.Normalized() : Vector2.Up;

            // Rotate the diamond's +Y up axis onto the hit normal, then scale to Size.
            float angle = normal.Angle() - Mathf.Pi / 2f;
            var transform = new Transform2D(angle, Vector2.One * _Size, 0f, position + normal * _NormalOffset);

            multimesh.SetInstanceTransform2D(nextInstance, transform);
            multimesh.SetInstanceCustomData(nextInstance, new Color((float)now, 0, 0, 0));

            nextInstance = (nextInstance + 1) % capacity;
        }

        multimesh.VisibleInstanceCount = capacity;
    }

    [ExportGroup("Ray Casting")]

    int _RayCount = 32;
    /// <summary>Number of visualisation rays cast</summary>
    [Export]
    public int RayCount
    {
        get => _RayCount;
        set
        {
            _RayCount = Math.Max(0, value);

            if (emitter?.emitter != null)
                emitter.emitter.VisualisationRayCount = _RayCount;
        }
    }

    int _BounceCount = 4;
    /// <summary>Number of times each visualisation ray bounces</summary>
    [Export]
    public int BounceCount
    {
        get => _BounceCount;
        set
        {
            _BounceCount = Math.Max(0, value);

            if (emitter?.emitter != null)
                emitter.emitter.VisualisationBounceCount = _BounceCount;
        }
    }

    int _UpdateFrequencyMs = 500;
    /// <summary>How often - in milliseconds - to cast visualisation rays</summary>
    [Export]
    public int UpdateFrequencyMs
    {
        get => _UpdateFrequencyMs;
        set
        {
            _UpdateFrequencyMs = Math.Max(1, value);

            if (emitter?.emitter != null)
                emitter.emitter.VisualisationUpdateFrequency = _UpdateFrequencyMs;
        }
    }

    [ExportGroup("Appearance")]

    int _FadeInMs = 100;
    /// <summary>How long - in milliseconds - each diamond takes to fade in</summary>
    [Export]
    public int FadeInMs
    {
        get => _FadeInMs;
        set
        {
            _FadeInMs = Math.Max(0, value);
            ApplyShaderUniforms();
        }
    }

    int _FadeOutMs = 400;
    /// <summary>How long - in milliseconds - each diamond takes to fade out</summary>
    [Export]
    public int FadeOutMs
    {
        get => _FadeOutMs;
        set
        {
            _FadeOutMs = Math.Max(0, value);
            ApplyShaderUniforms();
        }
    }

    int _DurationMs = 1500;
    /// <summary>Total lifetime - in milliseconds - of each diamond, including fade in/out</summary>
    [Export]
    public int DurationMs
    {
        get => _DurationMs;
        set
        {
            _DurationMs = Math.Max(0, value);
            ApplyShaderUniforms();
        }
    }

    Color _Color = new(0.11f, 0.97f, 1.0f, 0.75f);
    /// <summary>RGB is the diamond colour, A is the maximum opacity once faded in</summary>
    [Export]
    public Color Color
    {
        get => _Color;
        set
        {
            _Color = value;
            ApplyShaderUniforms();
        }
    }

    float _Size = 6.0f;
    /// <summary>Size of each diamond, in canvas pixels. Only affects future instance writes, not retroactive</summary>
    [Export(PropertyHint.Range, "0.5,200.0,0.5,or_greater")]
    public float Size
    {
        get => _Size;
        set => _Size = Math.Max(0.5f, value);
    }

    float _NormalOffset = 1.0f;
    /// <summary>Canvas pixels each diamond is pushed along its hit normal, off the surface it landed on</summary>
    [Export(PropertyHint.Range, "0.0,32.0,0.1,or_greater")]
    public float NormalOffset
    {
        get => _NormalOffset;
        set => _NormalOffset = Math.Max(0.0f, value);
    }

    float _MaxDistance = 2000.0f;
    /// <summary>Maximum distance from the parent emitter a ray bounce can be to be rendered. 0 = unlimited</summary>
    [Export(PropertyHint.Range, "0.0,20000.0,1.0,or_greater")]
    public float MaxDistance
    {
        get => _MaxDistance;
        set => _MaxDistance = Math.Max(0.0f, value);
    }
}
