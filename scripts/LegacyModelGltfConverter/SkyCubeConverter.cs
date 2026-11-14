using System.Numerics;

namespace LegacyModelGltfConverter;

/// <summary>
/// Rewrites the legacy sky cube (a DXT1 DDS cube map without mipmaps) in the axes of the converted race world, so that
/// <c>new(P) = old(P.x, P.y, -P.z)</c> for every direction <c>P</c>.
/// </summary>
/// <remarks>
/// The original samples the cube with <c>(x, z, y)</c> of its Z-up world direction; the conversion maps a legacy point to
/// <c>(x, z, -y)</c>, so the converted world direction P corresponds to the cube direction <c>(P.x, P.y, -P.z)</c>
/// (see ADR-0017). A mirror along Z is a permutation of the texels: the +Z and -Z faces swap, the +-X and +-Z faces are
/// flipped horizontally and the +-Y faces vertically. A DXT1 face is a grid of 4 x 4 texel blocks, so the flips reorder
/// the blocks and reverse the rows or the columns of the 2-bit indices of each block; the two colour endpoints of a block
/// do not change and nothing is recompressed: the conversion is lossless, and being a mirror it is its own inverse.
/// The face order and texel conventions are the standard cube map ones (+X, -X, +Y, -Y, +Z, -Z, as DDS stores them and as
/// <c>TextureCubeLoader</c> loads them; row 0 on top).
/// </remarks>
internal static class SkyCubeConverter
{
    private const int HeaderSize = 128;
    private const int BlockSize = 8;
    private const uint DdsMagic = 0x20534444;
    private const uint CubeMapAllFaces = 0x0000FE00;
    private const uint DxtFourCc = 0x31545844; // "DXT1"

    /// <summary>Converts <paramref name="sourcePath"/> into <paramref name="destinationPath"/> and verifies the result.</summary>
    /// <returns>A summary line, or throws <see cref="InvalidDataException"/> when the file is unsupported or the check fails.</returns>
    public static string Convert(string sourcePath, string destinationPath)
    {
        byte[] source = File.ReadAllBytes(sourcePath);
        int size = ReadFaceSize(source);

        byte[] mirrored = Mirror(source, size);
        VerifyAgainstSource(source, mirrored, size);

        // The mirror is an involution: converting the result must give the source back, byte for byte.
        byte[] roundTrip = Mirror(mirrored, size);
        if (!roundTrip.AsSpan().SequenceEqual(source))
        {
            throw new InvalidDataException("A second conversion of the converted cube does not give the source back.");
        }

        File.WriteAllBytes(destinationPath, mirrored);
        return $"{Path.GetFileName(destinationPath)}: {size}x{size} DXT1 cube, Z mirror, {size * size * 6} texels checked against the source, round trip identical to the source ({source.Length} bytes).";
    }

    /// <summary>Mirrors the cube along Z (the DXT1 data only; the 128-byte header is copied as is).</summary>
    internal static byte[] Mirror(byte[] file, int size)
    {
        int blocksPerSide = size / 4;
        int faceBytes = blocksPerSide * blocksPerSide * BlockSize;
        var result = new byte[file.Length];
        Array.Copy(file, result, HeaderSize);

        // (source face, flip horizontally) of each destination face: +X, -X, +Y, -Y, +Z, -Z.
        (int Source, bool FlipHorizontal)[] plan =
        [
            (0, true), (1, true), (2, false), (3, false), (5, true), (4, true),
        ];

        for (int face = 0; face < 6; face++)
        {
            int sourceOffset = HeaderSize + plan[face].Source * faceBytes;
            int destinationOffset = HeaderSize + face * faceBytes;
            for (int blockY = 0; blockY < blocksPerSide; blockY++)
            {
                for (int blockX = 0; blockX < blocksPerSide; blockX++)
                {
                    int sourceBlockX = plan[face].FlipHorizontal ? blocksPerSide - 1 - blockX : blockX;
                    int sourceBlockY = plan[face].FlipHorizontal ? blockY : blocksPerSide - 1 - blockY;
                    int from = sourceOffset + (sourceBlockY * blocksPerSide + sourceBlockX) * BlockSize;
                    int to = destinationOffset + (blockY * blocksPerSide + blockX) * BlockSize;

                    // The two RGB565 endpoints keep their place.
                    result[to] = file[from];
                    result[to + 1] = file[from + 1];
                    result[to + 2] = file[from + 2];
                    result[to + 3] = file[from + 3];

                    // 4 bytes of indices, one per texel row, 2 bits per texel, leftmost texel in the low bits.
                    for (int row = 0; row < 4; row++)
                    {
                        byte indices = file[from + 4 + (plan[face].FlipHorizontal ? row : 3 - row)];
                        result[to + 4 + row] = plan[face].FlipHorizontal ? ReverseIndices(indices) : indices;
                    }
                }
            }
        }

        return result;
    }

    private static byte ReverseIndices(byte indices)
        => (byte)(((indices & 0x03) << 6) | ((indices & 0x0C) << 2) | ((indices & 0x30) >> 2) | ((indices & 0xC0) >> 6));

    private static int ReadFaceSize(byte[] file)
    {
        if (file.Length < HeaderSize || BitConverter.ToUInt32(file, 0) != DdsMagic || BitConverter.ToUInt32(file, 4) != 124)
        {
            throw new InvalidDataException("Not a DDS file.");
        }

        int height = checked((int)BitConverter.ToUInt32(file, 12));
        int width = checked((int)BitConverter.ToUInt32(file, 16));
        uint mipMapCount = BitConverter.ToUInt32(file, 28);
        uint pixelFormatFlags = BitConverter.ToUInt32(file, 80);
        uint fourCc = BitConverter.ToUInt32(file, 84);
        uint caps2 = BitConverter.ToUInt32(file, 112);

        if ((caps2 & CubeMapAllFaces) != CubeMapAllFaces)
        {
            throw new InvalidDataException("The DDS file is not a cube map with six faces.");
        }

        if ((pixelFormatFlags & 0x4) == 0 || fourCc != DxtFourCc)
        {
            throw new InvalidDataException("Only DXT1 cube maps are supported.");
        }

        if (mipMapCount > 1)
        {
            throw new InvalidDataException("A cube map with mipmaps is not supported.");
        }

        if (width != height || width < 4 || width % 4 != 0)
        {
            throw new InvalidDataException($"The faces must be square with a size multiple of 4 ({width}x{height}).");
        }

        int expectedLength = HeaderSize + 6 * (width / 4) * (width / 4) * BlockSize;
        if (file.Length != expectedLength)
        {
            throw new InvalidDataException($"Unexpected DDS length {file.Length}, expected {expectedLength}.");
        }

        return width;
    }

    // Decodes both cubes and checks new(P) == old(P.x, P.y, -P.z) at the centre of every texel of the new cube, with
    // the standard cube map direction <-> texel rules written independently of the block permutation above.
    private static void VerifyAgainstSource(byte[] source, byte[] converted, int size)
    {
        uint[][] oldFaces = DecodeFaces(source, size);
        uint[][] newFaces = DecodeFaces(converted, size);
        int mismatches = 0;
        for (int face = 0; face < 6; face++)
        {
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    Vector3 direction = TexelCenterDirection(face, column, row, size);
                    var mirrored = new Vector3(direction.X, direction.Y, -direction.Z);
                    (int oldFace, int oldColumn, int oldRow) = DirectionToTexel(mirrored, size);
                    if (newFaces[face][row * size + column] != oldFaces[oldFace][oldRow * size + oldColumn])
                    {
                        mismatches++;
                    }
                }
            }
        }

        if (mismatches > 0)
        {
            throw new InvalidDataException($"{mismatches} texel(s) of the converted cube differ from new(P) = old(P.x, P.y, -P.z).");
        }
    }

    // Standard cube map rules (face, sc, tc, ma), row 0 on top.
    private static (int Face, int Column, int Row) DirectionToTexel(Vector3 d, int size)
    {
        float ax = Math.Abs(d.X);
        float ay = Math.Abs(d.Y);
        float az = Math.Abs(d.Z);
        int face;
        float sc;
        float tc;
        float ma;
        if (ax >= ay && ax >= az)
        {
            face = d.X > 0 ? 0 : 1;
            ma = ax;
            sc = d.X > 0 ? -d.Z : d.Z;
            tc = -d.Y;
        }
        else if (ay >= az)
        {
            face = d.Y > 0 ? 2 : 3;
            ma = ay;
            sc = d.X;
            tc = d.Y > 0 ? d.Z : -d.Z;
        }
        else
        {
            face = d.Z > 0 ? 4 : 5;
            ma = az;
            sc = d.Z > 0 ? d.X : -d.X;
            tc = -d.Y;
        }

        int column = Math.Clamp((int)MathF.Floor((sc / ma + 1f) * 0.5f * size), 0, size - 1);
        int row = Math.Clamp((int)MathF.Floor((tc / ma + 1f) * 0.5f * size), 0, size - 1);
        return (face, column, row);
    }

    private static Vector3 TexelCenterDirection(int face, int column, int row, int size)
    {
        float sc = 2f * (column + 0.5f) / size - 1f;
        float tc = 2f * (row + 0.5f) / size - 1f;
        return face switch
        {
            0 => new Vector3(1f, -tc, -sc),
            1 => new Vector3(-1f, -tc, sc),
            2 => new Vector3(sc, 1f, tc),
            3 => new Vector3(sc, -1f, -tc),
            4 => new Vector3(sc, -tc, 1f),
            _ => new Vector3(-sc, -tc, -1f),
        };
    }

    private static uint[][] DecodeFaces(byte[] file, int size)
    {
        int blocksPerSide = size / 4;
        int faceBytes = blocksPerSide * blocksPerSide * BlockSize;
        var faces = new uint[6][];
        for (int face = 0; face < 6; face++)
        {
            var pixels = new uint[size * size];
            for (int blockY = 0; blockY < blocksPerSide; blockY++)
            {
                for (int blockX = 0; blockX < blocksPerSide; blockX++)
                {
                    int offset = HeaderSize + face * faceBytes + (blockY * blocksPerSide + blockX) * BlockSize;
                    ushort color0 = BitConverter.ToUInt16(file, offset);
                    ushort color1 = BitConverter.ToUInt16(file, offset + 2);
                    uint[] palette = BuildPalette(color0, color1);
                    for (int row = 0; row < 4; row++)
                    {
                        byte indices = file[offset + 4 + row];
                        for (int column = 0; column < 4; column++)
                        {
                            pixels[(blockY * 4 + row) * size + blockX * 4 + column] = palette[(indices >> (2 * column)) & 3];
                        }
                    }
                }
            }

            faces[face] = pixels;
        }

        return faces;
    }

    private static uint[] BuildPalette(ushort color0, ushort color1)
    {
        (int r0, int g0, int b0) = Expand565(color0);
        (int r1, int g1, int b1) = Expand565(color1);
        uint Pack(int r, int g, int b, int a = 255) => (uint)(a << 24 | r << 16 | g << 8 | b);

        uint[] palette = new uint[4];
        palette[0] = Pack(r0, g0, b0);
        palette[1] = Pack(r1, g1, b1);
        if (color0 > color1)
        {
            palette[2] = Pack((2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3);
            palette[3] = Pack((r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3);
        }
        else
        {
            palette[2] = Pack((r0 + r1) / 2, (g0 + g1) / 2, (b0 + b1) / 2);
            palette[3] = 0;
        }

        return palette;
    }

    private static (int R, int G, int B) Expand565(ushort color)
    {
        int r = (color >> 11) & 31;
        int g = (color >> 5) & 63;
        int b = color & 31;
        return ((r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2));
    }
}
