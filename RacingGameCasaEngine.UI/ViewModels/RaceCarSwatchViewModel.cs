using MGUI.Core.UI.Brushes.FillBrushes;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// One colour square of the car selection: RacingGame's ColorSelection sprite, tinted with the colour, at its rectangle.
/// The colour is set as a string in the XAML colour syntax (<c>rgba(r,g,b,a)</c>, a name or <c>#RRGGBB</c>), which
/// design-time data can give, or as the game's colour; <see cref="TextureColor"/> is what the image binds.
/// </summary>
public sealed class RaceCarSwatchViewModel : RaceViewModelBase
{
    private string _color = string.Empty;
    private Color? _textureColor;

    public RaceScreenRectViewModel Rect { get; } = new();

    public string ColorText
    {
        get => _color;
        set => SetColor(value, XNAColorStringConverter.ParseColor(value));
    }

    public Color? TextureColor => _textureColor;

    public void SetColor(Color color)
    {
        SetColor($"rgba({color.R},{color.G},{color.B},{color.A})", color);
    }

    private void SetColor(string text, Color color)
    {
        if (_textureColor == color && _color == text)
        {
            return;
        }

        _color = text;
        _textureColor = color;
        NotifyPropertyChanged(nameof(ColorText));
        NotifyPropertyChanged(nameof(TextureColor));
    }
}
