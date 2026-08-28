namespace vaudio_godot_mono_openal;

// VAEmitter (and its subclasses VAListener, and via VARaytracedSource: VASource, VASourceLeech,
// VAStreamSource, ...) has no visible representation of its own in the 2D editor, so draw a small
// brand-green dot at its origin. Editor-only. Mirrors the 3D addon's VANodeGizmoPlugin sphere.
public partial class VAEmitter
{
    // Brand green, matching icons/vercidium.svg's fill.
    static readonly Color GizmoColor = new("85ffa4");
    const float GizmoRadius = 6f;

    public override void _Draw()
    {
        if (!Engine.IsEditorHint())
            return;

        DrawCircle(Vector2.Zero, GizmoRadius, GizmoColor);
        DrawArc(Vector2.Zero, GizmoRadius, 0f, Mathf.Tau, 24, new Color(GizmoColor, 1f), 1.5f);
    }
}
