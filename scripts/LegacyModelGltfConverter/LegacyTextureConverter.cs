using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StbImageSharp;

namespace LegacyModelGltfConverter;

/// <summary>
/// Converts the legacy .tga textures to .png, the image format glTF accepts.
/// Colour textures are copied losslessly (RGBA, alpha kept); normal maps are rewritten to the standard layout (see
/// <see cref="ConvertToPng"/>). The base file name is preserved.
/// </summary>
internal static class LegacyTextureConverter
{
    /// <param name="isNormalMap">
    /// The legacy shaders read a normal map as ".agb" and build the tangent space as (cross(normal, tangent), tangent,
    /// normal) (NormalMapping.fx, ComputeTangentMatrix): alpha goes along the bitangent, green along the tangent, and
    /// red is unused (saturated). glTF and CasaEngine read red along the tangent and green along
    /// cross(normal, tangent): a normal map is written as (green, alpha, blue) with an opaque alpha.
    /// </param>
    public static void ConvertToPng(string sourcePath, string destinationPath, bool isNormalMap)
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
                    row[i + 1] = isNormalMap ? image.Data[sourceOffset + i + 3] : image.Data[sourceOffset + i + 1];
                    row[i + 2] = isNormalMap ? image.Data[sourceOffset + i + 1] : image.Data[sourceOffset + i];
                    row[i + 3] = isNormalMap ? (byte)255 : image.Data[sourceOffset + i + 3];
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
    /// Decodes both images with StbImageSharp, the decoder of the game, as RGBA bytes and compares the .png with what
    /// <see cref="ConvertToPng"/> writes from the .tga: the same pixels (colour copy) or, with <paramref name="isNormalMap"/>,
    /// the rewritten channels (normal map). Returns null when they are identical, otherwise a description of the first
    /// difference.
    /// </summary>
    public static string? Compare(string tgaPath, string pngPath, bool isNormalMap)
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
            byte r = isNormalMap ? tga.Data[i + 1] : tga.Data[i];
            byte g = isNormalMap ? tga.Data[i + 3] : tga.Data[i + 1];
            byte b = tga.Data[i + 2];
            byte a = isNormalMap ? (byte)255 : tga.Data[i + 3];
            if (r != png.Data[i] || g != png.Data[i + 1] || b != png.Data[i + 2] || a != png.Data[i + 3])
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
