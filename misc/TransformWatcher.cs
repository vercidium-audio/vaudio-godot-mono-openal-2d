// Human-created

namespace vaudio_godot_mono_openal;

/// <summary>Node that fires OnTransformChanged when its parent's global transform changes</summary>
partial class TransformWatcher : Node2D
{
    public Action OnTransformChanged { get; set; }

    public override void _Ready()
    {
        // Called here instead of on the parent, so only this node gets the notification
        SetNotifyTransform(true);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged)
            return;

        // Godot only re-queues NOTIFICATION_TRANSFORM_CHANGED for a Node2D once its cached global transform has been read again.
        //  Without this line, only the first move after the node enters the tree ever fires.
        //  2D Godot doesn't have this issue.
        _ = GlobalTransform;

        OnTransformChanged?.Invoke();
    }
}
