// Human-created

namespace vaudio_godot_mono_openal;

public static class VAVoiceExtensions
{
    public static void SetPosition(this VAVoice voice, Vector2 position) => voice.SetPosition(new Vector3(position.X, position.Y, 0));
}
