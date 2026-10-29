namespace vaudio_godot_mono_openal;

[Tool]
public partial class VAWorld : Node2D
{
    public static void RefreshOutputDeviceList() => ALManager.RefreshDeviceLists();

    // ALManager is a plain C# class, so expose its HRTF functions to GDScript here
    public static string[] GetAvailableHrtfs() => ALManager.GetAvailableHrtfs().ToArray();
    public static bool SetHrtf(string name) => ALManager.SetHrtf(name);
    public static string GetHrtf() => ALManager.GetHrtf();
    public static string GetActiveHrtf() => ALManager.GetActiveHrtf();
    public static bool SetHrtfEnabled(bool value) => ALManager.SetHrtfEnabled(value);
    public static bool GetHrtfEnabled() => ALManager.GetHrtfEnabled();
}
