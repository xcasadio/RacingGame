using System.ComponentModel;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the landscape mesh of a race track from its height map, centered like the rest of the track.</summary>
[DisplayName("Race Terrain Generator")]
public sealed class RaceTerrainGeneratorComponent : RaceTrackGeneratorComponent
{
    public RaceTerrainGeneratorComponent()
    {
    }

    private RaceTerrainGeneratorComponent(RaceTerrainGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceTerrainGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature();

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        output.Add(LegacyTrackSceneFactory.CreateTerrainModel(source, assetContentManager));
    }
}
