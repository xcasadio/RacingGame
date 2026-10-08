using System.ComponentModel;
using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the landscape mesh of a race track from its height map, centered like the rest of the track.</summary>
[DisplayName("Race Terrain Generator")]
public sealed class RaceTerrainGeneratorComponent : RaceTrackGeneratorComponent
{
    /// <summary>The catalogued material (Material.Track.Terrain) the landscape is drawn with.</summary>
    [SerializedProperty("material_asset_id")]
    public Guid MaterialAssetId { get; set; } = RaceTrackMaterialAssets.Terrain;

    public RaceTerrainGeneratorComponent()
    {
    }

    private RaceTerrainGeneratorComponent(RaceTerrainGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceTerrainGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature(materialAssetId: MaterialAssetId);

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        output.Add(LegacyTrackSceneFactory.CreateTerrainModel(source, MaterialAssetId));
    }
}
