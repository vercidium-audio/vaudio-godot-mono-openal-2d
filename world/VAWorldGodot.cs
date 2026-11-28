// Human-created

namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    public bool Initialised => world != null;

    public override void _EnterTree()
    {
        SetNotifyTransform(true);

        // Remove rotation/scale (world bounds is always axis aligned)
        NormalizeTransform();
        ReclampBoundsSize();

        if (Engine.IsEditorHint())
            return;

        VAAudioBackend.Ensure();
        ApplyBackendSettings();

        // Cache the scene root for quick access
        SceneRoot = GetTree().CurrentScene as Node2D;

        world = new();

        world.CoordinateSystem = vaudio.CoordinateSystem.Godot2D;
        world.Name = ResolvedWorldName;
        world.LogCallback = LogSDK;

        // Set world pos and size
        boundsPosition = Position;
        PlaceBounds();
        world.Size = ToVAudio(Size);
        world.Epsilon = Epsilon;

        // Reverb
        world.MaximumGroupedEAXCount = MaximumGroupedEAXCount;
        world.OnReverbUpdated = OnReverbUpdated;

        // Air absorption
        world.MetersPerUnit = MetersPerUnit;
        world.InverseSpeedOfSound = 1.0f / SpeedOfSound;
        world.ReferenceFrequencyLF = ReferenceFrequencyLF;
        world.ReferenceFrequencyHF = ReferenceFrequencyHF;

        // Emitters
        world.EmittersOutsideTheWorldAreMuffled = EmittersOutsideTheWorldAreMuffled;
        world.OcclusionRaysLoseEnergyFromWorldBounds = OcclusionRaysLoseEnergyFromWorldBounds;

        // Threading
        // 0 maps to processor count - 1, matching the native plugin's behaviour
        world.MaximumConcurrencyLevel = MaximumConcurrencyLevel == 0 ? vaudio.ThreadStatistics.BackgroundThreadCount : MaximumConcurrencyLevel;
        world.WorkItemCount = WorkItemCount;

        // Re-run the setter now that the world exists
        RenderingEnabled = _RenderingEnabled;
        world.VisualisationScale = _VisualisationScale;

        world.AirAbsorption.Humidity = Humidity;
        world.AirAbsorption.Temperature = Temperature;
        world.AirAbsorption.Pressure = Pressure;

        // Create reverb effects
        OnDeviceRecreated();

        RegisterDeviceRecreatedCallback(OnDeviceRecreated);
        RegisterDeviceDestroyedCallback(OnDeviceDestroyed);

        // Wait for the scene to initialise before adding primitives
        CallDeferred(nameof(InitializeSceneDeferred));

        RegisterDebuggerCapture();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged)
            return;

        NormalizeTransform();

        // 2D Godot 2D only sends transform notifications after reading the global transform
        GetGlobalTransform();

        // Redraw the bounds gizmo
        QueueRedraw();

        if (world != null && _BoundsFollow != VABoundsFollow.Listener)
            PlaceBounds();
    }

    void NormalizeTransform()
    {
        if (Rotation != 0f)
            Rotation = 0f;

        if (Scale != Vector2.One)
            Scale = Vector2.One;

        if (Skew != 0f)
            Skew = 0f;
    }

    bool listeningToTree;

    void InitializeSceneDeferred()
    {
        // This is a deferred call, so ensure we're still in the tree
        if (!IsInsideTree())
            return;

        // Scan from the tree root so all primitives are included
        Node root = GetTree().Root;

        foreach (var child in root.GetChildren())
            AddPrimitive(child, vaudio.MaterialType.Air, true);

        // Listen for scene tree changes
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().NodeRemoved += OnNodeRemoved;
        listeningToTree = true;
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
            return;

        VAAudioBackend.UnregisterDeviceDestroyedCallback(OnDeviceDestroyed);
        VAAudioBackend.UnregisterDeviceRecreatedCallback(OnDeviceRecreated);

        if (listeningToTree)
        {
            GetTree().NodeAdded -= OnNodeAdded;
            GetTree().NodeRemoved -= OnNodeRemoved;
            listeningToTree = false;
        }

        UnregisterDebuggerCapture();

        // SceneRoot can be null if the tree has no scene loaded (e.g. exiting during shutdown).
        if (SceneRoot != null)
            RemovePrimitive(SceneRoot, true);

        world?.Dispose();
    }

    // This fires for the new parent node AND each of its child nodes separately
    // Note the PARENT node is always invoked first
    void OnNodeAdded(Node node)
    {
        ResolveInherited(node, out var material, out var useFlatTransmission, out var filter);
        AddPrimitive(node, material, useFlatTransmission, filter, false);
    }

    // This fires for the new parent node AND each of its child nodes separately
    // Note the CHILD nodes are always invoked first
    void OnNodeRemoved(Node node) => RemovePrimitive(node, false);

    internal bool NoListenerWarningLogged;

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (SyncViewport)
                SendViewportCameraToRunningGame();

            return;
        }

        if (listener == null)
        {
            if (!NoListenerWarningLogged)
            {
                LogWarning($"Node {Name} has no main listener, so reverb cannot be updated. Add a VAListener node to this scene");
                NoListenerWarningLogged = true;
            }
        }
        else
        {
            // Sync the OpenAL/FMOD listener to our main listener
            VAAudioBackend.ListenerPosition = listener.GlobalPosition;
            VAAudioBackend.ListenerRotation = listener.GlobalRotation;
        }

        FollowBounds();

        VAAudioBackend.Update();
        world.Update();
    }
}
