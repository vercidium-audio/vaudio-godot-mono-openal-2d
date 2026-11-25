// Human-reviewed

namespace vaudio_godot_mono_openal;

public static partial class VAAudioBackend
{
    public static Vector2 ListenerPosition
    {
        set => Backend?.SetListenerPosition(new Vector3(value.X, value.Y, 0));
    }

    public static float ListenerRotation
    {
        set
        {
            // Top-down: rotation 0 faces screen-up. Up is -Z because Godot's Y-down XY plane is mirrored relative to the backends' right-handed space.
            var forward = new Vector3(Mathf.Sin(value), -Mathf.Cos(value), 0);

            Backend?.SetListenerOrientation(forward, new Vector3(0, 0, -1));
        }
    }
}
