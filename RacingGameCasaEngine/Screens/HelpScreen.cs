using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>Help, loaded from the <c>Screen.Help</c> screen asset (Content/UI/Screens/Help).</summary>
internal sealed class HelpScreen : RaceXamlScreenBase
{
    private readonly Action _back;
    private readonly RaceHelpViewModel _viewModel = new();
    private MGButton? _backButton;

    public HelpScreen(AssetContentManager assetContentManager, Action back)
        : base(assetContentManager, "Screen.Help")
    {
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        FillSections(_viewModel.Sections, RaceFrontEndCatalog.HelpSections);
        _viewModel.BackButton.Update(Root.Metrics.Scale);
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);

        _backButton = FindControl<MGButton>("btnBack");
        _backButton.AddCommandHandler((_, _) => _back());
    }

    public override void Show()
    {
        _backButton?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);

        if (_backButton != null)
        {
            LegacyMenuUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
        }
    }

    // Help.xaml has one fixed slot per catalogue section, each with a title and two lines.
    private static void FillSections(IReadOnlyList<RaceHelpSectionViewModel> slots, IReadOnlyList<HelpSection> sections)
    {
        if (sections.Count != slots.Count || sections.Any(section => section.Lines.Count != 2))
        {
            throw new InvalidOperationException(
                $"Help.xaml has {slots.Count} section slots of two lines each; the catalogue has {sections.Count} sections.");
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Title = sections[i].Title;
            slots[i].Line0 = sections[i].Lines[0];
            slots[i].Line1 = sections[i].Lines[1];
        }
    }
}
