using OpenALSource = global::OpenAL.managed.ALSource;

namespace vaudio_godot_mono_openal;

// A sound source relative to the listener with zero offset, e.g. footsteps, ambience, music.
// Always AL_SOURCE_RELATIVE with a pinned origin position.
[Tool]
public partial class ALSourceRelative : ALSource
{
    protected override void ConfigureSource(OpenALSource source)
    {
        source.SetRelative(true);
        source.SetPosition(Vector2.Zero);
    }
}
