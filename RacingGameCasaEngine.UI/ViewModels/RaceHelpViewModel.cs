using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.Help</c>, RacingGame's help screen (ADR-0013): the menu decoration, the "HELP" header, the
/// help image and the B BACK button, laid out every frame in screen pixels with RacingGame's formulas
/// (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/Help.cs</c>, Render; <c>Graphics/UIRenderer.cs</c>,
/// RenderBottomButtons).
/// </summary>
public sealed class RaceHelpViewModel : RaceViewModelBase
{
    private Rectangle _backButtonRect;

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    /// <summary>The "HELP" header (headers.png HeaderHelpGfxRect), drawn with RenderOnScreenRelative1600(10, 18).</summary>
    public RaceScreenRectViewModel Header { get; } = new();

    /// <summary>The help image (HelpScreenWindows.png), drawn with RenderOnScreenRelative4To3(0, 125).</summary>
    public RaceScreenRectViewModel Panel { get; } = new();

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>Lays everything out for the viewport, growing the B BACK button under the mouse.</summary>
    public void Update(int viewportWidth, int viewportHeight, int mouseX, int mouseY)
    {
        var layout = new LegacyScreenLayout(viewportWidth, viewportHeight);
        Set(Header, layout.CalcRectangle1600(10, 18, 512, 100));
        Set(Panel, layout.CalcRectangleKeep4To3(0, 125, 1024, 512));

        _backButtonRect = layout.BackButton();
        bool hovered = _backButtonRect.Contains(mouseX, mouseY);
        Set(BackButton, hovered ? layout.GrowBottomButton(_backButtonRect) : _backButtonRect);
        BackButton.IsHighlighted = hovered;
    }

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

    private static void Set(RaceScreenRectViewModel target, Rectangle rect) => target.Set(rect.X, rect.Y, rect.Width, rect.Height);
}
