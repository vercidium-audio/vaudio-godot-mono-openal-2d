namespace vaudio_godot_mono_openal;

// Lightweight child node that fires OnTransformChanged when its parent's global transform changes.
// SetNotifyTransform is called here (not on the parent) so only this node gets the notification.
partial class TransformWatcher : Node2D
{
    public Action OnTransformChanged { get; set; }

    public override void _Ready()
    {
        SetNotifyTransform(true);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged)
            return;

        // Godot only re-queues NOTIFICATION_TRANSFORM_CHANGED for a Node2D once its cached global
        // transform has been read again - without this touch, only the first move after the node
        // enters the tree ever fires.
        _ = GlobalTransform;

        OnTransformChanged?.Invoke();
    }
}
