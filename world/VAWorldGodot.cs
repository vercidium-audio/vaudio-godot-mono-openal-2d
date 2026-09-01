namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    public bool Initialised => world != null;

    public override void _EnterTree()
    {
        SetNotifyTransform(true);

        if (Engine.IsEditorHint())
            return;

        AudioManager.Ensure();

        // Cache the scene root since we access it often
        SceneRoot = GetTree().CurrentScene as Node2D;

        world = new();

        // Godot's 2D canvas is Y-down; tell the debug window so its render + viewport sync match.
        world.CoordinateSystem = vaudio.CoordinateSystem.Godot2D;

        world.LogCallback = Log;
        world.Position = ToVAudio(Position);
        world.Size = ToVAudio(Size);
        world.Epsilon = Epsilon;
        world.WorldIsIndoors = WorldIsIndoors;

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

        // Threading
        // 0 maps to processor count - 1, matching the native plugin's behaviour
        world.MaximumConcurrencyLevel = MaximumConcurrencyLevel == 0 ? vaudio.ThreadStatistics.BackgroundThreadCount : MaximumConcurrencyLevel;
        world.WorkItemCount = WorkItemCount;

        world.RenderingEnabled = RenderingEnabled;


        world.AirAbsorption.Humidity = Humidity;
        world.AirAbsorption.Temperature = Temperature;
        world.AirAbsorption.Pressure = Pressure;

        // Create reverb effects
        OnDeviceRecreated();

        if (!AudioManager.Initialised)
        {
            LogError("The godot-mono-openal addon is not enabled. Ensure godot-mono-openal is enabled in Project Settings > Plugins (try toggling it off and on if it's already enabled)");
        }

        // Register for device destroyed/recreated callbacks to clean up and recreate reverb effects
        RegisterDeviceRecreatedCallback(OnDeviceRecreated);
        RegisterDeviceDestroyedCallback(OnDeviceDestroyed);

        // Wait a frame for the scene to be fully loaded
        CallDeferred(nameof(InitializeScene));

        RegisterDebuggerCapture();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged)
            return;

        if (Rotation != 0f)
            Rotation = 0f;

        // Redraw the bounds gizmo whenever the node moves, whether from the viewport gizmo,
        // the Inspector's Position field, or code.
        QueueRedraw();

        if (world != null)
            world.Position = ToVAudio(Position);
    }

    void OnDeviceRecreated()
    {
        // Recreate the reverb slots after the device is recreated
        listenerReverbSlot = AudioManager.Backend.CreateReverbSlot();
    }

    void OnDeviceDestroyed()
    {
        // Delete all reverb slots / filters - they contain backend resources that are now invalid
        ambientFilter?.Delete();
        ambientFilter = null;

        listenerReverbSlot?.Dispose();
        listenerReverbSlot = null;

        foreach (var slot in groupedReverbSlots)
            slot.Dispose();

        groupedReverbSlots.Clear();
        groupedReverbEffects.Clear();
    }

    void InitializeScene()
    {
        // SceneRoot can be null if this node isn't under CurrentScene (e.g. added as a sibling autoload) -
        // scan from the tree root instead so primitives already baked into the scene aren't missed.
        Node root = GetTree()?.Root;

        if (root == null)
            return;

        foreach (var child in root.GetChildren())
            AddPrimitive(child, vaudio.MaterialType.Air, true);

        // Listen for scene tree changes
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().NodeRemoved += OnNodeRemoved;
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
            return;

        AudioManager.UnregisterDeviceDestroyedCallback(OnDeviceDestroyed);
        AudioManager.UnregisterDeviceRecreatedCallback(OnDeviceRecreated);

        GetTree().NodeAdded -= OnNodeAdded;
        GetTree().NodeRemoved -= OnNodeRemoved;

        UnregisterDebuggerCapture();

        // Remove vercidium_audio_* metadata fields from all nodes in the scene.
        // SceneRoot can be null if the tree has no scene loaded (e.g. exiting during shutdown).
        if (SceneRoot != null)
            RemovePrimitive(SceneRoot, true);

        world?.Dispose();
    }

    // This fires for the new parent node AND each of its child nodes separately
    //  Parent node is invoked first
    void OnNodeAdded(Node node) => AddPrimitive(node, vaudio.MaterialType.Air, false);

    // This fires for the new parent node AND each of its child nodes separately
    //  Child nodes are invoked first
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
        else if (AudioManager.Initialised)
        {
            // Sync the backend's listener to our main listener. VAWorld owns the 2D rotation ->
            // forward/up conversion so the backend interface stays dimension-agnostic.
            float rotation = listener.GlobalRotation;
            var forward = new Vector3(Mathf.Cos(rotation), Mathf.Sin(rotation), 0);

            AudioManager.Backend.SetListenerPosition(new Vector3(listener.GlobalPosition.X, listener.GlobalPosition.Y, 0));
            AudioManager.Backend.SetListenerOrientation(forward, new Vector3(0, 0, 1));
        }

        // The backend has no Node._Process of its own - VAWorld drives its tick the same way it
        // already drives world.Update() for the raytracer.
        AudioManager.Update();

        world.Update();
    }

}
