// Writes premultiplied-alpha copies of RacingGameCasaEngine's UI images (ADR-0012). MGUI draws its images with
// premultiplied blending (BlendState.AlphaBlend), and CasaEngine loads PNG files as they are, while RacingGame premultiplied
// its textures when it built them (RacingGame/Content/Content.mgcb, PremultiplyAlpha=True). Drawn as they are, the
// semi-transparent edge pixels added their full colour, which brightened every rounded or antialiased edge.
//
// Each texel becomes (round(r * a / 255), round(g * a / 255), round(b * a / 255), a): opaque texels keep their colour,
// transparent ones become black. The sources are left as they are (scripts/MenuIconExtractor reads buttons.png).
//
// Usage: UiTexturePremultiplier <repository root>
// Exit codes: 0 = images written, 2 = bad arguments or input.
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// (source, output), relative to the repository root.
(string Source, string Output)[] images =
[
    ("RacingGameCasaEngine/Content/Textures/background.png", "RacingGameCasaEngine/Content/UI/Textures/background.png"),
    ("RacingGameCasaEngine/Content/Textures/buttons.png", "RacingGameCasaEngine/Content/UI/Textures/buttons.png"),
    ("RacingGameCasaEngine/Content/Textures/ingame.png", "RacingGameCasaEngine/Content/UI/Textures/ingame.png"),
    ("RacingGameCasaEngine/Content/Textures/headers.png", "RacingGameCasaEngine/Content/UI/Textures/headers.png"),
    ("RacingGameCasaEngine/Content/Textures/ColorSelection.png", "RacingGameCasaEngine/Content/UI/Textures/ColorSelection.png"),
    ("RacingGameCasaEngine/Content/Textures/OptionsScreenWindows.png", "RacingGameCasaEngine/Content/UI/Textures/OptionsScreenWindows.png"),
    // The GameFont page, next to its .fnt (scripts/generate_rgce_gamefont.py).
    ("RacingGame/Content/Textures/GameFont.png", "RacingGameCasaEngine/Content/UI/Fonts/GameFont.png"),
];

if (args.Length != 1 || !Directory.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: UiTexturePremultiplier <repository root>");
    return 2;
}

foreach (var (source, output) in images)
{
    string sourcePath = Path.Combine(args[0], source);
    string outputPath = Path.Combine(args[0], output);
    if (!File.Exists(sourcePath))
    {
        Console.Error.WriteLine($"File not found: {sourcePath}");
        return 2;
    }

    using var bitmap = new Bitmap(sourcePath);
    var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    var pixels = new int[bitmap.Width * bitmap.Height];
    BitmapData read = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    Marshal.Copy(read.Scan0, pixels, 0, pixels.Length);
    bitmap.UnlockBits(read);

    for (int i = 0; i < pixels.Length; i++)
    {
        int argb = pixels[i];
        int a = (argb >> 24) & 0xFF;
        int r = Premultiply((argb >> 16) & 0xFF, a);
        int g = Premultiply((argb >> 8) & 0xFF, a);
        int b = Premultiply(argb & 0xFF, a);
        pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
    }

    using var premultiplied = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
    BitmapData write = premultiplied.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
    Marshal.Copy(pixels, 0, write.Scan0, pixels.Length);
    premultiplied.UnlockBits(write);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    premultiplied.Save(outputPath, ImageFormat.Png);
    Console.WriteLine($"{output} ({bitmap.Width}x{bitmap.Height})");
}

return 0;

// Rounded, so that an opaque texel keeps its exact colour.
static int Premultiply(int channel, int alpha) => (channel * alpha + 127) / 255;
