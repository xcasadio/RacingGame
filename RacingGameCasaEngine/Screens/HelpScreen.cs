using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Point = Microsoft.Xna.Framework.Point;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Help, loaded from the <c>Screen.Help</c> screen asset (Content/UI/Screens/Help): the help sections of
/// <see cref="RaceFrontEndCatalog.HelpSections"/> in the menus' style (ADR-0014), with RacingGame's help input
/// (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/Help.cs</c>, Update): Escape, B, Back or a left click anywhere
/// close it with the ScreenBack sound. The screen plays ScreenClick when shown, as RacingGame did when it pushed a screen,
/// and Highlight when the mouse enters the B BACK button. Nothing takes MGUI's keyboard focus.
/// </summary>
internal sealed class HelpScreen : RaceXamlScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly Action _back;
    private readonly RaceHelpViewModel _viewModel = new();
    private MGTextBlock[] _titles = [];
    private MGTextBlock[] _lines = [];
    // The first update after the screen opens takes no input: the key or click that opened it is still "just pressed".
    private bool _acceptsInput;
    private bool _isLeaving;
    private Point _lastMouse = new(-1, -1);

    public HelpScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, Action back)
        : base(assetContentManager, "Screen.Help")
    {
        _game = game;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        IReadOnlyList<HelpSection> sections = RaceFrontEndCatalog.HelpSections;
        if (sections.Count != RaceHelpViewModel.SectionCount || sections.Any(section => section.Lines.Count != 2))
        {
            throw new InvalidOperationException(
                $"Help.xaml has {RaceHelpViewModel.SectionCount} section slots of two lines each; the catalogue has {sections.Count} sections.");
        }

        _titles = new MGTextBlock[RaceHelpViewModel.SectionCount];
        _lines = new MGTextBlock[RaceHelpViewModel.SectionCount * 2];
        for (int i = 0; i < RaceHelpViewModel.SectionCount; i++)
        {
            RaceHelpSectionViewModel slot = _viewModel.Sections[i];
            slot.Title = sections[i].Title;
            slot.Line0 = sections[i].Lines[0];
            slot.Line1 = sections[i].Lines[1];
            _titles[i] = FindControl<MGTextBlock>($"txtSection{i}Title");
            _lines[2 * i] = FindControl<MGTextBlock>($"txtSection{i}Line0");
            _lines[2 * i + 1] = FindControl<MGTextBlock>($"txtSection{i}Line1");
        }

        foreach (MGTextBlock text in _titles.Concat(_lines))
        {
            text.RenderTransform.Origin = Vector2.Zero;
        }

        window.WindowDataContext = _viewModel;
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

        if (!_acceptsInput)
        {
            _acceptsInput = true;
            _lastMouse = position;
            return;
        }

        // Input.MouseInBox played Highlight whenever the mouse entered a box it tested.
        if (_viewModel.IsOverBackButton(position.X, position.Y) && !_viewModel.IsOverBackButton(_lastMouse.X, _lastMouse.Y))
        {
            PlaySound(MenuSound.Highlight);
        }

        _lastMouse = position;

        if (keyboard.IsKeyJustPressed(XnaKeys.Escape)
            || (gamePad.IsConnected && (gamePad.BJustPressed || gamePad.BackJustPressed))
            || mouse.LeftButtonJustPressed)
        {
            _isLeaving = true;
            PlaySound(MenuSound.ScreenBack);
            _back();
        }
    }

    private void PlaySound(MenuSound sound) => _game.MenuSounds?.Play(sound);

    // Lays the screen out for the viewport, the time and the mouse, as RacingGame did every frame, and scales the texts
    // as TextureFont did: the titles at full size, the lines at RaceHelpViewModel.LineScale.
    private void UpdateLayout(double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        Point mouse = _game.InputComponent.MouseManager.Position;
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, mouse.X, mouse.Y);

        var titleScale = new Vector2(_viewModel.TextScaleX, _viewModel.TextScaleY);
        foreach (MGTextBlock title in _titles)
        {
            title.RenderTransform.Scale = titleScale;
        }

        Vector2 lineScale = titleScale * RaceHelpViewModel.LineScale;
        foreach (MGTextBlock line in _lines)
        {
            line.RenderTransform.Scale = lineScale;
        }
    }
}
