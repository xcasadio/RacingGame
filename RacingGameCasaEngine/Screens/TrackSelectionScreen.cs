using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.BorderBrushes;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Screens;

/// <summary>Track selection, loaded from the <c>Screen.TrackSelection</c> screen asset (Content/UI/Screens/TrackSelection).</summary>
internal sealed class TrackSelectionScreen : RaceXamlScreenBase
{
    // TrackSelection.xaml has one slot per catalogue track (btnTrack*, frameTrack*, lblTrack*).
    private const int TrackSlotCount = 3;

    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private readonly RaceTrackSelectionViewModel _viewModel = new();
    private MGButton[] _trackButtons = [];
    private MGBorder[] _trackFrames = [];
    private MGTextBlock[] _trackLabels = [];
    private MGButton? _selectButton;
    private MGButton? _backButton;

    public TrackSelectionScreen(AssetContentManager assetContentManager, RaceFrontEndState state, Action confirm, Action back)
        : base(assetContentManager, "Screen.TrackSelection")
    {
        _state = state;
        _confirm = confirm;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        var tracks = RaceFrontEndCatalog.Tracks;
        if (tracks.Count != TrackSlotCount)
        {
            throw new InvalidOperationException($"TrackSelection.xaml has {TrackSlotCount} track slots; the catalogue has {tracks.Count} tracks.");
        }

        _viewModel.Track0Name = tracks[0].Name;
        _viewModel.Track1Name = tracks[1].Name;
        _viewModel.Track2Name = tracks[2].Name;
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);

        _trackButtons = new MGButton[TrackSlotCount];
        _trackFrames = new MGBorder[TrackSlotCount];
        _trackLabels = new MGTextBlock[TrackSlotCount];
        for (int i = 0; i < TrackSlotCount; i++)
        {
            int index = i;
            _trackButtons[i] = FindControl<MGButton>("btnTrack" + i);
            _trackFrames[i] = FindControl<MGBorder>("frameTrack" + i);
            _trackLabels[i] = FindControl<MGTextBlock>("lblTrack" + i);
            _trackButtons[i].AddCommandHandler((_, _) => SelectTrack(index));
        }

        // LegacyMenuUiTheme.ApplySpriteButtonState dims the image a sprite button carries in its Tag.
        _selectButton = FindControl<MGButton>("btnSelect");
        _selectButton.Tag = FindControl<MGImage>("imgSelect");
        _selectButton.AddCommandHandler((_, _) => _confirm());
        _backButton = FindControl<MGButton>("btnBack");
        _backButton.Tag = FindControl<MGImage>("imgBack");
        _backButton.AddCommandHandler((_, _) => _back());

        RefreshSelection();
    }

    public override void Show()
    {
        if (_state.SelectedTrackIndex >= 0 && _state.SelectedTrackIndex < _trackButtons.Length)
        {
            _trackButtons[_state.SelectedTrackIndex].Focus();
        }
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);
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
        for (int i = 0; i < _trackButtons.Length; i++)
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
}
