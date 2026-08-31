namespace vaudio_godot_mono_openal;

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
