using MGUI.Core.UI;
using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// One button of the main menu, in screen pixels: its square, the parts drawn with MGUI brushes (grey rim, inner bevel,
/// orange ring of the selected button) sized from RacingGame's 212x212 button art, its opacity and its label. Set by
/// <see cref="RaceMainMenuViewModel.Update"/>; the button sits in a slot of the row whose top is the active button's top.
/// The plain values (<see cref="Size"/>, <see cref="Top"/>, <see cref="Rim"/>, <see cref="Radius"/>,
/// <see cref="LabelGap"/>, <see cref="IsSelected"/>) are the ones design-time data sets; the others derive from them.
/// </summary>
public sealed class RaceMainMenuButtonViewModel : RaceViewModelBase
{
    // RacingGame's button art (buttons.png): a 212 px cell, 10 px rim, 10 px inner bevel, 38.5 px outer corner radius,
    // and 212x24 labels.
    private const float ArtSize = 212f;
    private const float ArtRimThickness = 10f;
    private const float ArtCornerRadius = 38.5f;
    private const int ArtLabelHeight = 24;

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

    public Visibility RingVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>The label's top-left corner in the slot, under the button.</summary>
    public Thickness LabelMargin => new(0, _top + _size + _labelGap, 0, 0);

    public int LabelHeight => _size * ArtLabelHeight / (int)ArtSize;

    public Visibility LabelVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

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
