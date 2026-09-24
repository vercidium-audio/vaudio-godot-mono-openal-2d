namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    // 2D implementation of common/world/VAWorldReverb.cs's ApplyGroupedEAXPan - derives the
    // listener-relative pan from the listener's single rotation angle.
    partial void ApplyGroupedEAXPan(vaudio.EAXReverb eax, ALReverbEffect effect)
    {
        if (eax.RelativeDirections == null || !eax.RelativeDirections.TryGetValue(listener.emitter, out var direction))
            return;

        // RelativeDirections are in vaudio's internal Y-up space, not Godot's Y-down. Godot's clockwise-positive rotation is counter-clockwise in Y-up space, so rotating by +rotation moves the direction into listener space.
        float c = MathF.Cos(listener.GlobalRotation);
        float s = MathF.Sin(listener.GlobalRotation);

        float right = (direction.X * c) - (direction.Y * s);
        float forward = (direction.X * s) + (direction.Y * c);

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
