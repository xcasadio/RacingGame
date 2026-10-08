using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Highscores, loaded from the <c>Screen.Highscores</c> screen asset (Content/UI/Screens/Highscores), as RacingGame's
/// (ADR-0014), with its input (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/Highscores.cs</c>, Update):
/// <list type="bullet">
/// <item>a click on a level tab selects it, and Left and Right (keyboard, D-pad, left stick past 0.5) select the
/// previous or next level, wrapping, all with the ButtonClick sound;</item>
/// <item>Escape, B, Back, a click below the board or on B BACK leave with ScreenBack;</item>
/// <item>the Highlight sound plays when the mouse enters a tab, a filled board line or the B button.</item>
/// </list>
/// The screen opens on Advanced, plays ScreenClick when shown, as RacingGame did when it pushed a screen, and shows
/// RacingGameCasaEngine's highscore catalogue. Nothing takes MGUI's keyboard focus.
/// </summary>
internal sealed class HighscoresScreen : RaceXamlScreenBase
{
    private const float StickThreshold = 0.5f;
    // RacingGame ended the separator line where its widest time, "5:67:89", would end.
    private const string WidestTime = "5:67:89";

    private readonly RacingGameCasaEngineGame _game;
    private readonly Action _back;
    private readonly RaceHighscoresViewModel _viewModel = new();
    private MGTextBlock[] _texts = [];
    private MGTextBlock[] _tabTexts = [];
    private MGTextBlock[] _rankTexts = [];
    private MGTextBlock[] _nameTexts = [];
    private int _entryCount;
    // The first update after the screen opens takes no input: the key or click that opened it is still "just pressed".
    private bool _acceptsInput;
    private bool _isLeaving;
    private Point _lastMouse = new(-1, -1);
    private float _lastStickX;

    public HighscoresScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, Action back)
        : base(assetContentManager, "Screen.Highscores")
    {
        _game = game;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        if (RaceFrontEndCatalog.Tracks.Count != RaceHighscoresViewModel.LevelCount)
        {
            throw new InvalidOperationException(
                $"Highscores.xaml has {RaceHighscoresViewModel.LevelCount} level tabs; the catalogue has {RaceFrontEndCatalog.Tracks.Count} tracks.");
        }

        _tabTexts = FindTexts("txtTab", RaceHighscoresViewModel.LevelCount);
        _rankTexts = FindTexts("txtRank", RaceHighscoresViewModel.RowCount);
        _nameTexts = FindTexts("txtName", RaceHighscoresViewModel.RowCount);
        _texts = [.. _tabTexts, .. _rankTexts, .. _nameTexts, .. FindTexts("txtTime", RaceHighscoresViewModel.RowCount)];
        foreach (MGTextBlock text in _texts)
        {
            text.RenderTransform.Origin = Vector2.Zero;
        }

        window.WindowDataContext = _viewModel;
        RefreshBoard();
        UpdateLayout(0.0);
    }

    public override void Show()
    {
        PlaySound(MenuSound.ScreenClick);
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        if (_isLeaving)
        {
            return;
        }

        UpdateLayout(gameTime.TotalGameTime.TotalSeconds);
    }

    private void HandleInput()
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        MouseManager mouse = _game.InputComponent.MouseManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);
        Point position = mouse.Position;
        float stickX = gamePad.IsConnected ? gamePad.LeftStickX : 0f;

        if (!_acceptsInput)
        {
            _acceptsInput = true;
            _lastMouse = position;
            _lastStickX = stickX;
            return;
        }

        // Input.MouseInBox played Highlight whenever the mouse entered a box it tested.
        int tab = _viewModel.GetTabAt(position.X, position.Y);
        int row = _viewModel.GetRowAt(position.X, position.Y);
        if ((tab >= 0 && tab != _viewModel.GetTabAt(_lastMouse.X, _lastMouse.Y))
            || (row >= 0 && row < _entryCount && row != _viewModel.GetRowAt(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverBackButton(position.X, position.Y) && !_viewModel.IsOverBackButton(_lastMouse.X, _lastMouse.Y)))
        {
            PlaySound(MenuSound.Highlight);
        }

        _lastMouse = position;

        bool click = mouse.LeftButtonJustPressed;
        if (click && tab >= 0)
        {
            PlaySound(MenuSound.ButtonClick);
            SelectLevel(tab);
        }

        bool left = keyboard.IsKeyJustPressed(XnaKeys.Left)
            || (gamePad.IsConnected && gamePad.DPadLeftJustPressed)
            || (stickX < -StickThreshold && _lastStickX >= -StickThreshold);
        bool right = keyboard.IsKeyJustPressed(XnaKeys.Right)
            || (gamePad.IsConnected && gamePad.DPadRightJustPressed)
            || (stickX > StickThreshold && _lastStickX <= StickThreshold);
        _lastStickX = stickX;
        int count = RaceHighscoresViewModel.LevelCount;
        if (left)
        {
            PlaySound(MenuSound.ButtonClick);
            SelectLevel((_viewModel.SelectedLevel + count - 1) % count);
        }
        else if (right)
        {
            PlaySound(MenuSound.ButtonClick);
            SelectLevel((_viewModel.SelectedLevel + 1) % count);
        }

        if (keyboard.IsKeyJustPressed(XnaKeys.Escape)
            || (gamePad.IsConnected && (gamePad.BJustPressed || gamePad.BackJustPressed))
            || (click && _viewModel.IsBelowRows(position.Y))
            || (click && _viewModel.IsOverBackButton(position.X, position.Y)))
        {
            _isLeaving = true;
            PlaySound(MenuSound.ScreenBack);
            _back();
        }
    }

    private void SelectLevel(int level)
    {
        _viewModel.SelectedLevel = level;
        RefreshBoard();
    }

    // Fills the board lines with the selected level's entries; the lines past them stay empty.
    private void RefreshBoard()
    {
        IReadOnlyList<HighscoreEntry> entries = RaceFrontEndCatalog.Highscores[RaceFrontEndCatalog.Tracks[_viewModel.SelectedLevel].Name];
        _entryCount = Math.Min(entries.Count, RaceHighscoresViewModel.RowCount);
        for (int i = 0; i < RaceHighscoresViewModel.RowCount; i++)
        {
            RaceHighscoreRowViewModel row = _viewModel.Rows[i];
            row.Rank = i < _entryCount ? $"{i + 1}." : string.Empty;
            row.Name = i < _entryCount ? entries[i].PlayerName : string.Empty;
            row.Time = i < _entryCount ? FormatTime(entries[i].Time) : string.Empty;
        }
    }

    // The catalogue writes times as "mm:ss.cc"; RacingGame's WriteGameTime wrote "m:ss.cc".
    private static string FormatTime(string time)
    {
        int colon = time.IndexOf(':');
        return colon > 0 && int.TryParse(time.AsSpan(0, colon), out int minutes) ? $"{minutes}{time[colon..]}" : time;
    }

    private MGTextBlock[] FindTexts(string prefix, int count)
    {
        var texts = new MGTextBlock[count];
        for (int i = 0; i < count; i++)
        {
            texts[i] = FindControl<MGTextBlock>(prefix + i);
        }

        return texts;
    }

    private void PlaySound(MenuSound sound) => _game.MenuSounds?.Play(sound);

    // Lays the screen out for the viewport, the time and the mouse, then scales and colours the texts as RacingGame's
    // TextureFont and Highscores.Render did.
    private void UpdateLayout(double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        Point mouse = _game.InputComponent.MouseManager.Position;
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, mouse.X, mouse.Y, _game.MeasureGameFontText(WidestTime));

        var scale = new Vector2(_viewModel.TextScaleX, _viewModel.TextScaleY);
        foreach (MGTextBlock text in _texts)
        {
            text.RenderTransform.Scale = scale;
        }

        for (int i = 0; i < _tabTexts.Length; i++)
        {
            SetForeground(_tabTexts[i], _viewModel.TabColors[i]);
        }

        for (int i = 0; i < RaceHighscoresViewModel.RowCount; i++)
        {
            SetForeground(_rankTexts[i], _viewModel.RowColors[i]);
            SetForeground(_nameTexts[i], _viewModel.RowColors[i]);
        }
    }

    private static void SetForeground(MGTextBlock text, Color color)
    {
        if (text.ActualForeground != color)
        {
            text.Foreground = new(color, color, color);
        }
    }
}
