using RacingGame.Graphics;
using RacingGame.Shaders;

namespace RacingGame.Landscapes;

/// <summary>
/// Renders the terrain mesh and provides map height queries.
/// </summary>
internal sealed class TerrainRenderer : IDisposable
{
    const int GridWidth = 257,
        GridHeight = 257;

    const float MapWidthFactor = 10,
        MapHeightFactor = 10,
        MapZScale = 300.0f;

    readonly TangentVertex[] vertices = new TangentVertex[GridWidth * GridHeight];
    readonly Material mat = new Material(
        new Color(88, 88, 88),
        new Color(234, 234, 234),
        new Color(33, 33, 33),
        "Landscape",
        "LandscapeNormal",
        "",
        "LandscapeDetail");
    readonly Material cityMat = new Material(
        new Color(32, 32, 32),
        new Color(200, 200, 200),
        new Color(128, 128, 128),
        "CityGround",
        "CityGroundNormal", "", "");
    PlaneRenderer cityPlane = null;
    VertexBuffer vertexBuffer;
    IndexBuffer indexBuffer;
    readonly float[,] mapHeights;

    public Material CityMaterial
    {
        get
        {
            return cityMat;
        }
    }

    public TerrainRenderer()
    {
        byte[] heights = new byte[GridWidth * GridHeight];
        using (Stream file = TitleContainer.OpenStream(
                   "Content\\LandscapeHeights.data"))
        {
            file.Read(heights, 0, GridWidth * GridHeight);
        }

        mapHeights = new float[GridWidth, GridHeight];

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int index = x + y * GridWidth;
                Vector3 pos = CalcLandscapePos(x, y, heights);
                mapHeights[x, y] = pos.Z;
                vertices[index].pos = pos;

                Vector3 edge1 = pos - CalcLandscapePos(x, y + 1, heights);
                Vector3 edge2 = pos - CalcLandscapePos(x + 1, y, heights);
                Vector3 edge3 = pos - CalcLandscapePos(x - 1, y + 1, heights);
                Vector3 edge4 = pos - CalcLandscapePos(x + 1, y + 1, heights);
                Vector3 edge5 = pos - CalcLandscapePos(x - 1, y - 1, heights);

                vertices[index].normal = Vector3.Normalize(
                    Vector3.Cross(edge2, edge1) +
                    Vector3.Cross(edge4, edge3) +
                    Vector3.Cross(edge3, edge5));
                vertices[index].tangent = Vector3.Normalize(edge1);
                vertices[index].uv = new Vector2(
                    y / (float)(GridHeight - 1),
                    x / (float)(GridWidth - 1));
            }
        }

        Vector3[,] normalsForSmoothing = new Vector3[GridWidth, GridHeight];
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int index = x + y * GridWidth;
                normalsForSmoothing[x, y] = vertices[index].normal;
            }
        }

        for (int x = 1; x < GridWidth - 1; x++)
        {
            for (int y = 1; y < GridHeight - 1; y++)
            {
                int index = x + y * GridWidth;
                Vector3 normal = vertices[index].normal * 4;
                for (int xAdd = -1; xAdd <= 1; xAdd++)
                {
                    for (int yAdd = -1; yAdd <= 1; yAdd++)
                    {
                        normal += normalsForSmoothing[x + xAdd, y + yAdd];
                    }
                }

                vertices[index].normal = Vector3.Normalize(normal);
                Vector3 helperVector = Vector3.Cross(
                    vertices[index].normal,
                    vertices[index].tangent);
                vertices[index].tangent = Vector3.Cross(
                    helperVector,
                    vertices[index].normal);
            }
        }

        vertexBuffer = new VertexBuffer(
            BaseGame.Device,
            typeof(TangentVertex),
            vertices.Length,
            BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);

        uint[] indices = new uint[(GridWidth - 1) * (GridHeight - 1) * 6];
        int currentIndex = 0;
        for (int x = 0; x < GridWidth - 1; x++)
        {
            for (int y = 0; y < GridHeight - 1; y++)
            {
                indices[currentIndex + 0] = (uint)(x * GridHeight + y);
                indices[currentIndex + 2] =
                    (uint)((x + 1) * GridHeight + (y + 1));
                indices[currentIndex + 1] = (uint)((x + 1) * GridHeight + y);
                indices[currentIndex + 3] =
                    (uint)((x + 1) * GridHeight + (y + 1));
                indices[currentIndex + 5] = (uint)(x * GridHeight + y);
                indices[currentIndex + 4] = (uint)(x * GridHeight + (y + 1));
                currentIndex += 6;
            }
        }

        indexBuffer = new IndexBuffer(
            BaseGame.Device,
            typeof(uint),
            (GridWidth - 1) * (GridHeight - 1) * 6,
            BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);
    }

    public float GetMapHeight(int x, int y)
    {
        if (x < 0)
        {
            x = 0;
        }

        if (y < 0)
        {
            y = 0;
        }

        if (x >= GridWidth)
        {
            x = GridWidth - 1;
        }

        if (y >= GridHeight)
        {
            y = GridHeight - 1;
        }

        return mapHeights[x, y];
    }

    public float GetMapHeight(float x, float y)
    {
        x /= MapWidthFactor;
        y /= MapHeightFactor;

        int ix = ModulateValueInRange(x, GridWidth - 1);
        int iy = ModulateValueInRange(y, GridHeight - 1);
        float fX = x - ((float)((int)x));
        float fY = y - ((float)((int)y));

        int ix2 = (ix + 1) % (GridWidth - 1);
        int iy2 = (iy + 1) % (GridHeight - 1);

        if (fX + fY < 1)
        {
            return
                mapHeights[ix, iy] +
                fX * (mapHeights[ix2, iy] - mapHeights[ix, iy]) +
                fY * (mapHeights[ix, iy2] - mapHeights[ix, iy]);
        }

        return
            mapHeights[ix2, iy2] +
            (1.0f - fY) * (mapHeights[ix2, iy] - mapHeights[ix2, iy2]) +
            (1.0f - fX) * (mapHeights[ix, iy2] - mapHeights[ix2, iy2]);
    }

    public void UpdateCityPlane(LandscapeObject cityAnchor)
    {
        if (cityAnchor == null)
        {
            cityPlane = null;
            return;
        }

        cityPlane = new PlaneRenderer(
            cityAnchor.Position,
            new Plane(new Vector3(0, 0, 1), 0.1f),
            cityMat, Math.Min(cityAnchor.Position.X, cityAnchor.Position.Y));
    }

    public void Render()
    {
        BaseGame.Device.DepthStencilState = DepthStencilState.Default;
        BaseGame.WorldMatrix = Matrix.Identity;
        ShaderEffect.landscapeNormalMapping.Render(
            mat, "DiffuseWithDetail20",
            new BaseGame.RenderHandler(RenderLandscapeVertices));

        if (cityPlane != null)
        {
            cityPlane.Render();
        }
    }

    public void RenderShadowReceiver()
    {
        ShaderEffect.shadowMapping.UpdateCalcShadowWorldMatrix(Matrix.Identity);
        RenderLandscapeVertices();
    }

    public void Dispose()
    {
        mat.Dispose();
        cityMat.Dispose();
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }

    static int ModulateValueInRange(float val, int max)
    {
        if (val < 0.0f)
        {
            return (max - 1) - ((int)(-val) % max);
        }

        return (int)val % max;
    }

    static Vector3 CalcLandscapePos(int x, int y, byte[] heights)
    {
        int mapX = x < 0 ? 0 : x >= GridWidth ? GridWidth - 1 : x;
        int mapY = y < 0 ? 0 : y >= GridHeight ? GridHeight - 1 : y;
        float heightPercent = heights[mapX + mapY * GridWidth] / 255.0f;

        return new Vector3(
            x * MapWidthFactor,
            y * MapHeightFactor,
            heightPercent * MapZScale);
    }

    void RenderLandscapeVertices()
    {
        BaseGame.Device.SetVertexBuffer(vertexBuffer);
        BaseGame.Device.Indices = indexBuffer;
        BaseGame.Device.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, 0, 0, (GridWidth - 1) * (GridHeight - 1) * 2);
    }
}