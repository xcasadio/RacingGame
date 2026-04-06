using RacingGame.Graphics;

namespace RacingGame.Landscapes;

/// <summary>
/// Placeholder for terrain responsibilities while the facade is frozen.
/// </summary>
internal sealed class TerrainRenderer : IDisposable
{
    public Material CityMaterial
    {
        get
        {
            return null;
        }
    }

    public float GetMapHeight(int x, int y)
    {
        return 0;
    }

    public float GetMapHeight(float x, float y)
    {
        return 0;
    }

    public void UpdateCityPlane(LandscapeObject cityAnchor)
    {
    }

    public void Render()
    {
    }

    public void RenderShadowReceiver()
    {
    }

    public void Dispose()
    {
    }
}