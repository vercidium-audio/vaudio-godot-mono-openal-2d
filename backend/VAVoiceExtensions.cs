namespace vaudio_godot_mono_openal;

// The backends are 3D, so 2D positions map onto the XY plane with Z left at 0
public static class VAVoiceExtensions
{
    public static void SetPosition(this VAVoice voice, Vector2 position) => voice.SetPosition(new Vector3(position.X, position.Y, 0));
}
