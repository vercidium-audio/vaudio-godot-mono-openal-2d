namespace vaudio_godot_mono_openal;

public static class Conversions
{
    public static List<vaudio.Vector> ConvertPointsToVectorList(Vector2[] points)
    {
        List<vaudio.Vector> result = new(points.Length);

        foreach (var p in points)
            result.Add(ToVAudio(p));

        return result;
    }
}
