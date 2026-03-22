using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;

namespace RacingGameCasaEngine.Screens;

internal sealed class HelpScreen : RaceFrontEndScreenBase
{
    private readonly Action _back;
    private MGButton? _backButton;

    public HelpScreen(Microsoft.Xna.Framework.Graphics.Texture2D? backgroundTexture, Microsoft.Xna.Framework.Graphics.Texture2D? buttonsTexture, Action back)
        : base(backgroundTexture, buttonsTexture)
    {
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        var window = CreateForegroundWindow(root);
        var band = LegacyMenuUiTheme.CreateMenuBand(window, 120, 480, new MonoGame.Extended.Thickness(32, 22, 32, 18));
        var layout = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 10);
        layout.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        layout.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        layout.TryAddChild(LegacyMenuUiTheme.CreateHeading(window, "Help"));

        var scrollViewer = new MGScrollViewer(window)
        {
            PreferredWidth = 900,
            PreferredHeight = 290,
            AllowClickDragScrolling = false,
        };

        var content = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 12);
        foreach (var section in RaceFrontEndCatalog.HelpSections)
        {
            content.TryAddChild(LegacyMenuUiTheme.CreateBodyText(window, section.Title, LegacyMenuUiTheme.AccentColor));
            foreach (string line in section.Lines)
                content.TryAddChild(LegacyMenuUiTheme.CreateBodyText(window, line));
        }
        scrollViewer.SetContent(content);
        layout.TryAddChild(scrollViewer);

        _backButton = LegacyMenuUiTheme.CreateMenuTextButton(window, "Back", _back);
        layout.TryAddChild(_backButton);

        band.SetContent(layout);
        window.SetContent(band);
    }

    public override void Show()
    {
        _backButton?.Focus();
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        if (_backButton != null)
        {
            LegacyMenuUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
        }
    }
}