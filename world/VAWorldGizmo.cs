namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    public override void _Draw()
    {
        if (!Engine.IsEditorHint())
            return;

        var rect = new Rect2(Vector2.Zero, Size);

        DrawRect(rect, BoundsColor, filled: true);
        DrawRect(rect, new Color(BoundsColor, 1f), filled: false, width: 2f);
    }
}
