// Extracts the black icon glyphs of RacingGame's main-menu buttons from buttons.png onto a transparent background, so
// RacingGameCasaEngine can draw them over buttons painted with MGUI brushes (plan ai-agent/tasks/
// rgce-main-menu-xna-look-tasks.md, P3).
//
// In each 212x212 button cell the icon is pure black, antialiased over a face that is a vertical grey gradient, flat
// along each row. The face grey of a row is the lightest pixel of that row inside the face; a pixel of luminance L over
// a face grey F is black at coverage (F - L) / F. Only the glyph's box is kept (its half-covered pixels, widened by the
// antialiasing margin): the face also darkens at the rounded inner corners, which are not ink. The output keeps each
// cell's full 212x212 frame, transparent outside the glyph, so a glyph image stretched over a button of the cell's size
// lands where the original icon was.
//
// Exit codes: 0 = image written, 2 = bad arguments or input.
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

const int CellSize = 212;
// Inside each cell: the face core, clear of the 10 px rim (1..10) and the 10 px inner bevel (11..20); the face spans 21..190.
const int FaceStart = 22;
const int FaceEnd = 189;
// Coverage below this is the face itself (gradient noise), not glyph ink.
const double MinimumCoverage = 3.0 / 255.0;
// Pixels kept around the glyph's half-covered box, for its antialiased edge.
const int AntialiasingMargin = 3;

// Button cells of buttons.png, in menu order (RacingGame.Shared/Graphics/UIRenderer.cs, MenuButton*GfxRect).
(string Name, int X, int Y)[] cells =
[
    ("Play", 0, 0),
    ("Highscores", 212, 0),
    ("Options", 424, 0),
    ("Help", 636, 0),
    ("Quit", 212, 240),
];

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: MenuIconExtractor <buttons.png> <output png>");
    return 2;
}

if (!File.Exists(args[0]))
{
    Console.Error.WriteLine($"File not found: {args[0]}");
    return 2;
}

using var source = new Bitmap(args[0]);
int[] sourcePixels = ReadPixels(source);
var output = new int[cells.Length * CellSize * CellSize];

for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
{
    var cell = cells[cellIndex];
    var coverage = new double[CellSize, CellSize];
    int left = CellSize, top = CellSize, right = -1, bottom = -1;
    for (int y = FaceStart; y <= FaceEnd; y++)
    {
        int face = 0;
        for (int x = FaceStart; x <= FaceEnd; x++)
        {
            face = Math.Max(face, Luminance(sourcePixels[(cell.Y + y) * source.Width + cell.X + x]));
        }

        if (face == 0)
        {
            continue;
        }

        for (int x = FaceStart; x <= FaceEnd; x++)
        {
            int luminance = Luminance(sourcePixels[(cell.Y + y) * source.Width + cell.X + x]);
            coverage[x, y] = Math.Clamp((face - luminance) / (double)face, 0.0, 1.0);
            if (coverage[x, y] >= 0.5)
            {
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
    }

    if (right < 0)
    {
        Console.Error.WriteLine($"No glyph found in the {cell.Name} cell.");
        return 2;
    }

    for (int y = Math.Max(FaceStart, top - AntialiasingMargin); y <= Math.Min(FaceEnd, bottom + AntialiasingMargin); y++)
    {
        for (int x = Math.Max(FaceStart, left - AntialiasingMargin); x <= Math.Min(FaceEnd, right + AntialiasingMargin); x++)
        {
            if (coverage[x, y] < MinimumCoverage)
            {
                continue;
            }

            int alpha = (int)Math.Round(coverage[x, y] * 255.0);
            output[y * cells.Length * CellSize + cellIndex * CellSize + x] = alpha << 24;
        }
    }

    Console.WriteLine($"{cell.Name}: glyph box x {left}..{right}, y {top}..{bottom}");
}

using var glyphs = new Bitmap(cells.Length * CellSize, CellSize, PixelFormat.Format32bppArgb);
BitmapData data = glyphs.LockBits(new Rectangle(0, 0, glyphs.Width, glyphs.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
Marshal.Copy(output, 0, data.Scan0, output.Length);
glyphs.UnlockBits(data);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
glyphs.Save(args[1], ImageFormat.Png);
Console.WriteLine($"{cells.Length} glyphs of {CellSize}x{CellSize} written to {args[1]}");
return 0;

static int[] ReadPixels(Bitmap bitmap)
{
    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var pixels = new int[bitmap.Width * bitmap.Height];
    Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
    bitmap.UnlockBits(data);
    return pixels;
}

// The button art is grey (R = G = B); the green channel stands for the luminance.
static int Luminance(int argb) => (argb >> 8) & 0xFF;
