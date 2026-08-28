namespace vaudio_godot_mono_openal;

// 2D has no EditorNode3DGizmoPlugin equivalent - the world bounds are drawn straight into the
// canvas by VAWorld's own _Draw() (queued via QueueRedraw() from the Size/BoundsColor setters and
// the transform-changed notification). Editor-only; the running game never draws it.
//
// The bounds rect is always axis-aligned starting at the node's position with rotation/scale
// hidden in the Inspector (see VAWorldProperties._ValidateProperty), so in local space it's simply
// [0, Size].
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
