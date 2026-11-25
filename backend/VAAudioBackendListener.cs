// Human-created

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
            // Map from godot-space to AL/FMOD space
            var forward = new Vector3(Mathf.Sin(value), -Mathf.Cos(value), 0);
            var up = new Vector3(0, 0, -1);

            Backend?.SetListenerOrientation(forward, up);
        }
    }
}
