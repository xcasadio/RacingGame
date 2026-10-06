using MGUI.Core.UI;
using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// A rectangle of a screen laid out in screen pixels, for a top-left aligned element of an overlay panel: its margin is
/// its top-left corner. <see cref="IsShown"/> hides it, and <see cref="IsHighlighted"/> shows a highlight drawn over it
/// (the orange outline of a hovered A or B button).
/// </summary>
public sealed class RaceScreenRectViewModel : RaceViewModelBase
{
    private int _left;
    private int _top;
    private int _width;
    private int _height;
    private bool _isShown = true;
    private bool _isHighlighted;

    public int Left
    {
        get => _left;
        set
        {
            if (SetProperty(ref _left, value))
            {
                NotifyPropertyChanged(nameof(Margin));
            }
        }
    }

    public int Top
    {
        get => _top;
        set
        {
            if (SetProperty(ref _top, value))
            {
                NotifyPropertyChanged(nameof(Margin));
            }
        }
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public bool IsShown
    {
        get => _isShown;
        set
        {
            if (SetProperty(ref _isShown, value))
            {
                NotifyPropertyChanged(nameof(Visibility));
            }
        }
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (SetProperty(ref _isHighlighted, value))
            {
                NotifyPropertyChanged(nameof(HighlightVisibility));
            }
        }
    }

    public Thickness Margin => new(_left, _top, 0, 0);

    public Visibility Visibility => _isShown ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HighlightVisibility => _isHighlighted ? Visibility.Visible : Visibility.Collapsed;

    public void Set(int left, int top, int width, int height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    /// <summary>Whether the point (in screen pixels) is inside, as RacingGame's <c>Input.MouseInBox</c> tested it.</summary>
    public bool Contains(int x, int y) => x >= _left && y >= _top && x < _left + _width && y < _top + _height;
}
