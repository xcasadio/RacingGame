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
        ImageResult image;
        using (var stream = File.OpenRead(sourcePath))
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }

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
}
