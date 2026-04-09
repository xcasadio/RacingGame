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
        EdgeSpeedRetainFactor = other.EdgeSpeedRetainFactor;
        ShoulderDeceleration = other.ShoulderDeceleration;
    }

    public RaceTrackPhysicsProfile TrackPhysicsProfile { get; }

    public float ShoulderWidth { get; set; } = 2.5f;

    public float EdgeSpeedRetainFactor { get; set; } = 0.58f;

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