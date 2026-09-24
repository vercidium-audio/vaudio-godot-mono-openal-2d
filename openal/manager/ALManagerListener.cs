namespace vaudio_godot_mono_openal;

public static unsafe partial class ALManager
{
    static Vector2 _listenerPosition;
    static float _listenerRotation;

    public static Vector2 ListenerPosition
    {
        get => _listenerPosition;
        set => UpdateProperty(ref _listenerPosition, value, SetListenerPosition);
    }

    public static float ListenerRotation
    {
        get => _listenerRotation;
        set => UpdateProperty(ref _listenerRotation, value, SetListenerRotation);
    }

    static void SetListenerPosition(Vector2 position) => AL.Listenerfv(AL.AL_POSITION, [position.X, position.Y, 0]);

    static void SetListenerRotation(float rotation)
    {
        // Top-down: rotation 0 faces screen-up. Up is -Z because Godot's Y-down XY plane is mirrored relative to OpenAL's right-handed space.
        var forward = new Vector2(Mathf.Sin(rotation), -Mathf.Cos(rotation));

        AL.Listenerfv(AL.AL_ORIENTATION, [forward.X, forward.Y, 0, 0, 0, -1]);
    }
}
