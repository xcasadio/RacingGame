using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.BorderBrushes;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// One button of the main menu, in screen pixels: its square, the parts drawn with MGUI brushes (grey rim, inner bevel,
/// orange ring of the selected button) sized from RacingGame's 212x212 button art, its opacity and its label. Set by
/// <see cref="RaceMainMenuViewModel.Update"/>; the button sits in a slot of the row whose top is the active button's top.
/// The plain values (<see cref="Size"/>, <see cref="Top"/>, <see cref="Rim"/>, <see cref="Radius"/>,
/// <see cref="LabelGap"/>, <see cref="SlotHeight"/>, <see cref="IsSelected"/>) are the ones design-time data sets; the
/// others derive from them.
/// </summary>
public sealed class RaceMainMenuButtonViewModel : RaceViewModelBase
{
    // RacingGame's button art (buttons.png): a 212 px cell, 10 px rim, 10 px inner bevel, 38.5 px outer corner radius,
    // and 212x24 labels.
    private const float ArtSize = 212f;
    private const float ArtRimThickness = 10f;
    private const float ArtCornerRadius = 38.5f;
    private const int ArtLabelHeight = 24;

    // The art's inner bevel, from the rim inwards, 10 px per side (buttons.png, every button cell, at x or y = 106).
    private static readonly int[] ArtBevelTop = [87, 112, 140, 164, 186, 205, 219, 231, 240, 246];
    private static readonly int[] ArtBevelSide = [70, 90, 114, 134, 151, 166, 178, 189, 197, 202];
    private static readonly int[] ArtBevelBottom = [55, 69, 87, 103, 116, 129, 138, 147, 153, 157];

    // Opacity of an unselected button's glyph. RacingGame tints the whole button sprite at 192/255 (premultiplied), so
    // its black ink shows 0.247 of what lies under the button (U, 31 to 62 on the band, 44 on average). Here the face is
    // drawn at 0.75 first (0.75 x 205 + 0.25 x 44 = 165 in the middle), so the glyph needs 1 - 0.247 x 44 / 165 = 0.93
    // to leave the same ink, about 11, instead of the 0.75 x 0.75 layering's 35 to 41.
    private const float UnselectedGlyphOpacity = 0.93f;

    // One bevel brush per thickness, shared by the buttons.
    private static readonly Dictionary<int, IBorderBrush> BevelBrushes = new();

    private int _size;
    private int _top;
    private int _rim;
    private int _radius;
    private int _labelGap;
    private int _slotHeight;
    private bool _isSelected;

    /// <summary>Side of the square button.</summary>
    public int Size
    {
        get => _size;
        set
        {
            if (SetProperty(ref _size, value))
            {
                NotifyPropertyChanged(nameof(LabelMargin));
                NotifyPropertyChanged(nameof(LabelHeight));
            }
        }
    }

    /// <summary>The button's top, relative to its slot.</summary>
    public int Top
    {
        get => _top;
        set
        {
            if (SetProperty(ref _top, value))
            {
                NotifyPropertyChanged(nameof(Margin));
                NotifyPropertyChanged(nameof(LabelMargin));
            }
        }
    }

    /// <summary>Thickness of the grey rim, of the orange ring that replaces it, and of the inner bevel.</summary>
    public int Rim
    {
        get => _rim;
        set
        {
            if (SetProperty(ref _rim, value))
            {
                NotifyPropertyChanged(nameof(RimThickness));
                NotifyPropertyChanged(nameof(BevelCornerRadius));
                NotifyPropertyChanged(nameof(BevelBrush));
            }
        }
    }

    /// <summary>Outer corner radius.</summary>
    public int Radius
    {
        get => _radius;
        set
        {
            if (SetProperty(ref _radius, value))
            {
                NotifyPropertyChanged(nameof(CornerRadius));
                NotifyPropertyChanged(nameof(BevelCornerRadius));
            }
        }
    }

    /// <summary>Space between the button and its label (RacingGame's <c>YToRes(5)</c>).</summary>
    public int LabelGap
    {
        get => _labelGap;
        set
        {
            if (SetProperty(ref _labelGap, value))
            {
                NotifyPropertyChanged(nameof(LabelMargin));
            }
        }
    }

    /// <summary>Height of every slot of the row.</summary>
    public int SlotHeight
    {
        get => _slotHeight;
        set => SetProperty(ref _slotHeight, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                NotifyPropertyChanged(nameof(Opacity));
                NotifyPropertyChanged(nameof(GlyphOpacity));
                NotifyPropertyChanged(nameof(RingVisibility));
                NotifyPropertyChanged(nameof(LabelVisibility));
            }
        }
    }

    /// <summary>The button's top-left corner in its slot.</summary>
    public Thickness Margin => new(0, _top, 0, 0);

    public MGCornerRadius CornerRadius => new(_radius);

    public Thickness RimThickness => new(_rim);

    /// <summary>Corner radius inside the rim, concentric with <see cref="CornerRadius"/>.</summary>
    public MGCornerRadius BevelCornerRadius => new(Math.Max(0, _radius - _rim));

    /// <summary>1 for the selected button; 0.75 for the others, as RacingGame tinted them (192/255, premultiplied).</summary>
    public float Opacity => _isSelected ? 1f : 0.75f;

    /// <summary>1 for the selected button; for the others, the opacity that leaves the original's ink.</summary>
    public float GlyphOpacity => _isSelected ? 1f : UnselectedGlyphOpacity;

    /// <summary>
    /// The inner bevel as wide as the rim: one 1-pixel band per pixel of thickness, each a docked border with the art's
    /// colour per side at that depth, so the bevel ramps from dark at the rim to the face colour, darker at the bottom.
    /// </summary>
    public IBorderBrush BevelBrush => GetBevelBrush(_rim);

    public Visibility RingVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>The label's top-left corner in the slot, under the button.</summary>
    public Thickness LabelMargin => new(0, _top + _size + _labelGap, 0, 0);

    public int LabelHeight => _size * ArtLabelHeight / (int)ArtSize;

    public Visibility LabelVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    private static IBorderBrush GetBevelBrush(int thickness)
    {
        thickness = Math.Max(1, thickness);
        if (BevelBrushes.TryGetValue(thickness, out IBorderBrush? brush))
        {
            return brush;
        }

        // MGBandedBorderBrush gives each band (int)(thickness x weight / total weight) pixels: with equal weights, a band
        // count whose share truncates below 1 pixel would draw nothing, so the count drops until each band gets 1.
        int bandCount = thickness;
        while (bandCount > 1 && (int)(thickness * (1.0 / bandCount)) < 1)
        {
            bandCount--;
        }

        var bands = new MGBorderBand[bandCount];
        for (int i = 0; i < bandCount; i++)
        {
            float artPosition = (i + 0.5f) * ArtBevelSide.Length / bandCount - 0.5f;
            Color side = Grey(Sample(ArtBevelSide, artPosition));
            bands[i] = new MGBorderBand(new MGDockedBorderBrush(side, Grey(Sample(ArtBevelTop, artPosition)), side, Grey(Sample(ArtBevelBottom, artPosition))), 1.0);
        }

        brush = new MGBandedBorderBrush(bands);
        BevelBrushes[thickness] = brush;
        return brush;
    }

    private static float Sample(int[] profile, float position)
    {
        position = Math.Clamp(position, 0f, profile.Length - 1);
        int index = Math.Min((int)position, profile.Length - 2);
        return profile[index] + (profile[index + 1] - profile[index]) * (position - index);
    }

    private static Color Grey(float value)
    {
        int level = (int)Math.Round(value);
        return new Color(level, level, level);
    }

    /// <summary>Lays the button out with RacingGame's rectangles (MainMenu.Render).</summary>
    /// <param name="size">The button's side, interpolated between the inactive and the active size.</param>
    /// <param name="top">The button's top, relative to the slot.</param>
    /// <param name="labelGap">Space between the button and its label.</param>
    /// <param name="slotHeight">Height of every slot of the row.</param>
    /// <param name="selected">Whether this is the selected button.</param>
    public void Layout(int size, int top, int labelGap, int slotHeight, bool selected)
    {
        Size = size;
        Top = top;
        Rim = Math.Max(1, (int)Math.Round(ArtRimThickness * size / ArtSize));
        Radius = (int)Math.Round(ArtCornerRadius * size / ArtSize);
        LabelGap = labelGap;
        SlotHeight = slotHeight;
        IsSelected = selected;
    }
}
