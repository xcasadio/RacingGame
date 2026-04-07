using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Graphics;
using CasaEngine.Framework.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Worlds;

internal static partial class LegacyTrackSceneFactory
{
    private static class LegacyTerrainMeshBuilder
    {
        private const int GridWidth = 257;
        private const int GridHeight = 257;
        private const float MapWidthFactor = 10f;
        private const float MapHeightFactor = 10f;
        private const float MapZScale = 300f;

        internal static IReadOnlyList<Entity> CreateEntities(string trackName, Vector3 origin, AssetContentManager assetContentManager)
        {
            string filePath = Path.Combine(GetProjectContentPath(), "LandscapeHeights.data");
            byte[] heights = File.ReadAllBytes(filePath);
            if (heights.Length < GridWidth * GridHeight)
            {
                throw new InvalidOperationException($"Landscape height data '{filePath}' is incomplete.");
            }

            var vertices = BuildVertices(heights, origin);
            uint[] indices = BuildIndices();

            var groundMesh = new StaticModelMesh { Name = $"{trackName}.Ground" };
            groundMesh.SetData(vertices, indices);
            groundMesh.Material = CreateTerrainMaterial(trackName, assetContentManager);

            var groundModel = new StaticModel { Name = $"{trackName}.Ground" };
            groundModel.Meshes.Add(groundMesh);
            groundModel.RootNode = new StaticModelNode
            {
                Name = "Root",
                MeshIndex = 0,
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            };

            return [CreateStaticModelEntity($"Track.Ground.{trackName}", groundModel, Matrix.Identity)];
        }

        private static VertexPositionNormalTextureTangent[] BuildVertices(byte[] heights, Vector3 origin)
        {
            var vertices = new VertexPositionNormalTextureTangent[GridWidth * GridHeight];
            var normalsForSmoothing = new Vector3[GridWidth, GridHeight];
            var tangentsForSmoothing = new Vector3[GridWidth, GridHeight];

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int index = x + y * GridWidth;
                    Vector3 legacyPosition = CalcLandscapePos(x, y, heights);
                    Vector3 edge1 = legacyPosition - CalcLandscapePos(x, y + 1, heights);
                    Vector3 edge2 = legacyPosition - CalcLandscapePos(x + 1, y, heights);
                    Vector3 edge3 = legacyPosition - CalcLandscapePos(x - 1, y + 1, heights);
                    Vector3 edge4 = legacyPosition - CalcLandscapePos(x + 1, y + 1, heights);
                    Vector3 edge5 = legacyPosition - CalcLandscapePos(x - 1, y - 1, heights);

                    Vector3 legacyNormal = Vector3.Normalize(
                        Vector3.Cross(edge2, edge1)
                        + Vector3.Cross(edge4, edge3)
                        + Vector3.Cross(edge3, edge5));
                    Vector3 legacyTangent = Vector3.Normalize(edge1);

                    normalsForSmoothing[x, y] = legacyNormal;
                    tangentsForSmoothing[x, y] = legacyTangent;

                    vertices[index] = new VertexPositionNormalTextureTangent(
                        ConvertLegacyPoint(legacyPosition) - origin,
                        NormalizeOrFallback(ConvertLegacyVector(legacyNormal), Vector3.Up),
                        new Vector2(y / (float)(GridHeight - 1), x / (float)(GridWidth - 1)),
                        new Vector4(NormalizeOrFallback(ConvertLegacyVector(legacyTangent), Vector3.Right), 1f));
                }
            }

            for (int x = 1; x < GridWidth - 1; x++)
            {
                for (int y = 1; y < GridHeight - 1; y++)
                {
                    int index = x + y * GridWidth;
                    Vector3 legacyNormal = normalsForSmoothing[x, y] * 4f;

                    for (int xAdd = -1; xAdd <= 1; xAdd++)
                    {
                        for (int yAdd = -1; yAdd <= 1; yAdd++)
                        {
                            legacyNormal += normalsForSmoothing[x + xAdd, y + yAdd];
                        }
                    }

                    legacyNormal = NormalizeOrFallback(legacyNormal, Vector3.UnitZ);
                    Vector3 legacyTangent = tangentsForSmoothing[x, y];
                    Vector3 helperVector = Vector3.Cross(legacyNormal, legacyTangent);
                    legacyTangent = Vector3.Cross(helperVector, legacyNormal);
                    legacyTangent = NormalizeOrFallback(legacyTangent, Vector3.UnitY);

                    vertices[index].Normal = NormalizeOrFallback(ConvertLegacyVector(legacyNormal), Vector3.Up);
                    vertices[index].Tangent = new Vector4(NormalizeOrFallback(ConvertLegacyVector(legacyTangent), Vector3.Right), 1f);
                }
            }

            return vertices;
        }

        private static uint[] BuildIndices()
        {
            var indices = new uint[(GridWidth - 1) * (GridHeight - 1) * 6];
            int currentIndex = 0;
            for (int x = 0; x < GridWidth - 1; x++)
            {
                for (int y = 0; y < GridHeight - 1; y++)
                {
                    indices[currentIndex + 0] = (uint)(x * GridHeight + y);
                    indices[currentIndex + 1] = (uint)((x + 1) * GridHeight + y);
                    indices[currentIndex + 2] = (uint)((x + 1) * GridHeight + (y + 1));
                    indices[currentIndex + 3] = (uint)((x + 1) * GridHeight + (y + 1));
                    indices[currentIndex + 4] = (uint)(x * GridHeight + (y + 1));
                    indices[currentIndex + 5] = (uint)(x * GridHeight + y);
                    currentIndex += 6;
                }
            }

            return indices;
        }

        private static LitDiffuseMaterial CreateTerrainMaterial(string trackName, AssetContentManager assetContentManager)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "Landscape.tga", "CityGround.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "LandscapeNormal.tga", "CityGroundNormal.tga");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.TerrainMaterial",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(182, 166, 118),
                EmissiveColor = new Vector3(0.01f, 0.01f, 0.008f),
                SpecularColor = new Vector3(0.05f),
                SpecularPower = 6f,
                SamplerState = SamplerState.AnisotropicWrap,
                RasterizerState = RasterizerState.CullCounterClockwise,
            };
        }

        private static Vector3 CalcLandscapePos(int x, int y, byte[] heights)
        {
            int mapX = x < 0 ? 0 : x >= GridWidth ? GridWidth - 1 : x;
            int mapY = y < 0 ? 0 : y >= GridHeight ? GridHeight - 1 : y;
            float heightPercent = heights[mapX + mapY * GridWidth] / 255f;
            return new Vector3(
                x * MapWidthFactor,
                y * MapHeightFactor,
                heightPercent * MapZScale);
        }

        private static Vector3 NormalizeOrFallback(Vector3 vector, Vector3 fallback)
        {
            if (vector.LengthSquared() < 0.0001f)
            {
                return fallback;
            }

            vector.Normalize();
            return vector;
        }
    }
}