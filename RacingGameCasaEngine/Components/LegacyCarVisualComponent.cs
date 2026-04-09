using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using CasaEngine.Framework.Rendering.Models;

namespace RacingGameCasaEngine.Components;

public sealed class LegacyCarVisualComponent : EntityComponent
{
    private bool _isVisualReady;
    private bool _hasLoggedFailure;

    public override EntityComponent Clone()
    {
        return new LegacyCarVisualComponent();
    }

    public override void InitializeWithWorld(World world)
    {
        base.InitializeWithWorld(world);
        TryInitializeVisual();
    }

    public override void Update(float elapsedTime)
    {
        if (!_isVisualReady)
        {
            TryInitializeVisual();
        }
    }

    private void TryInitializeVisual()
    {
        if (_isVisualReady || Owner is not RacingCarPawn pawn || pawn.World == null)
        {
            return;
        }

        StaticModelComponent? visualComponent = pawn.CarVisualComponent;
        if (visualComponent == null)
        {
            if (!_hasLoggedFailure)
            {
                Logs.WriteWarning("Player car visual component is missing from RacingCarPawn hierarchy.");
                _hasLoggedFailure = true;
            }

            return;
        }

        AssetContentManager assetContentManager = pawn.World.Game.AssetContentManager;
        StaticModel? model = LegacyCarVisualFactory.LoadConfiguredCarModel(assetContentManager, pawn.SelectedCarIndex, pawn.SelectedCarColorIndex);
        if (model == null)
        {
            if (!_hasLoggedFailure)
            {
                Logs.WriteWarning("Unable to load legacy car model; keeping debug car visual fallback active.");
                _hasLoggedFailure = true;
            }

            return;
        }

        BoundingBox bounds = LegacyCarVisualFactory.GetCarBounds(assetContentManager);
        float scale = LegacyCarVisualFactory.ComputeUniformScale(bounds);
        float visualPivotLiftY = pawn.VisualPivotComponent?.LocalPosition.Y ?? 0f;

        visualComponent.StaticModel = model;
        visualComponent.LocalScale = new Vector3(scale);
        visualComponent.LocalOrientation = LegacyCarVisualFactory.LegacyCarFacingCorrection;
        visualComponent.LocalPosition = LegacyCarVisualFactory.ComputeGroundedModelOffset(bounds, scale, visualPivotLiftY);
        visualComponent.InitializeWithWorld(pawn.World);

        _isVisualReady = true;
    }
}