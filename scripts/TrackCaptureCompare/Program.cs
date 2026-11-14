// Compares the 3D reference captures of the original RacingGame (RacingGame.exe --capture-track-audit) with a
// candidate run: either the CasaEngine port replaying the same views (RacingGameCasaEngine.exe
// --capture-reference-views) or a second run of the original (determinism check).
// The pairs come from views.json. The original names its files <Track>-<view>-<variant>.png and the port
// <Track>-<view>.png; either folder may hold either kind.
// Writes report.md and, per pair, compare-<Track>-<view>.png: reference | candidate on top, difference x4 | edges
// below (reference edges in red, candidate edges in green, common edges in yellow).
// Exit codes: 0 = every pair compared, 1 = a file is missing or two sizes differ, 2 = bad arguments.
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

const int DifferentPixelThreshold = 16;
const int EdgeThreshold = 96;

string variant = "nopost";
string? probesPath = null;
string? terrainDetailPath = null;
bool terrainCorrection = true;
var positional = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--variant" && i + 1 < args.Length)
    {
        variant = args[++i];
    }
    else if (args[i] == "--probes" && i + 1 < args.Length)
    {
        probesPath = args[++i];
    }
    else if (args[i] == "--terrain-detail" && i + 1 < args.Length)
    {
        terrainDetailPath = args[++i];
    }
    else if (args[i] == "--no-terrain-detail-correction")
    {
        terrainCorrection = false;
    }
    else
    {
        positional.Add(args[i]);
    }
}

if (positional.Count != 4)
{
    Console.Error.WriteLine("Usage: TrackCaptureCompare <views.json> <reference folder> <candidate folder> <output folder> [--variant full|nopost] [--probes <probes.json> [--terrain-detail <tga|png> | --no-terrain-detail-correction]]");
    return 2;
}

string viewsPath = positional[0];
string referenceDirectory = positional[1];
string candidateDirectory = positional[2];
string outputDirectory = positional[3];
if (!File.Exists(viewsPath) || !Directory.Exists(referenceDirectory) || !Directory.Exists(candidateDirectory))
{
    Console.Error.WriteLine("views.json, the reference folder or the candidate folder was not found.");
    return 2;
}

if (probesPath is not null && !File.Exists(probesPath))
{
    Console.Error.WriteLine($"probes file not found: {probesPath}");
    return 2;
}

if (probesPath is not null && terrainCorrection && (terrainDetailPath is null || !File.Exists(terrainDetailPath)))
{
    Console.Error.WriteLine("--probes needs --terrain-detail <existing file> or --no-terrain-detail-correction.");
    return 2;
}

var pairs = new List<(string Name, string Reference, string Candidate)>();
using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(viewsPath)))
{
    foreach (JsonElement track in document.RootElement.GetProperty("tracks").EnumerateArray())
    {
        string trackName = track.GetProperty("name").GetString()!;
        foreach (JsonElement view in track.GetProperty("views").EnumerateArray())
        {
            string viewName = view.GetProperty("name").GetString()!;
            // Original runs name their files <Track>-<view>-<variant>.png, port runs <Track>-<view>.png: either
            // folder may hold either kind, so two runs of the same game can be compared too.
            string variantFile = $"{trackName}-{viewName}-{variant}.png";
            string plainFile = $"{trackName}-{viewName}.png";
            string referenceFile = File.Exists(Path.Combine(referenceDirectory, variantFile)) ? variantFile : plainFile;
            string candidateFile = File.Exists(Path.Combine(candidateDirectory, plainFile)) ? plainFile : variantFile;

            pairs.Add(($"{trackName}-{viewName}", Path.Combine(referenceDirectory, referenceFile), Path.Combine(candidateDirectory, candidateFile)));
        }
    }
}

List<string> missing = pairs.SelectMany(pair => new[] { pair.Reference, pair.Candidate }).Where(path => !File.Exists(path)).ToList();
if (missing.Count > 0)
{
    missing.ForEach(path => Console.Error.WriteLine($"missing file: {path}"));
    return 1;
}

Directory.CreateDirectory(outputDirectory);
var report = new List<string>
{
    "# Track capture comparison",
    "",
    $"- Reference: `{referenceDirectory}` (variant `{variant}`)",
    $"- Candidate: `{candidateDirectory}`",
    "",
    "Mean absolute difference per channel (0-255), share of pixels whose largest channel difference exceeds "
        + $"{DifferentPixelThreshold}, mean luminance of each image, and share of the reference edges also found within "
        + "2 pixels in the candidate.",
    "",
    "| View | R | G | B | Different pixels | Luminance ref | Luminance cand | Edges matched |",
    "|---|---|---|---|---|---|---|---|",
};

foreach ((string name, string referencePath, string candidatePath) in pairs)
{
    using var reference = new Bitmap(referencePath);
    using var candidate = new Bitmap(candidatePath);
    if (reference.Size != candidate.Size)
    {
        Console.Error.WriteLine($"size mismatch for '{name}': reference {reference.Width}x{reference.Height}, candidate {candidate.Width}x{candidate.Height}");
        return 1;
    }

    int width = reference.Width;
    int height = reference.Height;
    byte[] first = ReadPixels(reference);
    byte[] second = ReadPixels(candidate);
    byte[] difference = new byte[first.Length];
    long sumB = 0, sumG = 0, sumR = 0, differentPixels = 0;
    double luminanceFirst = 0, luminanceSecond = 0;
    for (int index = 0; index < first.Length; index += 4)
    {
        int db = Math.Abs(first[index] - second[index]);
        int dg = Math.Abs(first[index + 1] - second[index + 1]);
        int dr = Math.Abs(first[index + 2] - second[index + 2]);
        sumB += db;
        sumG += dg;
        sumR += dr;
        if (Math.Max(db, Math.Max(dg, dr)) > DifferentPixelThreshold)
        {
            differentPixels++;
        }

        luminanceFirst += Luminance(first, index);
        luminanceSecond += Luminance(second, index);
        difference[index] = (byte)Math.Min(255, db * 4);
        difference[index + 1] = (byte)Math.Min(255, dg * 4);
        difference[index + 2] = (byte)Math.Min(255, dr * 4);
        difference[index + 3] = 255;
    }

    double pixelCount = first.Length / 4.0;
    bool[] firstEdges = Edges(first, width, height);
    bool[] secondEdges = Edges(second, width, height);
    byte[] edges = new byte[first.Length];
    long referenceEdgeCount = 0, matchedEdgeCount = 0;
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int pixel = y * width + x;
            if (firstEdges[pixel])
            {
                referenceEdgeCount++;
                if (HasEdgeNearby(secondEdges, width, height, x, y))
                {
                    matchedEdgeCount++;
                }
            }

            edges[pixel * 4] = 0;
            edges[pixel * 4 + 1] = secondEdges[pixel] ? (byte)255 : (byte)0;
            edges[pixel * 4 + 2] = firstEdges[pixel] ? (byte)255 : (byte)0;
            edges[pixel * 4 + 3] = 255;
        }
    }

    double edgesMatched = referenceEdgeCount == 0 ? 100 : 100.0 * matchedEdgeCount / referenceEdgeCount;
    report.Add(string.Create(CultureInfo.InvariantCulture,
        $"| {name} | {sumR / pixelCount:0.00} | {sumG / pixelCount:0.00} | {sumB / pixelCount:0.00} | {100.0 * differentPixels / pixelCount:0.0} % | {luminanceFirst / pixelCount:0.0} | {luminanceSecond / pixelCount:0.0} | {edgesMatched:0.0} % |"));

    using Bitmap differenceBitmap = ToBitmap(difference, width, height);
    using Bitmap edgesBitmap = ToBitmap(edges, width, height);
    WriteSheet(reference, candidate, differenceBitmap, edgesBitmap, Path.Combine(outputDirectory, $"compare-{name}.png"), name);
}

File.WriteAllLines(Path.Combine(outputDirectory, "report.md"), report);
report.ForEach(Console.WriteLine);

if (probesPath is not null)
{
    double[] detailFactor = terrainCorrection ? ProbeReport.MeanDoubleDetail(terrainDetailPath!) : new[] { 1.0, 1.0, 1.0 };
    string probeReport = ProbeReport.Build(probesPath, pairs, referenceDirectory, candidateDirectory, variant, terrainCorrection, terrainDetailPath, detailFactor);
    File.WriteAllText(Path.Combine(outputDirectory, "probes.md"), probeReport);
    Console.WriteLine();
    Console.WriteLine(probeReport);
}
return 0;

static double Luminance(byte[] pixels, int index)
{
    return 0.114 * pixels[index] + 0.587 * pixels[index + 1] + 0.299 * pixels[index + 2];
}

// Sobel gradient of the luminance, thresholded.
static bool[] Edges(byte[] pixels, int width, int height)
{
    var luminance = new float[width * height];
    for (int pixel = 0; pixel < luminance.Length; pixel++)
    {
        luminance[pixel] = (float)Luminance(pixels, pixel * 4);
    }

    var edges = new bool[width * height];
    for (int y = 1; y < height - 1; y++)
    {
        for (int x = 1; x < width - 1; x++)
        {
            int p = y * width + x;
            float gx = luminance[p - width + 1] + 2 * luminance[p + 1] + luminance[p + width + 1]
                - luminance[p - width - 1] - 2 * luminance[p - 1] - luminance[p + width - 1];
            float gy = luminance[p + width - 1] + 2 * luminance[p + width] + luminance[p + width + 1]
                - luminance[p - width - 1] - 2 * luminance[p - width] - luminance[p - width + 1];
            edges[p] = Math.Abs(gx) + Math.Abs(gy) > EdgeThreshold;
        }
    }

    return edges;
}

// True when the other image has an edge within 2 pixels.
static bool HasEdgeNearby(bool[] edges, int width, int height, int x, int y)
{
    for (int dy = -2; dy <= 2; dy++)
    {
        for (int dx = -2; dx <= 2; dx++)
        {
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < width && ny < height && edges[ny * width + nx])
            {
                return true;
            }
        }
    }

    return false;
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

static Bitmap ToBitmap(byte[] pixels, int width, int height)
{
    var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
    try
    {
        for (int row = 0; row < height; row++)
        {
            Marshal.Copy(pixels, row * width * 4, data.Scan0 + row * data.Stride, width * 4);
        }
    }
    finally
    {
        bitmap.UnlockBits(data);
    }

    return bitmap;
}

// Four half-size panels: reference | candidate on top, difference x4 | edges below.
static void WriteSheet(Bitmap reference, Bitmap candidate, Bitmap difference, Bitmap edges, string path, string name)
{
    int width = reference.Width / 2;
    int height = reference.Height / 2;
    const int Header = 24;
    const int Gap = 10;
    using var sheet = new Bitmap(width * 2 + Gap, (height + Header) * 2);
    using (Graphics graphics = Graphics.FromImage(sheet))
    {
        graphics.Clear(Color.White);
        using var font = new Font("Segoe UI", 10);
        graphics.DrawString($"{name}: reference (original)", font, Brushes.Black, 4, 4);
        graphics.DrawString("candidate", font, Brushes.Black, width + Gap + 4, 4);
        graphics.DrawImage(reference, 0, Header, width, height);
        graphics.DrawImage(candidate, width + Gap, Header, width, height);
        int secondRow = height + Header;
        graphics.DrawString("difference x4", font, Brushes.Black, 4, secondRow + 4);
        graphics.DrawString("edges: reference red, candidate green, both yellow", font, Brushes.Black, width + Gap + 4, secondRow + 4);
        graphics.DrawImage(difference, 0, secondRow + Header, width, height);
        graphics.DrawImage(edges, width + Gap, secondRow + Header, width, height);
    }

    sheet.Save(path, ImageFormat.Png);
}
