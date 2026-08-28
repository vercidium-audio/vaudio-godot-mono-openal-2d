using OpenALSource = global::OpenAL.managed.ALSource;

namespace vaudio_godot_mono_openal;

// OpenAL is always a 3D API - 2D vectors map onto the XY plane with Z left at 0.
public static class ALExtensions
{
    public static void SetPosition(this OpenALSource source, Vector2 v)  => AL.Sourcefv(source.ID, AL.AL_POSITION, [v.X, v.Y, 0]);
    public static void SetVelocity(this OpenALSource source, Vector2 v)  => AL.Sourcefv(source.ID, AL.AL_VELOCITY, [v.X, v.Y, 0]);
    public static void SetDirection(this OpenALSource source, Vector2 v) => AL.Sourcefv(source.ID, AL.AL_DIRECTION, [v.X, v.Y, 0]);
}
