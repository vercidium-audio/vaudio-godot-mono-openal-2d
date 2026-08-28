namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    // 2D implementation of common/world/VAWorldReverb.cs's ApplyGroupedEAXPan - derives the
    // listener-relative pan from the listener's single rotation angle.
    partial void ApplyGroupedEAXPan(vaudio.EAXReverb eax, ALReverbEffect effect)
    {
        if (eax.RelativeDirections == null || !eax.RelativeDirections.TryGetValue(listener.emitter, out var pan))
            return;

        // Convert to a listener-relative vector for OpenAL
        pan = world.CalculateListenerRelativePan(pan, listener.GlobalRotation);

        effect.effectSlotGain = eax.RelativeGains[listener.emitter];
        effect.effectSlotGain = Math.Max(0, effect.effectSlotGain);
        effect.effectSlotGain = Math.Min(1, effect.effectSlotGain);

        // TODO - separate pan for late reverb and reflections. OpenAL pan is always 3D; the 2D world maps onto its XY plane with Z fixed at 0.
        effect.lateReverbPan[0] = pan.X;
        effect.lateReverbPan[1] = pan.Y;
        effect.lateReverbPan[2] = 0;

        effect.reflectionsPan[0] = pan.X;
        effect.reflectionsPan[1] = pan.Y;
        effect.reflectionsPan[2] = 0;
    }
}
