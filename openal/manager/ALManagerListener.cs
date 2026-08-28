namespace vaudio_godot_mono_openal;

// 2D listener state for ALManager - position vector plus a single rotation angle. Runtime-API-only,
// driven from VAWorldGodot each frame. Shared body is in common/openal/manager/.
// OpenAL is a 3D API: the 2D world maps onto its XY plane, listener up is a constant +Z.
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
        var forward = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));

        AL.Listenerfv(AL.AL_ORIENTATION, [forward.X, forward.Y, 0, 0, 0, 1]);
    }
}
