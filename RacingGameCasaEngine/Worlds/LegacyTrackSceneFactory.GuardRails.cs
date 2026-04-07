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
    private static class LegacyTrackGuardRailBuilder
    {
        private const float RailCorrectionScale = 0.0019f;
        private const float HolderGap = 15.0f;
        private const float GuardRailHeight = 1.35f * 1.5f * 0.425f;
        private const float InsideRoadDistance = 0.5f;
        private const float ColumnsDistance = 33.0f;
        private const float ColumnGroundHeight = 1.0f;
        private const float MinimumColumnHeight = 2.5f;
        private const float TopColumnSubHeight = 0.55f;

        private static readonly Vector3 HolderPileCorrectionVector = new(0.225f, 0f, 0f);

        private static readonly RailSectionVertex[] GuardRailSectionVertices =
        [
            new(new Vector3(10, 0, -105), 1f - 0.442877f, new Vector3(-0.382683f, 0, -0.923880f), new Vector3(0, -1, 0)),
            new(new Vector3(20, 0, -105), 1f - 0.432881f, new Vector3(0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-10, 0, -75), 1f - 0.402893f, new Vector3(0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-10, 0, -45), 1f - 0.372905f, new Vector3(0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(20, 0, -15), 1f - 0.342917f, new Vector3(0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(20, 0, 15), 1f - 0.312929f, new Vector3(0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-10, 0, 45), 1f - 0.282941f, new Vector3(0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-10, 0, 75), 1f - 0.252953f, new Vector3(0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(20, 0, 105), 1f - 0.222965f, new Vector3(0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(10, 0, 105), 1f - 0.212969f, new Vector3(-0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-20, 0, 75), 1f - 0.182981f, new Vector3(-0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-20, 0, 45), 1f - 0.152993f, new Vector3(-0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(10, 0, 15), 1f - 0.123005f, new Vector3(-0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(10, 0, -15), 1f - 0.093017f, new Vector3(-0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-20, 0, -45), 1f - 0.063029f, new Vector3(-0.923880f, 0, 0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(-20, 0, -75), 1f - 0.033041f, new Vector3(-0.923880f, 0, -0.382683f), new Vector3(0, -1, 0)),
            new(new Vector3(10, 0, -105), 1f - 0.003053f, new Vector3(-0.382683f, 0, -0.923880f), new Vector3(0, -1, 0)),
        ];

        private static readonly ColumnBaseVertex[] BaseColumnVertices =
        [
            new(new Vector3(1f, 0f, 0f), 0.0f / 6.0f, new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(0.5f, 0.866025f, 0f), 1.0f / 6.0f, new Vector3(0.5f, 0.866025f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(-0.5f, 0.866025f, 0f), 2.0f / 6.0f, new Vector3(-0.5f, 0.866025f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(-1f, 0f, 0f), 3.0f / 6.0f, new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(-0.5f, -0.866025f, 0f), 4.0f / 6.0f, new Vector3(-0.5f, -0.866025f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(0.5f, -0.866025f, 0f), 5.0f / 6.0f, new Vector3(0.5f, -0.866025f, 0f), new Vector3(0f, 0f, -1f)),
            new(new Vector3(1f, 0f, 0f), 6.0f / 6.0f, new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f)),
        ];

        internal static IReadOnlyList<Entity> CreateEntities(
            string trackName,
            IReadOnlyList<LegacyTrackPoint> roadPoints,
            Vector3 origin,
            LegacyTerrainHeightSampler terrainSampler,
            AssetContentManager assetContentManager)
        {
            var entities = new List<Entity>();
            if (roadPoints.Count < 4)
            {
                return entities;
            }

            List<LegacyRailPoint> leftRailPoints = BuildRailPoints(roadPoints, leftSide: true);
            List<LegacyRailPoint> rightRailPoints = BuildRailPoints(roadPoints, leftSide: false);

            entities.Add(CreateRailEntity(trackName, leftRailPoints, origin, assetContentManager, leftSide: true));
            entities.Add(CreateRailEntity(trackName, rightRailPoints, origin, assetContentManager, leftSide: false));

            entities.AddRange(CreateHolderEntities(trackName, leftRailPoints, origin, assetContentManager, leftSide: true));
            entities.AddRange(CreateHolderEntities(trackName, rightRailPoints, origin, assetContentManager, leftSide: false));
            entities.AddRange(CreateColumnEntities(trackName, roadPoints, origin, terrainSampler, assetContentManager));

            return entities;
        }

        private static List<LegacyRailPoint> BuildRailPoints(IReadOnlyList<LegacyTrackPoint> roadPoints, bool leftSide)
        {
            var railPoints = new List<LegacyRailPoint>(roadPoints.Count / 2 + 1);
            int railPointCount = roadPoints.Count / 2 + 1;

            for (int index = 0; index < railPointCount; index++)
            {
                int pointIndex = index * 2;
                if (pointIndex >= roadPoints.Count - 1)
                {
                    pointIndex = roadPoints.Count - 1;
                }

                LegacyTrackPoint roadPoint = roadPoints[pointIndex];
                Vector3 right = roadPoint.Right;
                Vector3 direction = roadPoint.Direction;
                Vector3 position = roadPoint.Position + (leftSide ? -1f : 1f) * (LegacyRoadWidthScale * roadPoint.RoadWidth * right * 0.5f);

                if (leftSide)
                {
                    right = -right;
                    direction = -direction;
                }

                position -= right * InsideRoadDistance;
                railPoints.Add(new LegacyRailPoint(position, right, direction, roadPoint.Up));
            }

            return railPoints;
        }

        private static Entity CreateRailEntity(
            string trackName,
            IReadOnlyList<LegacyRailPoint> railPoints,
            Vector3 origin,
            AssetContentManager assetContentManager,
            bool leftSide)
        {
            var vertices = new VertexPositionNormalTextureTangent[railPoints.Count * GuardRailSectionVertices.Length];
            float uTexValue = 0.5f;

            for (int index = 0; index < railPoints.Count; index++)
            {
                LegacyRailPoint railPoint = railPoints[index];
                Matrix pointSpace = CreatePointSpaceMatrix(railPoint.Right, railPoint.Direction, railPoint.Up);
                Vector3 localPosition = railPoint.Position + railPoint.Up * GuardRailHeight;
                Matrix transform = pointSpace * Matrix.CreateTranslation(localPosition);

                for (int vertexIndex = 0; vertexIndex < GuardRailSectionVertices.Length; vertexIndex++)
                {
                    RailSectionVertex source = GuardRailSectionVertices[vertexIndex];
                    Vector3 legacyPosition = Vector3.Transform(source.Position * RailCorrectionScale, transform);
                    Vector3 legacyNormal = Vector3.TransformNormal((leftSide ? -1f : 1f) * source.Normal, pointSpace);
                    Vector3 legacyTangent = Vector3.TransformNormal(-source.Tangent, pointSpace);

                    vertices[index * GuardRailSectionVertices.Length + vertexIndex] = new VertexPositionNormalTextureTangent(
                        ConvertLegacyPoint(legacyPosition) - origin,
                        NormalizeOrFallback(ConvertLegacyVector(legacyNormal), Vector3.Up),
                        new Vector2(uTexValue, source.V),
                        new Vector4(NormalizeOrFallback(ConvertLegacyVector(legacyTangent), Vector3.Right), 1f));
                }

                Vector3 nextPosition = railPoints[(index + 1) % railPoints.Count].Position;
                float distance = Vector3.Distance(nextPosition, railPoint.Position);
                uTexValue += (1f / HolderGap) * distance * 2.0f;
            }

            var indices = new uint[(railPoints.Count - 1) * (GuardRailSectionVertices.Length - 1) * 6];
            int currentIndex = 0;
            int currentVertex = 0;
            for (int stripIndex = 0; stripIndex < railPoints.Count - 1; stripIndex++)
            {
                for (int quadIndex = 0; quadIndex < GuardRailSectionVertices.Length - 1; quadIndex++)
                {
                    indices[currentIndex + 0] = (uint)(currentVertex + quadIndex);
                    indices[currentIndex + 1] = (uint)(currentVertex + quadIndex + 1);
                    indices[currentIndex + 2] = (uint)(currentVertex + quadIndex + 1 + GuardRailSectionVertices.Length);
                    indices[currentIndex + 3] = indices[currentIndex + 2];
                    indices[currentIndex + 4] = (uint)(currentVertex + quadIndex + GuardRailSectionVertices.Length);
                    indices[currentIndex + 5] = indices[currentIndex + 0];
                    currentIndex += 6;
                }

                currentVertex += GuardRailSectionVertices.Length;
            }

            string sideName = leftSide ? "Left" : "Right";
            var mesh = new StaticModelMesh { Name = $"{trackName}.GuardRail.{sideName}" };
            mesh.SetData(vertices, indices);
            mesh.Material = CreateGuardRailMaterial(trackName, assetContentManager, sideName);

            var model = new StaticModel { Name = $"{trackName}.GuardRail.{sideName}" };
            model.Meshes.Add(mesh);
            model.RootNode = new StaticModelNode
            {
                Name = "Root",
                MeshIndex = 0,
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            };

            return CreateStaticModelEntity($"Track.GuardRail.{sideName}.{trackName}", model, Matrix.Identity);
        }

        private static IReadOnlyList<Entity> CreateHolderEntities(
            string trackName,
            IReadOnlyList<LegacyRailPoint> railPoints,
            Vector3 origin,
            AssetContentManager assetContentManager,
            bool leftSide)
        {
            StaticModel? holderModel = LoadLegacyModel("GuardRailHolder", assetContentManager);
            if (holderModel == null)
            {
                return [];
            }

            var entities = new List<Entity>();
            float lastHolderGap = 0f;
            string sideName = leftSide ? "Left" : "Right";

            for (int index = 0; index < railPoints.Count; index++)
            {
                LegacyRailPoint railPoint = railPoints[index];
                Vector3 nextPosition = railPoints[(index + 1) % railPoints.Count].Position;
                float distance = Vector3.Distance(nextPosition, railPoint.Position);

                if (distance > 0.0001f && lastHolderGap - distance <= 0f)
                {
                    Vector3 p1 = railPoints[index - 1 < 0 ? railPoints.Count - 1 : index - 1].Position;
                    Vector3 p2 = railPoint.Position;
                    Vector3 p3 = railPoints[(index + 1) % railPoints.Count].Position;
                    Vector3 p4 = railPoints[(index + 2) % railPoints.Count].Position;
                    Vector3 holderPoint = Vector3.CatmullRom(p1, p2, p3, p4, lastHolderGap / distance);

                    Matrix pointSpace = CreatePointSpaceMatrix(railPoint.Right, railPoint.Direction, railPoint.Up);
                    Matrix legacyTransform = Matrix.CreateScale(1.125f)
                        * Matrix.CreateTranslation(HolderPileCorrectionVector)
                        * pointSpace
                        * Matrix.CreateTranslation(holderPoint);

                    Matrix runtimeTransform = ConvertLegacyTransform(legacyTransform, origin);
                    entities.Add(CreateStaticModelEntity($"Track.GuardRailHolder.{sideName}.{trackName}.{entities.Count:000}", holderModel, runtimeTransform));
                    lastHolderGap += HolderGap;
                }

                lastHolderGap -= distance;
            }

            return entities;
        }

        private static IReadOnlyList<Entity> CreateColumnEntities(
            string trackName,
            IReadOnlyList<LegacyTrackPoint> roadPoints,
            Vector3 origin,
            LegacyTerrainHeightSampler terrainSampler,
            AssetContentManager assetContentManager)
        {
            var columnPositions = new List<Vector3>();
            var topSpaces = new List<Matrix>();
            var bottomSpaces = new List<Matrix>();
            float lastColumnsDistance = ColumnsDistance;

            for (int index = 0; index < roadPoints.Count; index++)
            {
                LegacyTrackPoint currentPoint = roadPoints[index];
                LegacyTrackPoint nextPoint = roadPoints[(index + 1) % roadPoints.Count];
                float distance = Vector3.Distance(nextPoint.Position, currentPoint.Position);

                if (distance > 0.0001f && lastColumnsDistance - distance <= 0f)
                {
                    Vector3 p1 = roadPoints[index - 1 < 0 ? roadPoints.Count - 1 : index - 1].Position;
                    Vector3 p2 = currentPoint.Position;
                    Vector3 p3 = nextPoint.Position;
                    Vector3 p4 = roadPoints[(index + 2) % roadPoints.Count].Position;
                    Vector3 columnPoint = Vector3.CatmullRom(p1, p2, p3, p4, lastColumnsDistance / distance);

                    float draft = Vector3.Dot(currentPoint.Up, new Vector3(0f, 0f, 1f));
                    float columnHeight = columnPoint.Z - terrainSampler.GetMapHeight(columnPoint.X, columnPoint.Y);
                    if (draft > 0.3f && columnHeight > MinimumColumnHeight)
                    {
                        columnPositions.Add(columnPoint);
                        topSpaces.Add(CreatePointSpaceMatrix(currentPoint.Right, currentPoint.Direction, currentPoint.Up));

                        Vector3 upVector = new(0f, 0f, 1f);
                        Vector3 rightVector = Vector3.Cross(currentPoint.Direction, upVector);
                        if (rightVector.LengthSquared() < 0.0001f)
                        {
                            rightVector = Vector3.Right;
                        }
                        else
                        {
                            rightVector.Normalize();
                        }

                        bottomSpaces.Add(CreatePointSpaceMatrix(rightVector, currentPoint.Direction, upVector));
                    }

                    lastColumnsDistance += ColumnsDistance;
                }

                lastColumnsDistance -= distance;
            }

            if (columnPositions.Count == 0)
            {
                return [];
            }

            var entities = new List<Entity>();
            entities.Add(CreateProceduralColumnsEntity(trackName, columnPositions, topSpaces, bottomSpaces, origin, terrainSampler, assetContentManager));
            entities.AddRange(CreateColumnSegmentEntities(trackName, columnPositions, origin, terrainSampler, assetContentManager));
            return entities;
        }

        private static Entity CreateProceduralColumnsEntity(
            string trackName,
            IReadOnlyList<Vector3> columnPositions,
            IReadOnlyList<Matrix> topSpaces,
            IReadOnlyList<Matrix> bottomSpaces,
            Vector3 origin,
            LegacyTerrainHeightSampler terrainSampler,
            AssetContentManager assetContentManager)
        {
            var vertices = new VertexPositionNormalTextureTangent[columnPositions.Count * BaseColumnVertices.Length * 2];

            for (int index = 0; index < columnPositions.Count; index++)
            {
                Vector3 topAnchor = columnPositions[index];
                Vector3 bottomAnchor = new(topAnchor.X, topAnchor.Y, terrainSampler.GetMapHeight(topAnchor.X, topAnchor.Y) + ColumnGroundHeight);
                Vector3 topPosition = new(topAnchor.X, topAnchor.Y, topAnchor.Z - TopColumnSubHeight);
                float topTexV = Vector3.Distance(topPosition, bottomAnchor) / (MathHelper.Pi * 2f);

                for (int topBottom = 0; topBottom < 2; topBottom++)
                {
                    Matrix pointSpace = topBottom == 0 ? bottomSpaces[index] : topSpaces[index];
                    Vector3 anchor = topBottom == 0 ? bottomAnchor : topPosition;
                    float texV = topBottom == 0 ? 0f : topTexV;

                    for (int vertexIndex = 0; vertexIndex < BaseColumnVertices.Length; vertexIndex++)
                    {
                        ColumnBaseVertex source = BaseColumnVertices[vertexIndex];
                        int outputIndex = index * BaseColumnVertices.Length * 2 + topBottom * BaseColumnVertices.Length + vertexIndex;

                        Vector3 legacyPosition = anchor + Vector3.Transform(source.Position, pointSpace);
                        Vector3 legacyNormal = Vector3.Transform(source.Normal, pointSpace);
                        Vector3 legacyTangent = Vector3.Transform(-source.Tangent, pointSpace);
                        vertices[outputIndex] = new VertexPositionNormalTextureTangent(
                            ConvertLegacyPoint(legacyPosition) - origin,
                            NormalizeOrFallback(ConvertLegacyVector(legacyNormal), Vector3.Up),
                            new Vector2(source.U, texV),
                            new Vector4(NormalizeOrFallback(ConvertLegacyVector(legacyTangent), Vector3.Right), 1f));
                    }
                }
            }

            var indices = new uint[(BaseColumnVertices.Length - 1) * columnPositions.Count * 6];
            int currentIndex = 0;
            int currentVertex = 0;
            for (int columnIndex = 0; columnIndex < columnPositions.Count; columnIndex++)
            {
                for (int quadIndex = 0; quadIndex < BaseColumnVertices.Length - 1; quadIndex++)
                {
                    indices[currentIndex + 0] = (uint)(currentVertex + quadIndex);
                    indices[currentIndex + 1] = (uint)(currentVertex + quadIndex + 1 + BaseColumnVertices.Length);
                    indices[currentIndex + 2] = (uint)(currentVertex + quadIndex + 1);
                    indices[currentIndex + 3] = indices[currentIndex + 1];
                    indices[currentIndex + 4] = indices[currentIndex + 0];
                    indices[currentIndex + 5] = (uint)(currentVertex + quadIndex + BaseColumnVertices.Length);
                    currentIndex += 6;
                }

                currentVertex += BaseColumnVertices.Length * 2;
            }

            var mesh = new StaticModelMesh { Name = $"{trackName}.Columns" };
            mesh.SetData(vertices, indices);
            mesh.Material = CreateRoadColumnMaterial(trackName, assetContentManager);

            var model = new StaticModel { Name = $"{trackName}.Columns" };
            model.Meshes.Add(mesh);
            model.RootNode = new StaticModelNode
            {
                Name = "Root",
                MeshIndex = 0,
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            };

            return CreateStaticModelEntity($"Track.Columns.{trackName}", model, Matrix.Identity);
        }

        private static IReadOnlyList<Entity> CreateColumnSegmentEntities(
            string trackName,
            IReadOnlyList<Vector3> columnPositions,
            Vector3 origin,
            LegacyTerrainHeightSampler terrainSampler,
            AssetContentManager assetContentManager)
        {
            StaticModel? segmentModel = LoadLegacyModel("RoadColumnSegment", assetContentManager);
            if (segmentModel == null)
            {
                return [];
            }

            var entities = new List<Entity>(columnPositions.Count);
            for (int index = 0; index < columnPositions.Count; index++)
            {
                Vector3 columnPoint = columnPositions[index];
                Vector3 bottomPosition = new(
                    columnPoint.X,
                    columnPoint.Y,
                    terrainSampler.GetMapHeight(columnPoint.X, columnPoint.Y));

                Matrix legacyTransform = Matrix.CreateScale(1.2f)
                    * Matrix.CreateTranslation(new Vector3(bottomPosition.X, bottomPosition.Y, bottomPosition.Z));
                Matrix runtimeTransform = ConvertLegacyTransform(legacyTransform, origin);
                entities.Add(CreateStaticModelEntity($"Track.ColumnSegment.{trackName}.{index:000}", segmentModel, runtimeTransform));
            }

            return entities;
        }

        private static LitDiffuseMaterial CreateGuardRailMaterial(string trackName, AssetContentManager assetContentManager, string sideName)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "Leitplanke.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "LeitplankeNormal.tga");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.GuardRailMaterial.{sideName}",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(182, 182, 182),
                EmissiveColor = new Vector3(0.03f, 0.03f, 0.03f),
                SpecularColor = new Vector3(0.88f, 0.88f, 0.88f),
                SpecularPower = 16f,
                SamplerState = SamplerState.AnisotropicWrap,
                RasterizerState = RasterizerState.CullCounterClockwise,
            };
        }

        private static LitDiffuseMaterial CreateRoadColumnMaterial(string trackName, AssetContentManager assetContentManager)
        {
            Texture2D? diffuseTexture = LoadProjectTexture(assetContentManager, "RoadCement.tga");
            Texture2D? normalTexture = LoadProjectTexture(assetContentManager, "RoadCementNormal.tga");
            return new LitDiffuseMaterial
            {
                Name = $"{trackName}.ColumnMaterial",
                BasColor = diffuseTexture,
                NormalMap = normalTexture,
                DiffuseColor = diffuseTexture != null ? Color.White : new Color(176, 176, 176),
                EmissiveColor = new Vector3(0.01f, 0.01f, 0.01f),
                SpecularColor = new Vector3(0.18f),
                SpecularPower = 8f,
                SamplerState = SamplerState.AnisotropicWrap,
                RasterizerState = RasterizerState.CullCounterClockwise,
            };
        }

        private static Matrix CreatePointSpaceMatrix(Vector3 right, Vector3 direction, Vector3 up)
        {
            if (right.LengthSquared() > 0.0001f)
            {
                right.Normalize();
            }
            else
            {
                right = Vector3.Right;
            }

            if (direction.LengthSquared() > 0.0001f)
            {
                direction.Normalize();
            }
            else
            {
                direction = Vector3.UnitY;
            }

            if (up.LengthSquared() > 0.0001f)
            {
                up.Normalize();
            }
            else
            {
                up = Vector3.UnitZ;
            }

            Matrix pointSpace = Matrix.Identity;
            pointSpace.M11 = right.X;
            pointSpace.M12 = right.Y;
            pointSpace.M13 = right.Z;
            pointSpace.M21 = direction.X;
            pointSpace.M22 = direction.Y;
            pointSpace.M23 = direction.Z;
            pointSpace.M31 = up.X;
            pointSpace.M32 = up.Y;
            pointSpace.M33 = up.Z;
            return pointSpace;
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

        private readonly record struct LegacyRailPoint(Vector3 Position, Vector3 Right, Vector3 Direction, Vector3 Up);

        private readonly record struct RailSectionVertex(Vector3 Position, float V, Vector3 Normal, Vector3 Tangent);

        private readonly record struct ColumnBaseVertex(Vector3 Position, float U, Vector3 Normal, Vector3 Tangent);
    }
}