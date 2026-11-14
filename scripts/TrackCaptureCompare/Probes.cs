// Probe evaluation of TrackCaptureCompare: named image points, classed lit / shadow / away / away-shadowed,
// measured as the mean colour of a 9x9 square in the reference and in the candidate. See README.md.
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

internal static class ProbeReport
{
    private const int Half = 4; // 9x9 square
    private const double ColorTolerance = 12;
    private const double RatioTolerance = 0.05;
    // A probe is "shadowed" in the candidate when its luminance is below this share of the mean luminance of the
    // candidate's own lit probes of the same surface in the same view.
    private const double ShadowedBelow = 0.6;

    private sealed record Probe(string Name, int X, int Y, string Class, string? Surface, string Note);

    private sealed record Sample(double[] Mean, double StdDev);

    private sealed record Measured(Probe Probe, double[] Reference, double[] Candidate, double ReferenceStd, double CandidateStd);

    public static string Build(string probesPath, List<(string Name, string Reference, string Candidate)> pairs,
        string referenceDirectory, string candidateDirectory, string variant, bool terrainCorrection,
        string? terrainDetailPath, double[] detailFactor)
    {
        var probesByView = new Dictionary<string, List<Probe>>();
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(probesPath)))
        {
            foreach (JsonProperty view in document.RootElement.GetProperty("views").EnumerateObject())
            {
                var list = new List<Probe>();
                foreach (JsonElement p in view.Value.EnumerateArray())
                {
                    string cls = p.GetProperty("class").GetString()!;
                    if (cls is not ("lit" or "shadow" or "away" or "away-shadowed"))
                    {
                        throw new InvalidDataException($"probe class '{cls}' in {view.Name} is not lit, shadow, away or away-shadowed");
                    }

                    list.Add(new Probe(p.GetProperty("name").GetString()!, p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32(), cls,
                        p.TryGetProperty("surface", out JsonElement s) ? s.GetString() : null,
                        p.TryGetProperty("note", out JsonElement n) ? n.GetString() ?? "" : ""));
                }

                probesByView[view.Name] = list;
            }
        }

        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("# Probe report");
        sb.AppendLine();
        sb.AppendLine($"- Reference: `{referenceDirectory}` (variant `{variant}`)");
        sb.AppendLine($"- Candidate: `{candidateDirectory}`");
        sb.AppendLine($"- Probes: `{probesPath}`; colours are the mean of a 9x9 square (full-resolution pixels), std = standard deviation of the luminance in the square.");
        if (terrainCorrection)
        {
            sb.AppendLine(string.Create(inv, $"- Terrain detail correction: reference colour of `surface: terrain` probes divided by the mean of 2 x detail/255 of `{terrainDetailPath}` = (R {detailFactor[0]:0.0000}, G {detailFactor[1]:0.0000}, B {detailFactor[2]:0.0000}), clamped to 255."));
        }
        else
        {
            sb.AppendLine("- Terrain detail correction: disabled (--no-terrain-detail-correction), factor 1.");
        }

        sb.AppendLine(string.Create(inv, $"- Criteria: (1) `lit` and `away`: candidate within +-{ColorTolerance:0} of the reference per channel; (2) `shadow`: candidate shadow/lit luminance ratio within +-{RatioTolerance:0.00} of the reference's, per view and surface; ")
            + string.Create(inv, $"(3) class agreement: a probe is shadowed in the candidate when its luminance is below {ShadowedBelow:0.00} x the mean luminance of the candidate's `lit` probes of the same surface in that view (all `lit` probes of the view when that surface has none); each `shadow` probe must be shadowed and each `lit` probe not shadowed. (4) `away-shadowed`: candidate within +-{ColorTolerance:0} of the reference per channel, like criterion 1."));
        sb.AppendLine();

        var body = new StringBuilder();
        int c1Pass = 0, c1Total = 0, c2Pass = 0, c2Total = 0, c3Pass = 0, c3Total = 0, c4Pass = 0, c4Total = 0;

        foreach (var pair in pairs)
        {
            if (!probesByView.TryGetValue(pair.Name, out List<Probe>? probes))
            {
                continue;
            }

            byte[] reference = ReadPixels(pair.Reference, out int width, out int height);
            byte[] candidate = ReadPixels(pair.Candidate, out _, out _);
            var measured = new List<Measured>();
            foreach (Probe probe in probes)
            {
                if (probe.X < Half || probe.Y < Half || probe.X >= width - Half || probe.Y >= height - Half)
                {
                    throw new InvalidDataException($"probe {pair.Name}/{probe.Name} is too close to the image border");
                }

                Sample r = Measure(reference, width, probe.X, probe.Y);
                Sample c = Measure(candidate, width, probe.X, probe.Y);
                double[] refColor = r.Mean;
                if (terrainCorrection && probe.Surface == "terrain")
                {
                    refColor = refColor.Select((v, i) => Math.Min(255, v / detailFactor[i])).ToArray();
                }

                measured.Add(new Measured(probe, refColor, c.Mean, r.StdDev, c.StdDev));
            }

            body.AppendLine($"## {pair.Name}");
            body.AppendLine();
            body.AppendLine("| Probe | Class | Surface | x,y | Reference RGB (std) | Candidate RGB (std) | Diff RGB | Crit. 1 | Crit. 3 | Crit. 4 |");
            body.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

            foreach (Measured m in measured)
            {
                double[] diff = m.Candidate.Select((v, i) => v - m.Reference[i]).ToArray();
                string crit1 = "-";
                if (m.Probe.Class is "lit" or "away")
                {
                    bool ok = diff.All(d => Math.Abs(d) <= ColorTolerance);
                    crit1 = ok ? "PASS" : "FAIL";
                    c1Total++;
                    c1Pass += ok ? 1 : 0;
                }

                string crit4 = "-";
                if (m.Probe.Class == "away-shadowed")
                {
                    bool ok = diff.All(d => Math.Abs(d) <= ColorTolerance);
                    crit4 = ok ? "PASS" : "FAIL";
                    c4Total++;
                    c4Pass += ok ? 1 : 0;
                }

                string crit3 = "-";
                if (m.Probe.Class is "lit" or "shadow")
                {
                    double? lit = LitLevel(measured, m.Probe.Surface);
                    if (lit is null)
                    {
                        crit3 = "n/a (no lit probe)";
                    }
                    else
                    {
                        bool shadowed = Lum(m.Candidate) < ShadowedBelow * lit.Value;
                        bool ok = shadowed == (m.Probe.Class == "shadow");
                        crit3 = (ok ? "PASS" : "FAIL") + (shadowed ? " (cand. shadowed)" : " (cand. lit)");
                        c3Total++;
                        c3Pass += ok ? 1 : 0;
                    }
                }

                body.AppendLine(string.Create(inv,
                    $"| {m.Probe.Name} | {m.Probe.Class} | {m.Probe.Surface ?? "-"} | {m.Probe.X},{m.Probe.Y} | {Fmt(m.Reference)} ({m.ReferenceStd:0.0}) | {Fmt(m.Candidate)} ({m.CandidateStd:0.0}) | {FmtSigned(diff)} | {crit1} | {crit3} | {crit4} |"));
            }

            body.AppendLine();
            foreach (IGrouping<string, Measured> group in measured.GroupBy(m => m.Probe.Surface ?? "(none)"))
            {
                Measured[] shadows = group.Where(m => m.Probe.Class == "shadow").ToArray();
                Measured[] lits = group.Where(m => m.Probe.Class == "lit").ToArray();
                if (shadows.Length == 0 || lits.Length == 0)
                {
                    continue;
                }

                double refRatio = shadows.Average(m => Lum(m.Reference)) / lits.Average(m => Lum(m.Reference));
                double candRatio = shadows.Average(m => Lum(m.Candidate)) / lits.Average(m => Lum(m.Candidate));
                bool ok = Math.Abs(candRatio - refRatio) <= RatioTolerance;
                c2Total++;
                c2Pass += ok ? 1 : 0;
                body.AppendLine(string.Create(inv,
                    $"- Shadow/lit luminance ratio, surface {group.Key}: reference {refRatio:0.000}, candidate {candRatio:0.000}, difference {candRatio - refRatio:+0.000;-0.000;0.000} -> criterion 2 {(ok ? "PASS" : "FAIL")}"));
            }

            body.AppendLine();
            foreach (Measured m in measured.Where(m => !string.IsNullOrEmpty(m.Probe.Note)))
            {
                body.AppendLine($"- `{m.Probe.Name}` ({m.Probe.Class}): {m.Probe.Note}");
            }

            body.AppendLine();
        }

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Criterion | Passed | Judged |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine(string.Create(inv, $"| 1. lit and away within +-{ColorTolerance:0} | {c1Pass} | {c1Total} |"));
        sb.AppendLine(string.Create(inv, $"| 2. shadow/lit ratio within +-{RatioTolerance:0.00} (per view and surface) | {c2Pass} | {c2Total} |"));
        sb.AppendLine($"| 3. class agreement (shadow shadowed, lit lit) | {c3Pass} | {c3Total} |");
        sb.AppendLine(string.Create(inv, $"| 4. away-shadowed within +-{ColorTolerance:0} | {c4Pass} | {c4Total} |"));
        sb.AppendLine();
        sb.Append(body);
        return sb.ToString();
    }

    // Per-channel mean (R, G, B) of 2 x detail / 255.
    public static double[] MeanDoubleDetail(string path)
    {
        (byte[] bgra, int count) = path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ? ReadTga(path) : ReadBitmapBgra(path);
        double b = 0, g = 0, r = 0;
        for (int i = 0; i < count; i++)
        {
            b += bgra[i * 4];
            g += bgra[i * 4 + 1];
            r += bgra[i * 4 + 2];
        }

        return new[] { 2 * r / count / 255, 2 * g / count / 255, 2 * b / count / 255 };
    }

    private static double? LitLevel(List<Measured> measured, string? surface)
    {
        Measured[] lits = measured.Where(m => m.Probe.Class == "lit" && m.Probe.Surface == surface).ToArray();
        if (lits.Length == 0)
        {
            lits = measured.Where(m => m.Probe.Class == "lit").ToArray();
        }

        return lits.Length == 0 ? null : lits.Average(m => Lum(m.Candidate));
    }

    private static double Lum(double[] rgb) => 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2];

    private static string Fmt(double[] c) => string.Create(CultureInfo.InvariantCulture, $"{c[0]:0},{c[1]:0},{c[2]:0}");

    private static string FmtSigned(double[] c) => string.Create(CultureInfo.InvariantCulture, $"{c[0]:+0;-0;0},{c[1]:+0;-0;0},{c[2]:+0;-0;0}");

    private static Sample Measure(byte[] pixels, int width, int cx, int cy)
    {
        double r = 0, g = 0, b = 0, l = 0, l2 = 0;
        int n = 0;
        for (int y = cy - Half; y <= cy + Half; y++)
        {
            for (int x = cx - Half; x <= cx + Half; x++)
            {
                int i = (y * width + x) * 4;
                double lum = 0.299 * pixels[i + 2] + 0.587 * pixels[i + 1] + 0.114 * pixels[i];
                r += pixels[i + 2];
                g += pixels[i + 1];
                b += pixels[i];
                l += lum;
                l2 += lum * lum;
                n++;
            }
        }

        double mean = l / n;
        return new Sample(new[] { r / n, g / n, b / n }, Math.Sqrt(Math.Max(0, l2 / n - mean * mean)));
    }

    private static byte[] ReadPixels(string path, out int width, out int height)
    {
        using var bitmap = new Bitmap(path);
        width = bitmap.Width;
        height = bitmap.Height;
        return ToBgra(bitmap).Pixels;
    }

    private static (byte[] Pixels, int Count) ReadBitmapBgra(string path)
    {
        using var bitmap = new Bitmap(path);
        return ToBgra(bitmap);
    }

    private static (byte[] Pixels, int Count) ToBgra(Bitmap bitmap)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int rowBytes = bitmap.Width * 4;
            var pixels = new byte[rowBytes * bitmap.Height];
            for (int row = 0; row < bitmap.Height; row++)
            {
                Marshal.Copy(data.Scan0 + row * data.Stride, pixels, row * rowBytes, rowBytes);
            }

            return (pixels, bitmap.Width * bitmap.Height);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    // Minimal TGA reader: true-colour 24 or 32 bpp, uncompressed (type 2) or RLE (type 10). Returns BGRA pixels
    // in file order (row order is irrelevant for a mean).
    private static (byte[] Pixels, int Count) ReadTga(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        int idLength = file[0], type = file[2], width = file[12] | file[13] << 8, height = file[14] | file[15] << 8, bpp = file[16];
        if (file[1] != 0 || (type != 2 && type != 10) || (bpp != 24 && bpp != 32))
        {
            throw new NotSupportedException($"unsupported TGA (colour map {file[1]}, type {type}, {bpp} bpp): only true-colour 24/32 bpp, raw or RLE");
        }

        int bytesPerPixel = bpp / 8, count = width * height, position = 18 + idLength;
        var pixels = new byte[count * 4];
        for (int p = 0; p < count;)
        {
            int run = 1;
            bool repeat = false;
            if (type == 10)
            {
                int header = file[position++];
                run = (header & 0x7F) + 1;
                repeat = (header & 0x80) != 0;
            }

            for (int k = 0; k < run && p < count; k++, p++)
            {
                int source = position + (repeat ? 0 : k * bytesPerPixel);
                pixels[p * 4] = file[source];
                pixels[p * 4 + 1] = file[source + 1];
                pixels[p * 4 + 2] = file[source + 2];
                pixels[p * 4 + 3] = bytesPerPixel == 4 ? file[source + 3] : (byte)255;
            }

            position += repeat ? bytesPerPixel : run * bytesPerPixel;
        }

        return (pixels, count);
    }
}
