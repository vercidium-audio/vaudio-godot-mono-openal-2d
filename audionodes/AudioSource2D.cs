namespace vaudio_godot_mono_openal;

[Tool]
public partial class AudioSource2D : AudioSource
{
    float _maxDistance = 100;
    float _referenceDistance = 8;

    /// <summary>The max distance that the sound can be heard at. Also affected by the falloff model set on the audio backend</summary>
    [Export]
    public float MaxDistance
    {
        get => _maxDistance;
        set => UpdateProperty(ref _maxDistance, MathF.Max(0, value), (v, source) => source.SetMaxDistance(v));
    }

    /// <summary>The distance that sound volume falloff starts at</summary>
    [Export]
    public float ReferenceDistance
    {
        get => _referenceDistance;
        set => UpdateProperty(ref _referenceDistance, MathF.Max(0, value), (v, source) => source.SetReferenceDistance(v));
    }

    // OpenAL is always a 3D API - 2D position maps onto the XY plane with Z left at 0.
    Vector3 SpatialPosition => new(GlobalPosition.X, GlobalPosition.Y, 0);

    protected override void ConfigureSource(IAudioSourceHandle source)
    {
        source.SetMaxDistance(MaxDistance);
        source.SetReferenceDistance(ReferenceDistance);
        source.SetPosition(SpatialPosition);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint())
            return;

        foreach (var s in sources)
            s.SetPosition(SpatialPosition);
    }
}
