using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.FillBrushes;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.CarSelection</c>: the selected car's summary and four stats, the 3D preview image rendered by
/// the screen, and the eleven fixed colour swatches of CarSelection.xaml. A swatch binds its whole background brush, built
/// from its colour as the code-built screen did, so every visual state shows the colour. The colours are strings in the
/// XAML colour syntax (<c>rgba(r,g,b,a)</c>, a name or <c>#RRGGBB</c>), which design-time data can give.
/// </summary>
public sealed class RaceCarSelectionViewModel : RaceViewModelBase
{
    public const int StatCount = 4;
    public const int SwatchCount = 11;

    private readonly string[] _statLabels = Enumerable.Repeat(string.Empty, StatCount).ToArray();
    private readonly float[] _statFills = new float[StatCount];
    private readonly string[] _swatchColors = Enumerable.Repeat(string.Empty, SwatchCount).ToArray();
    private readonly VisualStateFillBrush?[] _swatchBrushes = new VisualStateFillBrush?[SwatchCount];
    private string _summary = string.Empty;
    private MGTextureData? _previewImage;

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    /// <summary>Car name and summary, on two lines.</summary>
    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    /// <summary>The render target of the 3D car preview; set by the screen, absent at design time.</summary>
    public MGTextureData? PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    public string Stat0Label { get => _statLabels[0]; set => SetStatLabel(0, value); }

    public string Stat1Label { get => _statLabels[1]; set => SetStatLabel(1, value); }

    public string Stat2Label { get => _statLabels[2]; set => SetStatLabel(2, value); }

    public string Stat3Label { get => _statLabels[3]; set => SetStatLabel(3, value); }

    public float Stat0Fill { get => _statFills[0]; set => SetStatFill(0, value); }

    public float Stat1Fill { get => _statFills[1]; set => SetStatFill(1, value); }

    public float Stat2Fill { get => _statFills[2]; set => SetStatFill(2, value); }

    public float Stat3Fill { get => _statFills[3]; set => SetStatFill(3, value); }

    public string Swatch0Color { get => _swatchColors[0]; set => SetSwatchColor(0, value); }

    public string Swatch1Color { get => _swatchColors[1]; set => SetSwatchColor(1, value); }

    public string Swatch2Color { get => _swatchColors[2]; set => SetSwatchColor(2, value); }

    public string Swatch3Color { get => _swatchColors[3]; set => SetSwatchColor(3, value); }

    public string Swatch4Color { get => _swatchColors[4]; set => SetSwatchColor(4, value); }

    public string Swatch5Color { get => _swatchColors[5]; set => SetSwatchColor(5, value); }

    public string Swatch6Color { get => _swatchColors[6]; set => SetSwatchColor(6, value); }

    public string Swatch7Color { get => _swatchColors[7]; set => SetSwatchColor(7, value); }

    public string Swatch8Color { get => _swatchColors[8]; set => SetSwatchColor(8, value); }

    public string Swatch9Color { get => _swatchColors[9]; set => SetSwatchColor(9, value); }

    public string Swatch10Color { get => _swatchColors[10]; set => SetSwatchColor(10, value); }

    public VisualStateFillBrush? Swatch0Brush => _swatchBrushes[0];

    public VisualStateFillBrush? Swatch1Brush => _swatchBrushes[1];

    public VisualStateFillBrush? Swatch2Brush => _swatchBrushes[2];

    public VisualStateFillBrush? Swatch3Brush => _swatchBrushes[3];

    public VisualStateFillBrush? Swatch4Brush => _swatchBrushes[4];

    public VisualStateFillBrush? Swatch5Brush => _swatchBrushes[5];

    public VisualStateFillBrush? Swatch6Brush => _swatchBrushes[6];

    public VisualStateFillBrush? Swatch7Brush => _swatchBrushes[7];

    public VisualStateFillBrush? Swatch8Brush => _swatchBrushes[8];

    public VisualStateFillBrush? Swatch9Brush => _swatchBrushes[9];

    public VisualStateFillBrush? Swatch10Brush => _swatchBrushes[10];

    public void SetStatLabel(int index, string label)
    {
        SetProperty(ref _statLabels[index], label, $"Stat{index}Label");
    }

    public void SetStatFill(int index, float fillPercent)
    {
        SetProperty(ref _statFills[index], fillPercent, $"Stat{index}Fill");
    }

    /// <summary>Sets a swatch from a colour in the XAML colour syntax.</summary>
    public void SetSwatchColor(int index, string color)
    {
        SetSwatch(index, color, XNAColorStringConverter.ParseColor(color));
    }

    /// <summary>Sets a swatch from the game's colour, kept exact for the brush.</summary>
    public void SetSwatchColor(int index, Color color)
    {
        SetSwatch(index, $"rgba({color.R},{color.G},{color.B},{color.A})", color);
    }

    private void SetSwatch(int index, string text, Color color)
    {
        if (_swatchBrushes[index] != null && _swatchColors[index] == text)
        {
            return;
        }

        _swatchColors[index] = text;
        _swatchBrushes[index] = new VisualStateFillBrush(color.AsFillBrush());
        NotifyPropertyChanged($"Swatch{index}Color");
        NotifyPropertyChanged($"Swatch{index}Brush");
    }
}
