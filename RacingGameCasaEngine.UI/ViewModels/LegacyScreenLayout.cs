using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// RacingGame's resolution helpers for one viewport (<c>git show 4f840a3^:RacingGame.Shared/Graphics/BaseGame.cs</c>,
/// XToRes to CalcRectangleCenteredWithGivenHeight), with the same float arithmetic and the same rounding, half to even.
/// RacingGame laid its screens out on a 1024x640 base, its Options and Help panels on a 1024x768 base (Keep4To3), its
/// headers on a 1600x1200 base and its bitmap-font texts on a 1400x1050 base (TextureFont).
/// </summary>
public readonly struct LegacyScreenLayout
{
    // UIRenderer.BottomButton*GfxRect (212x92) of buttons.png, 48 units high, centred on y 587 (RenderBottomButtons).
    private const int BottomButtonArtWidth = 212;
    private const int BottomButtonArtHeight = 92;
    private const int BottomButtonHeight = 48;
    private const int BottomButtonCentreY = 587;

    public LegacyScreenLayout(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }

    public int Width { get; }

    public int Height { get; }

    public int XToRes(int x) => (int)Math.Round(x * Width / 1024.0f);

    public int YToRes(int y) => (int)Math.Round(y * Height / 640.0f);

    public int YToRes768(int y) => (int)Math.Round(y * Height / 768.0f);

    public int XToRes1400(int x) => (int)Math.Round(x * Width / 1400.0f);

    public int YToRes1050(int y) => (int)Math.Round(y * Height / 1050.0f);

    public Rectangle CalcRectangle(int x, int y, int width, int height) => Scale(x, y, width, height, Width / 1024.0f, Height / 640.0f);

    public Rectangle CalcRectangleKeep4To3(int x, int y, int width, int height) => Scale(x, y, width, height, Width / 1024.0f, Height / 768.0f);

    public Rectangle CalcRectangleKeep4To3(Rectangle rect) => CalcRectangleKeep4To3(rect.X, rect.Y, rect.Width, rect.Height);

    public Rectangle CalcRectangle1600(int x, int y, int width, int height) => Scale(x, y, width, height, Width / 1600.0f, Height / 1200.0f);

    /// <summary>A rectangle <paramref name="height"/> units high, as wide as the art's aspect gives, centred on (x, y).</summary>
    public Rectangle CalcRectangleCenteredWithGivenHeight(int x, int y, int height, int artWidth, int artHeight)
    {
        float widthFactor = Width / 1024.0f;
        float heightFactor = Height / 640.0f;
        int rectHeight = (int)Math.Round(height * heightFactor);
        int rectWidth = (int)Math.Round(artWidth * rectHeight / (float)artHeight);
        return new Rectangle(
            Math.Max(0, (int)Math.Round(x * widthFactor) - rectWidth / 2),
            Math.Max(0, (int)Math.Round(y * heightFactor) - rectHeight / 2),
            rectWidth,
            rectHeight);
    }

    /// <summary>The B BACK button at the bottom right, where UIRenderer.RenderBottomButtons drew it and tested the mouse.</summary>
    public Rectangle BackButton()
    {
        Rectangle rect = CalcRectangleCenteredWithGivenHeight(0, BottomButtonCentreY, BottomButtonHeight, BottomButtonArtWidth, BottomButtonArtHeight);
        rect.X = Width - rect.Width - XToRes(25 + 25);
        return rect;
    }

    /// <summary>A bottom button as drawn while the mouse is over it: grown by XToRes(16) x YToRes(9) around its centre.</summary>
    public Rectangle GrowBottomButton(Rectangle rect)
    {
        int xAdd = XToRes(16);
        int yAdd = YToRes(9);
        return new Rectangle(rect.X - xAdd / 2, rect.Y - yAdd / 2, rect.Width + xAdd, rect.Height + yAdd);
    }

    private static Rectangle Scale(int x, int y, int width, int height, float widthFactor, float heightFactor) =>
        new((int)Math.Round(x * widthFactor), (int)Math.Round(y * heightFactor), (int)Math.Round(width * widthFactor), (int)Math.Round(height * heightFactor));
}
