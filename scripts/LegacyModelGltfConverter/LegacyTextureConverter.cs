using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StbImageSharp;

namespace LegacyModelGltfConverter;

/// <summary>
/// Converts the legacy .tga textures to .png, the image format glTF accepts.
/// Pixels are copied losslessly (RGBA, alpha kept) and the base file name is preserved.
/// </summary>
internal static class LegacyTextureConverter
{
    public static void ConvertToPng(string sourcePath, string destinationPath)
    {
        ImageResult image = Decode(sourcePath);

        using var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        var rectangle = new Rectangle(0, 0, image.Width, image.Height);
        BitmapData data = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            // GDI+ stores 32bpp ARGB pixels as B, G, R, A bytes.
            var row = new byte[image.Width * 4];
            for (int y = 0; y < image.Height; y++)
            {
                int sourceOffset = y * image.Width * 4;
                for (int x = 0; x < image.Width; x++)
                {
                    int i = x * 4;
                    row[i] = image.Data[sourceOffset + i + 2];
                    row[i + 1] = image.Data[sourceOffset + i + 1];
                    row[i + 2] = image.Data[sourceOffset + i];
                    row[i + 3] = image.Data[sourceOffset + i + 3];
                }

                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        bitmap.Save(destinationPath, ImageFormat.Png);
    }

    /// <summary>
    /// Decodes both images with StbImageSharp, the decoder of the game, as RGBA bytes and compares them.
    /// Returns null when they are identical, otherwise a description of the first difference.
    /// </summary>
    public static string? Compare(string tgaPath, string pngPath)
    {
        ImageResult tga = Decode(tgaPath);
        ImageResult png = Decode(pngPath);
        if (tga.Width != png.Width || tga.Height != png.Height)
        {
            return $"size {tga.Width}x{tga.Height} against {png.Width}x{png.Height}";
        }

        int differentPixels = 0;
        int firstDifference = -1;
        for (int i = 0; i < tga.Data.Length; i += 4)
        {
            if (tga.Data[i] != png.Data[i] || tga.Data[i + 1] != png.Data[i + 1]
                || tga.Data[i + 2] != png.Data[i + 2] || tga.Data[i + 3] != png.Data[i + 3])
            {
                differentPixels++;
                if (firstDifference < 0)
                {
                    firstDifference = i / 4;
                }
            }
        }

        if (differentPixels == 0)
        {
            return null;
        }

        return $"{differentPixels} pixels differ, first at ({firstDifference % tga.Width}, {firstDifference / tga.Width})";
    }

    private static ImageResult Decode(string path)
    {
        using var stream = File.OpenRead(path);
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }
}
