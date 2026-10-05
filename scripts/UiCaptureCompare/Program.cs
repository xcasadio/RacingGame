// Compares two UI capture runs of RacingGameCasaEngine (--capture-ui-screens): each folder must hold exactly one
// ui-<state>.png per state. Writes report.txt and one side-by-side sheet per state into the output folder.
// Exit codes: 0 = every pair compared, 1 = a state is missing or duplicated or two sizes differ, 2 = bad arguments.
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

string[] states =
[
    "splash", "main-menu", "highscores", "options", "help",
    "car-selection", "track-selection", "race-hud", "pause", "race-finished",
];
const double Threshold = 2.0;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: UiCaptureCompare <reference folder> <after folder> <output folder>");
    return 2;
}

string referenceDirectory = args[0];
string afterDirectory = args[1];
string outputDirectory = args[2];
foreach (string directory in new[] { referenceDirectory, afterDirectory })
{
    if (!Directory.Exists(directory))
    {
        Console.Error.WriteLine($"Folder not found: {directory}");
        return 2;
    }
}

var errors = new List<string>();
foreach (string state in states)
{
    foreach (string directory in new[] { referenceDirectory, afterDirectory })
    {
        string[] candidates = Directory.GetFiles(directory, $"ui-{state}*.png");
        string exact = Path.Combine(directory, $"ui-{state}.png");
        if (!File.Exists(exact))
        {
            errors.Add($"missing state '{state}' in {directory}");
        }
        else if (candidates.Length > 1)
        {
            errors.Add($"duplicate state '{state}' in {directory}: {string.Join(", ", candidates.Select(Path.GetFileName))}");
        }
    }
}

if (errors.Count > 0)
{
    errors.ForEach(error => Console.Error.WriteLine(error));
    return 1;
}

Directory.CreateDirectory(outputDirectory);
var report = new List<string> { "state\tR\tG\tB\tverdict" };
foreach (string state in states)
{
    using var reference = new Bitmap(Path.Combine(referenceDirectory, $"ui-{state}.png"));
    using var after = new Bitmap(Path.Combine(afterDirectory, $"ui-{state}.png"));
    if (reference.Size != after.Size)
    {
        Console.Error.WriteLine($"size mismatch for '{state}': reference {reference.Width}x{reference.Height}, after {after.Width}x{after.Height}");
        return 1;
    }

    (double r, double g, double b) = MeanAbsoluteDifference(reference, after);
    string verdict = r <= Threshold && g <= Threshold && b <= Threshold ? "within" : "ABOVE";
    report.Add(string.Create(CultureInfo.InvariantCulture, $"{state}\t{r:0.00}\t{g:0.00}\t{b:0.00}\t{verdict} {Threshold:0.0}"));
    WriteSheet(reference, after, Path.Combine(outputDirectory, $"compare-{state}.png"), state, r, g, b);
}

File.WriteAllLines(Path.Combine(outputDirectory, "report.txt"), report);
report.ForEach(Console.WriteLine);
return 0;

// Mean absolute difference per R, G, B channel, 8-bit sRGB values (0-255), over the whole image.
static (double R, double G, double B) MeanAbsoluteDifference(Bitmap reference, Bitmap after)
{
    byte[] first = ReadPixels(reference);
    byte[] second = ReadPixels(after);
    long sumB = 0, sumG = 0, sumR = 0;
    for (int index = 0; index < first.Length; index += 4)
    {
        sumB += Math.Abs(first[index] - second[index]);
        sumG += Math.Abs(first[index + 1] - second[index + 1]);
        sumR += Math.Abs(first[index + 2] - second[index + 2]);
    }

    double pixelCount = first.Length / 4.0;
    return (sumR / pixelCount, sumG / pixelCount, sumB / pixelCount);
}

// 32 bpp ARGB pixels in memory order B, G, R, A, rows packed without stride padding.
static byte[] ReadPixels(Bitmap bitmap)
{
    var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try
    {
        int rowBytes = bitmap.Width * 4;
        var pixels = new byte[rowBytes * bitmap.Height];
        for (int row = 0; row < bitmap.Height; row++)
        {
            Marshal.Copy(data.Scan0 + row * data.Stride, pixels, row * rowBytes, rowBytes);
        }

        return pixels;
    }
    finally
    {
        bitmap.UnlockBits(data);
    }
}

// Reference and after side by side at half size, with the state name and the differences.
static void WriteSheet(Bitmap reference, Bitmap after, string path, string state, double r, double g, double b)
{
    int width = reference.Width / 2;
    int height = reference.Height / 2;
    const int Header = 24;
    using var sheet = new Bitmap(width * 2 + 10, height + Header);
    using (Graphics graphics = Graphics.FromImage(sheet))
    {
        graphics.Clear(Color.White);
        graphics.DrawImage(reference, 0, Header, width, height);
        graphics.DrawImage(after, width + 10, Header, width, height);
        using var font = new Font("Segoe UI", 10);
        string caption = string.Create(CultureInfo.InvariantCulture, $"{state}: reference | after   mean |diff| R={r:0.00} G={g:0.00} B={b:0.00}");
        graphics.DrawString(caption, font, Brushes.Black, 4, 4);
    }

    sheet.Save(path, ImageFormat.Png);
}
