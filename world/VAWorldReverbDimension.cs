// Human-reviewed

namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    // Where a grouped EAX's reverb comes from relative to the listener, derived from the listener's single rotation angle. Used by common/world/VAWorldReverb.cs
    Vector3 GetGroupedEAXPan(vaudio.EAXReverb eax)
    {
        // The SDK returns listener space (X+ right, Y+ forward). The pan is left-handed listener space: +X right, +Z forward
        var pan = world.CalculateListenerRelativePan(eax.GetRelativeDirection(listener.emitter), listener.GlobalRotation);

        return new Vector3(pan.X, 0, pan.Y);
    }
}
