using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace RacingGameCasaEngine.Screens;

internal sealed class TrackSelectionScreen : RaceFrontEndScreenBase
{
    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private readonly List<MGButton> _trackButtons = [];
    private readonly List<MGBorder> _trackFrames = [];
    private readonly List<MGTextBlock> _trackLabels = [];
    private MGButton? _selectButton;
    private MGButton? _backButton;

    public TrackSelectionScreen(Texture2D? backgroundTexture, Texture2D? buttonsTexture, RaceFrontEndState state, Action confirm, Action back)
        : base(backgroundTexture, buttonsTexture)
    {
        _state = state;
        _confirm = confirm;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        if (ButtonsTexture == null)
        {
            throw new InvalidOperationException("Legacy menu atlas is required for the track selection screen.");
        }

        var window = CreateForegroundWindow(root);
        var rootPanel = new MGOverlayPanel(window)
        {
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Stretch,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Stretch,
        };

        var band = LegacyMenuUiTheme.CreateMenuBand(window, 248, 315, new MonoGame.Extended.Thickness(32, 22, 32, 18));
        var stack = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 48);
        stack.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        stack.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

        Rectangle[] trackRects =
        [
            LegacyMenuUiAtlas.TrackButtonBeginner,
            LegacyMenuUiAtlas.TrackButtonAdvanced,
            LegacyMenuUiAtlas.TrackButtonExpert,
        ];

        for (int i = 0; i < RaceFrontEndCatalog.Tracks.Count; i++)
        {
            int capturedIndex = i;
            var item = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 8);
            item.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
            item.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

            var button = CreateTrackButton(window, ButtonsTexture, trackRects[i], () => SelectTrack(capturedIndex), out MGBorder frame);
            _trackButtons.Add(button);
            _trackFrames.Add(frame);

            var label = LegacyMenuUiTheme.CreateBodyText(window, RaceFrontEndCatalog.Tracks[i].Name, LegacyMenuUiTheme.PrimaryTextColor);
            label.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
            label.TextAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
            _trackLabels.Add(label);

            item.TryAddChild(button);
            item.TryAddChild(label);
            stack.TryAddChild(item);
        }

        band.SetContent(stack);

        var actions = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 14);
        actions.ResponsiveAnchor = ResponsiveAnchor.BottomRight;
        _selectButton = LegacyMenuUiTheme.CreateSpriteButton(window, ButtonsTexture, LegacyMenuUiAtlas.BottomButtonA, _confirm);
        _backButton = LegacyMenuUiTheme.CreateSpriteButton(window, ButtonsTexture, LegacyMenuUiAtlas.BottomButtonB, _back);
        actions.TryAddChild(_selectButton);
        actions.TryAddChild(_backButton);
        rootPanel.TryAddChild(band);
        rootPanel.TryAddChild(actions, new MonoGame.Extended.Thickness(0, 0, 48, 20));

        window.SetContent(rootPanel);

        RefreshSelection();
    }

    public override void Show()
    {
        if (_state.SelectedTrackIndex >= 0 && _state.SelectedTrackIndex < _trackButtons.Count)
        {
            _trackButtons[_state.SelectedTrackIndex].Focus();
        }
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(gameTime.TotalGameTime.TotalSeconds);
        RefreshSelection();
        if (_selectButton != null) LegacyMenuUiTheme.ApplySpriteButtonState(_selectButton);
        if (_backButton != null) LegacyMenuUiTheme.ApplySpriteButtonState(_backButton);
    }

    private void SelectTrack(int index)
    {
        _state.SelectedTrackIndex = index;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < _trackButtons.Count; i++)
        {
            bool selected = _state.SelectedTrackIndex == i;
            _trackLabels[i].DefaultTextForeground = selected
                ? new VisualStateSetting<Color?>(LegacyMenuUiTheme.AccentColor, LegacyMenuUiTheme.AccentColor, LegacyMenuUiTheme.AccentColor)
                : new VisualStateSetting<Color?>(LegacyMenuUiTheme.PrimaryTextColor, LegacyMenuUiTheme.PrimaryTextColor, LegacyMenuUiTheme.PrimaryTextColor);
            _trackFrames[i].BorderBrush = selected
                ? new MGUniformBorderBrush(LegacyMenuUiTheme.AccentColor)
                : new MGUniformBorderBrush(new Color(255, 255, 255, 70));
            _trackFrames[i].BorderThickness = selected ? new MonoGame.Extended.Thickness(4) : new MonoGame.Extended.Thickness(2);
        }
    }

    private static MGButton CreateTrackButton(MGWindow window, Texture2D texture, Rectangle sourceRect, Action action, out MGBorder frame)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
            BorderThickness = new MonoGame.Extended.Thickness(0),
            Padding = new MonoGame.Extended.Thickness(0),
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };

        frame = new MGBorder(window, new MonoGame.Extended.Thickness(2), new MGUniformBorderBrush(new Color(255, 255, 255, 70)))
        {
            CornerRadius = new MGCornerRadius(18),
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 24).AsFillBrush()),
            Padding = new MonoGame.Extended.Thickness(0),
            PreferredWidth = 172,
            PreferredHeight = 286,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };

        frame.SetContent(new MGImage(window, UiImageResources.AsImage(texture), sourceRect, null, Stretch.Uniform)
        {
            PreferredWidth = 168,
            PreferredHeight = 280,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        });
        button.SetContent(frame);
        return button;
    }
}