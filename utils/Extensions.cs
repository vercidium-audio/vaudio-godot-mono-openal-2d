global using static vaudio_godot_mono_openal.Extensions;
global using static vaudio_godot_mono_openal.GlobalHelpers;

namespace vaudio_godot_mono_openal;

internal static class Extensions
{
    public static vaudio.Color ToVAudio(Godot.Color c) => new(c.R, c.G, c.B, c.A);

    public static vaudio.Vector ToVAudio(Vector2 v) => new(v.X, v.Y);
    public static Vector2 FromVAudio(vaudio.Vector v) => new(v.X, v.Y);

    public static void RegisterDeviceRecreatedCallback(Action callback) => ALManager.RegisterDeviceRecreatedCallback(callback);
    public static void RegisterDeviceDestroyedCallback(Action callback) => ALManager.RegisterDeviceDestroyedCallback(callback);
}
