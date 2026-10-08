using System.ComponentModel;
using CasaEngine.Core.Serialization;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// The physics of a race track: the road profile the vehicle solvers sample. It computes the profile when it is
/// initialized, from the same catalogued assets as the generator components.
/// </summary>
[DisplayName("Race Track Physics")]
public sealed class RaceTrackPhysicsComponent : EntityComponent
{
    /// <summary>The catalogued .Track asset (TrackBeginnerData, TrackAdvancedData or TrackExpertData).</summary>
    [SerializedProperty("track_asset_id")]
    public Guid TrackAssetId { get; set; }

    /// <summary>The catalogued landscape height map (Data.LandscapeHeights).</summary>
    [SerializedProperty("heightmap_asset_id")]
    public Guid HeightmapAssetId { get; set; }

    public RaceTrackPhysicsComponent()
    {
    }

    private RaceTrackPhysicsComponent(RaceTrackPhysicsComponent other)
        : base(other)
    {
        SerializedProperties.Copy(other, this);
        TrackPhysicsProfile = other.TrackPhysicsProfile;
        ShoulderWidth = other.ShoulderWidth;
        GuardRailInset = other.GuardRailInset;
        BarrierContactMargin = other.BarrierContactMargin;
        BarrierGlancingSpeedRetainFactor = other.BarrierGlancingSpeedRetainFactor;
        EdgeSpeedRetainFactor = other.EdgeSpeedRetainFactor;
        ShoulderDeceleration = other.ShoulderDeceleration;
    }

    /// <summary>The road profile; null until the component is initialized, or when its assets cannot be resolved.</summary>
    internal RaceTrackPhysicsProfile? TrackPhysicsProfile { get; private set; }

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

    public override void InitializeWithWorld(World world)
    {
        base.InitializeWithWorld(world);

        TrackPhysicsProfile = null;
        string owner = $"{nameof(RaceTrackPhysicsComponent)} '{Owner?.Name}'";
        try
        {
            if (LegacyTrackSceneFactory.TryResolveTrackSource(owner, TrackAssetId, HeightmapAssetId, out RaceTrackSource? source))
            {
                TrackPhysicsProfile = LegacyTrackSceneFactory.ComputeGeneratorLayout(source!, RaceTrackLayoutSettings.Default).PhysicsProfile;
            }
        }
        catch (Exception exception)
        {
            Exception cause = exception.GetBaseException();
            Logs.WriteWarning($"{owner}: the track profile could not be computed: {cause.GetType().Name}: {cause.Message}");
        }
    }

    internal bool TrySampleSurface(Vector3 position, int segmentHint, out RaceTrackSurfaceSample sample)
    {
        if (TrackPhysicsProfile == null)
        {
            sample = default;
            return false;
        }

        return TrackPhysicsProfile.TrySample(position, segmentHint, out sample);
    }
}
