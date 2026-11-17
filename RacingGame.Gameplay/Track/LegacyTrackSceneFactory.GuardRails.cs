using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Import;
using static RacingGame.Gameplay.Track.RaceTrackSpace;

namespace RacingGame.Gameplay.Track;

internal static partial class LegacyTrackSceneFactory
{
    private static class LegacyTrackGuardRailBuilder
    {
        private const float RailCorrectionScale = 0.0019f;
        private const float GuardRailHeight = 1.35f * 1.5f * 0.425f;
        private const float ColumnGroundHeight = 1.0f;
        private const float TopColumnSubHeight = 0.55f;

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

        /// <summary>One side's rail, then its posts: the models a guard rail generator places (none without guard rails).</summary>
        internal static IReadOnlyList<RaceGeneratedModel> CreateRailModels(
            string trackName,
            RaceTrackLayout layout,
            bool leftSide,
            Guid materialAssetId)
        {
            var models = new List<RaceGeneratedModel>();
            if (!layout.HasGuardRails)
            {
                return models;
            }

            models.Add(CreateRailModel(
                trackName,
                leftSide ? layout.LeftRailPoints : layout.RightRailPoints,
                layout.Origin,
                layout.Settings.HolderGap,
                materialAssetId,
                leftSide));
            models.AddRange(CreateHolderModels(leftSide ? layout.LeftPosts : layout.RightPosts));
            return models;
        }

        /// <summary>The columns' model, then their segments: the models a column generator places (none without columns).</summary>
        internal static IReadOnlyList<RaceGeneratedModel> CreateColumnModels(
            string trackName,
            RaceTrackLayout layout,
            Guid materialAssetId)
        {
            if (layout.Columns.Count == 0)
            {
                return [];
            }

            var models = new List<RaceGeneratedModel>();
            models.Add(CreateProceduralColumnsModel(trackName, layout.Columns, layout.Origin, materialAssetId));
            models.AddRange(CreateColumnSegmentModels(layout.ColumnSegments));
            return models;
        }

        private static RaceGeneratedModel CreateRailModel(
            string trackName,
            IReadOnlyList<RaceTrackRailPoint> railPoints,
            Vector3 origin,
            float holderGap,
            Guid materialAssetId,
            bool leftSide)
        {
            var vertices = new VertexPositionNormalTextureTangent[railPoints.Count * GuardRailSectionVertices.Length];
            float uTexValue = 0.5f;

            for (int index = 0; index < railPoints.Count; index++)
            {
                RaceTrackRailPoint railPoint = railPoints[index];
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
                uTexValue += (1f / holderGap) * distance * 2.0f;
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
            mesh.MaterialAssetId = materialAssetId;

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

            return new RaceGeneratedModel($"{trackName}.GuardRail.{sideName}", model, Matrix.Identity, CastShadows: true, OwnsBuffers: true);
        }

        // The post model is the catalogued one: each placement holds it through the engine, the generator never owns it.
        private static IReadOnlyList<RaceGeneratedModel> CreateHolderModels(IReadOnlyList<RaceTrackModelPlacement> posts)
        {
            if (!TryGetStaticModelAssetId(RaceTrackLayout.GuardRailPostModelName, out Guid holderModelAssetId))
            {
                return [];
            }

            var models = new List<RaceGeneratedModel>(posts.Count);
            for (int index = 0; index < posts.Count; index++)
            {
                models.Add(new RaceGeneratedModel($"Post.{index:000}", null, posts[index].Transform, CastShadows: posts[index].CastsShadow, OwnsBuffers: false, StaticModelAssetId: holderModelAssetId, ReceiveShadows: posts[index].ReceivesShadow));
            }

            return models;
        }

        private static RaceGeneratedModel CreateProceduralColumnsModel(
            string trackName,
            IReadOnlyList<RaceTrackColumnPlacement> columns,
            Vector3 origin,
            Guid materialAssetId)
        {
            var vertices = new VertexPositionNormalTextureTangent[columns.Count * BaseColumnVertices.Length * 2];

            for (int index = 0; index < columns.Count; index++)
            {
                Vector3 topAnchor = columns[index].Position;
                Vector3 bottomAnchor = new(topAnchor.X, topAnchor.Y, columns[index].GroundHeight + ColumnGroundHeight);
                Vector3 topPosition = new(topAnchor.X, topAnchor.Y, topAnchor.Z - TopColumnSubHeight);
                float topTexV = Vector3.Distance(topPosition, bottomAnchor) / (MathHelper.Pi * 2f);

                for (int topBottom = 0; topBottom < 2; topBottom++)
                {
                    Matrix pointSpace = topBottom == 0 ? columns[index].BottomSpace : columns[index].TopSpace;
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

            var indices = new uint[(BaseColumnVertices.Length - 1) * columns.Count * 6];
            int currentIndex = 0;
            int currentVertex = 0;
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
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
            mesh.MaterialAssetId = materialAssetId;

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

            // Neither cast nor received: Track.GenerateShadow and Track.UseShadow leave the columns out ("not required, we don't see near
            // columns anyway", Track.cs:1313-1315 and :1336-1338, both calls commented out), and TrackColumns has no shadow method. The
            // placed column segments are scenery objects with their own flags (CreateColumnSegmentModels).
            return new RaceGeneratedModel($"{trackName}.Columns", model, Matrix.Identity, CastShadows: false, OwnsBuffers: true, ReceiveShadows: false);
        }

        // The segment model is the catalogued one: each placement holds it through the engine, the generator never owns it.
        private static IReadOnlyList<RaceGeneratedModel> CreateColumnSegmentModels(IReadOnlyList<RaceTrackModelPlacement> segments)
        {
            if (!TryGetStaticModelAssetId(RaceTrackLayout.ColumnSegmentModelName, out Guid segmentModelAssetId))
            {
                return [];
            }

            var models = new List<RaceGeneratedModel>(segments.Count);
            for (int index = 0; index < segments.Count; index++)
            {
                models.Add(new RaceGeneratedModel($"Segment.{index:000}", null, segments[index].Transform, CastShadows: segments[index].CastsShadow, OwnsBuffers: false, StaticModelAssetId: segmentModelAssetId, ReceiveShadows: segments[index].ReceivesShadow));
            }

            return models;
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

        private readonly record struct RailSectionVertex(Vector3 Position, float V, Vector3 Normal, Vector3 Tangent);

        private readonly record struct ColumnBaseVertex(Vector3 Position, float U, Vector3 Normal, Vector3 Tangent);
    }
}