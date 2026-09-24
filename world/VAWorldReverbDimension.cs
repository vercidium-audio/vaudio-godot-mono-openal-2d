namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    // 2D implementation of common/world/VAWorldReverb.cs's ApplyGroupedEAXPan - derives the
    // listener-relative pan from the listener's single rotation angle.
    partial void ApplyGroupedEAXPan(vaudio.EAXReverb eax, ALReverbEffect effect)
    {
        if (eax.RelativeDirections == null || !eax.RelativeDirections.TryGetValue(listener.emitter, out var direction))
            return;

        // The SDK returns listener space (X+ right, Y+ forward)
        var pan = world.CalculateListenerRelativePan(direction, listener.GlobalRotation);
        float right = pan.X;
        float forward = pan.Y;

        effect.effectSlotGain = eax.RelativeGains[listener.emitter];
        effect.effectSlotGain = Math.Max(0, effect.effectSlotGain);
        effect.effectSlotGain = Math.Min(1, effect.effectSlotGain);

        // TODO - separate pan for late reverb and reflections. EFX pan is left-handed listener space: +X right, +Z forward.
        effect.lateReverbPan[0] = right;
        effect.lateReverbPan[1] = 0;
        effect.lateReverbPan[2] = forward;

        effect.reflectionsPan[0] = right;
        effect.reflectionsPan[1] = 0;
        effect.reflectionsPan[2] = forward;
    }
}
