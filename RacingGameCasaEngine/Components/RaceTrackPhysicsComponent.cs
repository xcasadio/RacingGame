using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Components;

internal sealed class RaceTrackPhysicsComponent : EntityComponent
{
    public RaceTrackPhysicsComponent(RaceTrackPhysicsProfile trackPhysicsProfile)
    {
        TrackPhysicsProfile = trackPhysicsProfile ?? throw new ArgumentNullException(nameof(trackPhysicsProfile));
    }

    private RaceTrackPhysicsComponent(RaceTrackPhysicsComponent other)
        : base(other)
    {
        TrackPhysicsProfile = other.TrackPhysicsProfile;
        ShoulderWidth = other.ShoulderWidth;
        GuardRailInset = other.GuardRailInset;
        BarrierContactMargin = other.BarrierContactMargin;
        BarrierGlancingSpeedRetainFactor = other.BarrierGlancingSpeedRetainFactor;
        EdgeSpeedRetainFactor = other.EdgeSpeedRetainFactor;
        ShoulderDeceleration = other.ShoulderDeceleration;
    }

    public RaceTrackPhysicsProfile TrackPhysicsProfile { get; }

    public float ShoulderWidth { get; set; } = 2.5f;

    public float GuardRailInset { get; set; } = 0.25f;

    public float BarrierContactMargin { get; set; } = 0.05f;

    public float BarrierGlancingSpeedRetainFactor { get; set; } = 0.96f;

    public float EdgeSpeedRetainFactor { get; set; } = 0.93f;

    public float ShoulderDeceleration { get; set; } = 12f;

    public override EntityComponent Clone()
    {
        return new RaceTrackPhysicsComponent(this);
    }

    public bool TrySampleSurface(Vector3 position, int segmentHint, out RaceTrackSurfaceSample sample)
    {
        return TrackPhysicsProfile.TrySample(position, segmentHint, out sample);
    }
}