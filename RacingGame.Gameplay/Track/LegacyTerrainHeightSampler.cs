using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Track;

/// <summary>Samples the legacy landscape height map (LandscapeHeights.data), with no GPU object involved.</summary>
internal sealed class LegacyTerrainHeightSampler
{
    private const int GridWidth = 257;
    private const int GridHeight = 257;
    private const float MapWidthFactor = 10f;
    private const float MapHeightFactor = 10f;
    private const float MapZScale = 300f;

    private readonly float[,] _mapHeights;

    private LegacyTerrainHeightSampler(float[,] mapHeights)
    {
        _mapHeights = mapHeights;
    }

    public static LegacyTerrainHeightSampler Load(string filePath)
    {
        byte[] heights = File.ReadAllBytes(filePath);
        if (heights.Length < GridWidth * GridHeight)
        {
            throw new InvalidOperationException($"Landscape height data '{filePath}' is incomplete.");
        }

        var mapHeights = new float[GridWidth, GridHeight];
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int index = x + y * GridWidth;
                float heightPercent = heights[index] / 255f;
                mapHeights[x, y] = heightPercent * MapZScale;
            }
        }

        return new LegacyTerrainHeightSampler(mapHeights);
    }

    public float GetMapHeight(float x, float y)
    {
        x /= MapWidthFactor;
        y /= MapHeightFactor;

        int ix = ModulateValueInRange(x, GridWidth - 1);
        int iy = ModulateValueInRange(y, GridHeight - 1);
        float fractionX = x - (int)x;
        float fractionY = y - (int)y;
        int ix2 = (ix + 1) % (GridWidth - 1);
        int iy2 = (iy + 1) % (GridHeight - 1);

        if (fractionX + fractionY < 1f)
        {
            return _mapHeights[ix, iy]
                + fractionX * (_mapHeights[ix2, iy] - _mapHeights[ix, iy])
                + fractionY * (_mapHeights[ix, iy2] - _mapHeights[ix, iy]);
        }

        return _mapHeights[ix2, iy2]
            + (1f - fractionY) * (_mapHeights[ix2, iy] - _mapHeights[ix2, iy2])
            + (1f - fractionX) * (_mapHeights[ix, iy2] - _mapHeights[ix2, iy2]);
    }

    private static int ModulateValueInRange(float value, int max)
    {
        if (value < 0f)
        {
            return (max - 1) - ((int)(-value) % max);
        }

        return (int)value % max;
    }
}
