using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// The settings a generator compares, every frame, with the ones it last built. A plain value: comparing two of them
/// allocates nothing.
/// </summary>
internal readonly record struct RaceGeneratorSignature(
    Guid TrackAssetId,
    Guid HeightmapAssetId,
    float HolderGap,
    float ColumnsDistance,
    RaceGuardRailSide Side,
    Guid MaterialAssetId);

/// <summary>
/// A scene component, root of its entity, that builds a part of a race track from catalogued data assets: the .Track layout
/// and the landscape height map. It adds what it builds as <see cref="RaceGeneratedModelComponent"/> children, and builds
/// again when its settings change, in the game and in the editor preview alike.
/// <para>
/// Ownership: the procedural models a generator creates are its own, and it frees their GPU buffers when it builds again
/// and when it is detached. It never frees what the engine shares: the catalogued models of the posts and of the column
/// segments, held by their components, and the compiled material of its <c>material_asset_id</c>.
/// </para>
/// </summary>
public abstract class RaceTrackGeneratorComponent : SceneComponent, IEntityPolicyDefaultsProvider
{
    // Models whose buffers this generator frees: the procedural ones it built. Replaced only on a build.
    private readonly List<StaticModel> _ownedModels = [];
    private World? _world;
    private bool _hasBuilt;
    private RaceGeneratorSignature _builtSignature;

    /// <summary>The catalogued .Track asset (TrackBeginnerData, TrackAdvancedData or TrackExpertData).</summary>
    [SerializedProperty("track_asset_id")]
    public Guid TrackAssetId { get; set; }

    /// <summary>The catalogued landscape height map (Data.LandscapeHeights).</summary>
    [SerializedProperty("heightmap_asset_id")]
    public Guid HeightmapAssetId { get; set; }

    private protected RaceTrackGeneratorComponent()
    {
    }

    private protected RaceTrackGeneratorComponent(RaceTrackGeneratorComponent other)
        : base(other)
    {
        //  A clone keeps the settings, not the generated children: SceneComponent's copy cloned them, and they belong to the
        //  build of the original (they share its models). The clone builds its own when it is initialized.
        SerializedProperties.Copy(other, this);
        RemoveGeneratedChildren();
    }

    private protected abstract RaceGeneratorSignature CreateSignature();

    /// <summary>Adds the models to build, from the resolved track assets, to <paramref name="output"/>.</summary>
    private protected abstract void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output);

    private protected RaceGeneratorSignature MakeSignature(float holderGap = 0f, float columnsDistance = 0f, RaceGuardRailSide side = RaceGuardRailSide.Left, Guid materialAssetId = default)
    {
        return new RaceGeneratorSignature(TrackAssetId, HeightmapAssetId, holderGap, columnsDistance, side, materialAssetId);
    }

    // A generator ticks in the editor, which may not set explicit policies: it must see its settings change.
    public void ApplyEntityPolicyDefaults(Entity owner, ref EntityPolicyDefaultsBuilder defaults)
    {
        defaults.Apply(new EntityPolicySet(Mobility.Static, TickPolicy.EveryFrame, SpatialPolicy.StaticIndex, RenderDynamicPolicy.Static));
    }

    public override void InitializeWithWorld(World world)
    {
        _world = world;
        // A duplicated entity, or a component initialized again, may carry generated children: they are rebuilt below.
        RemoveGeneratedChildren();
        base.InitializeWithWorld(world);
        Rebuild();
    }

    public override void Update(float elapsedTime)
    {
        if (_hasBuilt && _world != null && CreateSignature() != _builtSignature)
        {
            Rebuild();
        }

        base.Update(elapsedTime);
    }

    public override void Detach()
    {
        base.Detach();
        RemoveGeneratedChildren();
        _world = null;
        _hasBuilt = false;
    }

    // Like StaticModelComponent: the bounds are those of what is drawn, not SceneComponent's unit box.
    public override BoundingBox GetBoundingBox()
    {
        bool hasBounds = false;
        BoundingBox bounds = default;
        for (int index = 0; index < Children.Count; index++)
        {
            ExpandBounds(Children[index], ref hasBounds, ref bounds);
        }

        return hasBounds ? bounds : base.GetBoundingBox();
    }

    private static void ExpandBounds(SceneComponent component, ref bool hasBounds, ref BoundingBox bounds)
    {
        BoundingBox componentBounds = component.BoundingBox;
        bounds = hasBounds ? BoundingBox.CreateMerged(bounds, componentBounds) : componentBounds;
        hasBounds = true;

        for (int index = 0; index < component.Children.Count; index++)
        {
            ExpandBounds(component.Children[index], ref hasBounds, ref bounds);
        }
    }

    private void Rebuild()
    {
        _builtSignature = CreateSignature();
        _hasBuilt = true;
        RemoveGeneratedChildren();

        World? world = _world;
        if (world == null)
        {
            return;
        }

        string owner = $"{GetType().Name} '{Owner?.Name}'";
        try
        {
            if (LegacyTrackSceneFactory.TryResolveTrackSource(owner, TrackAssetId, HeightmapAssetId, out RaceTrackSource? source))
            {
                var generated = new List<RaceGeneratedModel>();
                Generate(source!, world.Game.AssetContentManager, generated);

                // Registered before any buffer exists, so a failure below still frees them.
                foreach (RaceGeneratedModel model in generated)
                {
                    if (model.OwnsBuffers && model.Model != null)
                    {
                        _ownedModels.Add(model.Model);
                    }
                }

                foreach (RaceGeneratedModel model in generated)
                {
                    AddGeneratedChild(model, world);
                }
            }
        }
        catch (Exception exception)
        {
            // Once per settings change: the signature is already recorded, so Update does not try again every frame.
            Exception cause = exception.GetBaseException();
            Logs.WriteWarning($"{owner}: the generation failed, nothing is kept: {cause.GetType().Name}: {cause.Message}");
            RemoveGeneratedChildren();
        }

        MarkBoundingBoxDirty();
    }

    private void AddGeneratedChild(RaceGeneratedModel model, World world)
    {
        if (!model.Transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
        {
            scale = Vector3.One;
            rotation = Quaternion.Identity;
            translation = model.Transform.Translation;
        }

        var child = new RaceGeneratedModelComponent
        {
            Name = model.Name,
            CastShadows = model.CastShadows,
        };
        if (model.StaticModelAssetId != Guid.Empty)
        {
            child.StaticModelAssetId = model.StaticModelAssetId;
        }
        else
        {
            child.StaticModel = model.Model!;
        }

        child.LocalTransform.Position = translation;
        child.LocalTransform.Orientation = rotation;
        child.LocalTransform.Scale = scale;

        AddChildComponent(child);
        child.InitializeWithWorld(world);
    }

    // Removes the generated children, then frees the GPU buffers of the procedural models this generator created.
    private void RemoveGeneratedChildren()
    {
        for (int index = Children.Count - 1; index >= 0; index--)
        {
            if (Children[index] is RaceGeneratedModelComponent generated)
            {
                RemoveChildComponent(generated);
            }
        }

        for (int modelIndex = 0; modelIndex < _ownedModels.Count; modelIndex++)
        {
            List<StaticModelMesh> meshes = _ownedModels[modelIndex].Meshes;
            for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
            {
                meshes[meshIndex].VertexBuffer?.Dispose();
                meshes[meshIndex].IndexBuffer?.Dispose();
            }
        }

        _ownedModels.Clear();
    }
}
