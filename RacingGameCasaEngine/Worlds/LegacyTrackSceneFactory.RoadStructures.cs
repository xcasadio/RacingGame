using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Graphics;
using CasaEngine.Framework.Materials;
using CasaEngine.Framework.Rendering.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Bootstrap;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Worlds;

internal static partial class LegacyTrackSceneFactory
{
    /// <summary>
    /// The three static pieces of geometry of the original that are neither the road, the rails, the columns nor the
    /// terrain: the underside of the road (Track.cs, roadBackVertices), the tunnels (roadTunnelVertices) and the
    /// city ground plane (Landscape.CreateCityPlane + PlaneRenderer).
    /// </summary>
    private static class LegacyRoadStructureBuilder
    {
        // Track.cs: RoadBackHullTextureWidthFactor, RoadBackSideTextureHeight, RoadTunnelTextureWidthFactor,
        // RoadTunnelSideTextureHeight; TrackVertex.cs: RoadThickness, RoadTunnelHeight.
        private const float RoadBackHullTextureWidthFactor = 1.0f;
        private const float RoadBackSideTextureHeight = 0.135f;
        private const float RoadTunnelTextureWidthFactor = 0.25f;
        private const float RoadTunnelSideTextureHeight = 0.235f;
        private const float RoadThickness = 1.0f;
        private const float RoadTunnelHeight = 7.125f;

        // PlaneRenderer.cs: Tiling; Landscape.CreateCityPlane: new Plane((0, 0, 1), 0.1f), the plane's D is the height.
        private const float CityPlaneTiling = 20.0f;
        private const float CityPlaneHeight = 0.1f;

        // The original creates its city plane once, when the Landscape is constructed for the Beginner level, and
        // never again for the shipped terrain (Landscape ctor, ApplyTerrain): all three tracks share that plane.
        private static Vector3? _cityPlaneAnchor;
        private static bool _cityPlaneAnchorSearched;

        internal static List<Entity> CreateEntities(
            string trackName,
            IReadOnlyList<LegacyTrackPoint> roadPoints,
            IReadOnlyList<LegacyRoadHelper> roadHelpers,
            Vector3 origin,
            LegacyTerrainHeightSampler terrainSampler,
            AssetContentManager assetContentManager)
        {
            var entities = new List<Entity>();
            entities.Add(CreateRoadBackEntity(trackName, roadPoints, origin, assetContentManager));

            Entity? tunnelEntity = CreateTunnelEntity(trackName, roadPoints, roadHelpers, origin, assetContentManager);
            if (tunnelEntity != null)
            {
                entities.Add(tunnelEntity);
            }

            Entity? cityGroundEntity = CreateCityGroundEntity(trackName, origin, terrainSampler, assetContentManager);
            if (cityGroundEntity != null)
            {
                entities.Add(cityGroundEntity);
            }

            return entities;
        }

        // Track.cs, GenerateVerticesAndObjects: 4 vertices per road point (left edge, bottom left, bottom right,
        // right edge), 6 triangles per segment (the last point duplicates the first one, so points.Count - 1 segments).
        private static Entity CreateRoadBackEntity(string trackName, IReadOnlyList<LegacyTrackPoint> roadPoints, Vector3 origin, AssetContentManager assetContentManager)
        {
            var vertices = new VertexPositionNormalTextureTangent[roadPoints.Count * 4];
            for (int index = 0; index < roadPoints.Count; index++)
            {
                LegacyTrackPoint point = roadPoints[index];
                Vector3 right = GetLegacyRawRight(point);
                Vector3 halfWidth = right * (LegacyRoadWidthScale * point.RoadWidth / 2.0f);
                Vector3 thickness = point.Up * (RoadThickness * point.RoadWidth);
                float u = point.TextureU * RoadBackHullTextureWidthFactor;
                int vertexIndex = index * 4;

                // TrackVertex.LeftTangentVertex, BottomLeftSideTangentVertex, BottomRightSideTangentVertex, RightTangentVertex
                vertices[vertexIndex + 0] = CreateStructureVertex(point.Position - halfWidth, point.Up, right, u, 0.0f, origin);
                vertices[vertexIndex + 1] = CreateStructureVertex(point.Position - halfWidth - thickness, -point.Up, -right, u, RoadBackSideTextureHeight, origin);
                vertices[vertexIndex + 2] = CreateStructureVertex(point.Position + halfWidth - thickness, -point.Up, -right, u, 1.0f - RoadBackSideTextureHeight, origin);
                vertices[vertexIndex + 3] = CreateStructureVertex(point.Position + halfWidth, point.Up, right, u, 1.0f, origin);
            }

            var indices = new uint[(roadPoints.Count - 1) * 6 * 3];
            int outputIndex = 0;
            int segmentStartVertex = 0;
            for (int segment = 0; segment < roadPoints.Count - 1; segment++)
            {
                AddStripIndices(indices, ref outputIndex, segmentStartVertex);
                segmentStartVertex += 4;
            }

            var mesh = new StaticModelMesh { Name = $"{trackName}.RoadBack" };
            mesh.SetData(vertices, indices);
            mesh.Material = CreateRoadBackMaterial(trackName, assetContentManager);

            Logs.WriteInfo($"Track '{trackName}' road back: {vertices.Length} vertices, {indices.Length} indices.");

            // Track.GenerateShadow ignores the road back ("Generate shadows for the road and the tunnel, ignore the
            // road back") and Track.UseShadow does not render it.
            return CreateStructureEntity($"Track.RoadBack.{trackName}", mesh, castShadows: false);
        }

        // Track.cs: the vertices of the tunnel parts (left edge of the road, top left, top right, right edge, 4 per
        // road point of a Tunnel range), the same 6 triangles per segment, rendered with culling off.
        private static Entity? CreateTunnelEntity(
            string trackName,
            IReadOnlyList<LegacyTrackPoint> roadPoints,
            IReadOnlyList<LegacyRoadHelper> roadHelpers,
            Vector3 origin,
            AssetContentManager assetContentManager)
        {
            List<LegacyRoadHelperPosition> tunnelRanges = BuildRoadHelperPositions(roadPoints, roadHelpers)
                .Where(static range => range.Type == LegacyRoadHelperType.Tunnel)
                .ToList();

            int totalTunnelPoints = 0;
            int totalSegments = 0;
            foreach (LegacyRoadHelperPosition range in tunnelRanges)
            {
                totalTunnelPoints += 1 + (range.EndIndex - range.StartIndex);
                totalSegments += range.EndIndex - range.StartIndex;
            }

            if (totalTunnelPoints == 0)
            {
                Logs.WriteInfo($"Track '{trackName}' tunnels: 0 ranges, 0 vertices, 0 indices.");
                return null;
            }

            var vertices = new VertexPositionNormalTextureTangent[totalTunnelPoints * 4];
            var indices = new uint[totalSegments * 6 * 3];
            int vertexIndex = 0;
            int outputIndex = 0;
            foreach (LegacyRoadHelperPosition range in tunnelRanges)
            {
                int rangeStartVertex = vertexIndex;
                for (int pointIndex = range.StartIndex; pointIndex <= range.EndIndex; pointIndex++)
                {
                    LegacyTrackPoint point = roadPoints[pointIndex];
                    Vector3 right = GetLegacyRawRight(point);
                    Vector3 halfWidth = right * (LegacyRoadWidthScale * point.RoadWidth / 2.0f);
                    Vector3 height = point.Up * RoadTunnelHeight;
                    float u = point.TextureU * RoadTunnelTextureWidthFactor;

                    // TrackVertex.LeftTangentVertex, TunnelTopLeftSideTangentVertex, TunnelTopRightSideTangentVertex,
                    // RightTangentVertex, whose normals and tangents all end up as -up / -right.
                    vertices[vertexIndex + 0] = CreateStructureVertex(point.Position - halfWidth, -point.Up, -right, u, 0.0f, origin);
                    vertices[vertexIndex + 1] = CreateStructureVertex(point.Position - halfWidth + height, -point.Up, -right, u, RoadTunnelSideTextureHeight, origin);
                    vertices[vertexIndex + 2] = CreateStructureVertex(point.Position + halfWidth + height, -point.Up, -right, u, 1.0f - RoadTunnelSideTextureHeight, origin);
                    vertices[vertexIndex + 3] = CreateStructureVertex(point.Position + halfWidth, -point.Up, -right, u, 1.0f, origin);
                    vertexIndex += 4;
                }

                int segmentStartVertex = rangeStartVertex;
                for (int pointIndex = range.StartIndex; pointIndex < range.EndIndex; pointIndex++)
                {
                    AddStripIndices(indices, ref outputIndex, segmentStartVertex);
                    segmentStartVertex += 4;
                }
            }

            var mesh = new StaticModelMesh { Name = $"{trackName}.RoadTunnel" };
            mesh.SetData(vertices, indices);
            mesh.Material = CreateRoadTunnelMaterial(trackName, assetContentManager);

            Logs.WriteInfo($"Track '{trackName}' tunnels: {tunnelRanges.Count} range(s), {vertices.Length} vertices, {indices.Length} indices.");

            // Track.GenerateShadow and Track.UseShadow both render the tunnels.
            return CreateStructureEntity($"Track.RoadTunnel.{trackName}", mesh, castShadows: true);
        }

        // Landscape.CreateCityPlane + PlaneRenderer.DrawPlaneVertices: one quad of half size min(x, y) of the first big
        // building, centered on it, 0.1 above its height, the texture tiled every 20 units of half size.
        private static Entity? CreateCityGroundEntity(string trackName, Vector3 origin, LegacyTerrainHeightSampler terrainSampler, AssetContentManager assetContentManager)
        {
            Vector3? anchor = GetCityPlaneAnchor(terrainSampler, assetContentManager);
            if (anchor == null)
            {
                Logs.WriteInfo($"Track '{trackName}' city ground: no big building, no plane.");
                return null;
            }

            Vector3 center = anchor.Value;
            float size = Math.Min(center.X, center.Y);
            float uvExtent = size / CityPlaneTiling;

            // Triangle strip of the original: (-,-) (-,+) (+,-) (+,+)
            var vertices = new VertexPositionNormalTextureTangent[4];
            for (int corner = 0; corner < 4; corner++)
            {
                float signX = corner < 2 ? -1.0f : 1.0f;
                float signY = (corner & 1) == 0 ? -1.0f : 1.0f;
                Vector3 legacyPosition = center + new Vector3(signX * size, signY * size, CityPlaneHeight);
                vertices[corner] = CreateStructureVertex(
                    legacyPosition,
                    new Vector3(0.0f, 0.0f, 1.0f),
                    new Vector3(1.0f, 0.0f, 0.0f),
                    signX * uvExtent,
                    signY * uvExtent,
                    origin);
            }

            // Triangle strip (0, 1, 2), (2, 1, 3)
            uint[] indices = [0, 1, 2, 2, 1, 3];

            var mesh = new StaticModelMesh { Name = $"{trackName}.CityGround" };
            mesh.SetData(vertices, indices);
            mesh.Material = CreateCityGroundMaterial(trackName, assetContentManager);

            Logs.WriteInfo($"Track '{trackName}' city ground: {vertices.Length} vertices, {indices.Length} indices, center ({center.X:F2}, {center.Y:F2}, {center.Z:F2}) legacy, half size {size:F2}.");

            // Landscape.GenerateShadow: the landscape (and its plane) only receives shadows.
            return CreateStructureEntity($"Track.CityGround.{trackName}", mesh, castShadows: false);
        }

        // The Landscape of the original is constructed for Level.Beginner (RacingGameManager) and its city plane is
        // made right then, at the first Hotel/Building of that level; later LoadLevel calls keep it.
        private static Vector3? GetCityPlaneAnchor(LegacyTerrainHeightSampler terrainSampler, AssetContentManager assetContentManager)
        {
            if (_cityPlaneAnchorSearched)
            {
                return _cityPlaneAnchor;
            }

            // The objects of the track file come first in the landscape of the original (TrackLine.Load), and the first
            // big building of the Beginner level is among them.
            LegacyTrackLayout beginnerLayout = LoadTrackLayout(RaceBootstrapAssets.TrackBeginnerData);
            var landscape = new LegacyLandscape(Vector3.Zero, terrainSampler, assetContentManager, new Random(0));
            var discardedEntities = new LegacyEntityGroup(static (modelName, index) => $"{modelName}.{index}");
            foreach (LegacyNeutralObject neutralObject in beginnerLayout.NeutralsObjects)
            {
                landscape.AddObjectToRender(neutralObject.modelName, neutralObject.matrix, discardedEntities);
                if (landscape.FirstBigBuildingPosition != null)
                {
                    break;
                }
            }

            _cityPlaneAnchor = landscape.FirstBigBuildingPosition;
            _cityPlaneAnchorSearched = true;
            return _cityPlaneAnchor;
        }

        // 3 sides (left wall, middle, right wall) of 2 triangles: (b + s, b + 5 + s, b + 4 + s), (b + 5 + s, b + s, b + 1 + s)
        private static void AddStripIndices(uint[] indices, ref int outputIndex, int segmentStartVertex)
        {
            for (int side = 0; side < 3; side++)
            {
                indices[outputIndex + 0] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 1] = (uint)(segmentStartVertex + 5 + side);
                indices[outputIndex + 2] = (uint)(segmentStartVertex + 4 + side);
                indices[outputIndex + 3] = (uint)(segmentStartVertex + 5 + side);
                indices[outputIndex + 4] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 5] = (uint)(segmentStartVertex + 1 + side);
                outputIndex += 6;
            }
        }

        private static VertexPositionNormalTextureTangent CreateStructureVertex(
            Vector3 legacyPosition,
            Vector3 legacyNormal,
            Vector3 legacyTangent,
            float u,
            float v,
            Vector3 origin)
        {
            return new VertexPositionNormalTextureTangent(
                ConvertLegacyPoint(legacyPosition) - origin,
                NormalizeOrFallback(ConvertLegacyVector(legacyNormal), Vector3.Up),
                new Vector2(u, v),
                new Vector4(NormalizeOrFallback(ConvertLegacyVector(legacyTangent), Vector3.Right), 1f));
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

        private static Entity CreateStructureEntity(string entityName, StaticModelMesh mesh, bool castShadows)
        {
            var model = new StaticModel { Name = entityName };
            model.Meshes.Add(mesh);
            model.RootNode = new StaticModelNode
            {
                Name = "Root",
                MeshIndex = 0,
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            };

            return CreateStaticModelEntity(entityName, model, Matrix.Identity, castShadows);
        }

        // Track.roadBackMaterial: Material("RoadBack", "RoadBackNormal"); the lighting values are the road's, for now.
        private static LitDiffuseMaterial CreateRoadBackMaterial(string trackName, AssetContentManager assetContentManager)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "RoadBack.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "RoadBackNormal.png");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.RoadBackMaterial",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(60, 62, 66),
                EmissiveColor = new Vector3(0.015f, 0.015f, 0.015f),
                SpecularColor = new Vector3(0.14f),
                SpecularPower = 18f,
                RasterizerState = RasterizerState.CullCounterClockwise,
                SamplerState = SamplerState.AnisotropicWrap,
            };
        }

        // Track.roadTunnelMaterial: Material(..., "RoadTunnel", "RoadTunnelNormal"); rendered with culling off.
        private static LitDiffuseMaterial CreateRoadTunnelMaterial(string trackName, AssetContentManager assetContentManager)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "RoadTunnel.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "RoadTunnelNormal.png");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.RoadTunnelMaterial",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(182, 182, 182),
                EmissiveColor = new Vector3(0.015f, 0.015f, 0.015f),
                SpecularColor = new Vector3(0.14f),
                SpecularPower = 18f,
                RasterizerState = RasterizerState.CullNone,
                SamplerState = SamplerState.AnisotropicWrap,
            };
        }

        // Landscape.cityMat: Material(..., "CityGround", "CityGroundNormal"); the lighting values are the terrain's, for now.
        private static LitDiffuseMaterial CreateCityGroundMaterial(string trackName, AssetContentManager assetContentManager)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "CityGround.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "CityGroundNormal.png");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.CityGroundMaterial",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(182, 166, 118),
                EmissiveColor = new Vector3(0.01f, 0.01f, 0.008f),
                SpecularColor = new Vector3(0.05f),
                SpecularPower = 6f,
                RasterizerState = RasterizerState.CullCounterClockwise,
                SamplerState = SamplerState.AnisotropicWrap,
            };
        }
    }
}
