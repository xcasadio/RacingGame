using CasaEngine.Framework.Assets;
using CasaEngine.Framework.UI;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Base of the RacingGameCasaEngine screens loaded from a catalogued <c>.uiscreen</c> asset (ADR-0001): the tree is the
/// screen's XAML, values come from a view model set as the window data context, actions and initial focus stay in code.
/// <para/>
/// Screens are single-use: <see cref="GameScreenManager"/> builds a new instance on every transition and
/// <see cref="ScreenStack"/> never disposes one, so <see cref="Hide"/> (called when the screen is popped or removed)
/// disposes it to take its bindings out of MGUI's static registry and give its envelope handle back.
/// </summary>
internal abstract class RaceXamlScreenBase : XamlUIScreenBase
{
    // Registered by MGDesktop.LoadDefaultResources (check boxes draw it); the engine runtime never calls it.
    private const string DefaultResourcesMarkerTexture = "CheckMark_64x64";

    protected RaceXamlScreenBase(AssetContentManager assetContentManager, string screenAssetName)
        : base(assetContentManager, screenAssetName)
    {
    }

    protected override void OnInitialize(UIRoot root)
    {
        // Textures, not TryGetTexture: an unknown name makes TryGetTexture fall back to the asset catalog
        // (MGUI ADR-0016), which logs a warning for a texture LoadDefaultResources adds just below.
        if (!root.Desktop.Resources.Textures.ContainsKey(DefaultResourcesMarkerTexture))
        {
            root.Desktop.LoadDefaultResources();
        }

        base.OnInitialize(root);
    }

    public override void Hide()
    {
        base.Hide();
        Dispose();
    }
}
