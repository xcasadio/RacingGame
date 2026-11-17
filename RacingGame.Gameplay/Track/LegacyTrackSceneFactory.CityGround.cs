using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static RacingGame.Gameplay.Track.RaceTrackSpace;

namespace RacingGame.Gameplay.Track;

internal static partial class LegacyTrackSceneFactory
{
    /// <summary>
    /// The anchor of the city ground plane, in the original's coordinates: the position of the first object of the Beginner
    /// level whose model name contains "hotel" or "building".
    /// <para>
    /// Derivation. The original makes one city plane, in the constructor of its Landscape, which is built for Level.Beginner
    /// (RacingGameManager.cs:402, <c>new Landscape(Level.Beginner)</c>), at the first object of <c>landscapeObjects</c> for which
    /// <c>IsBigBuilding</c> holds (Landscape.cs:71-77, 922-934: <c>CreateCityPlane</c>, <c>PlaneRenderer(obj.Position, ...)</c>).
    /// Loading another level keeps it for the shipped terrain (<c>ApplyTerrain</c>, Landscape.cs:963-980: the plane is only made
    /// again for a custom terrain), so the three tracks share this one plane. The objects of the track file are added first
    /// (TrackLine.cs:664-676); of the Beginner file, the first one the landscape accepts is the combo <c>CombiBuildings</c> (its
    /// first object, <c>TrackBeginner</c>, is not a model of the landscape), whose first model is a <c>Building</c>. Its position
    /// is the translation of the matrix the object is added with, its height raised to the landscape's when it is under it
    /// (Landscape.cs:446-457), here 28.235294, the terrain height at that spot.
    /// </para>
    /// <para>
    /// The value was measured twice, independently, and both agree to the last digit of a float: (a) from this port's own placement
    /// of Beginner (<c>RaceTrackLayout.Placements</c>, the first placement whose name contains "hotel" or "building", converted back
    /// to the original's axes); (b) by a log line added for the occasion to <c>CreateCityPlane</c> of the original, run on its
    /// capture of the three levels (a single line, for the Beginner level, with 1155 objects in <c>landscapeObjects</c>, the big
    /// building at index 0). See plan <c>ai-agent/tasks/rgce-pbr-handover-tasks.md</c>, T12.2.
    /// </para>
    /// A track generator does not read another track's file, so the value is a constant, as the original's plane is for the
    /// shipped tracks.
    /// </summary>
    internal static readonly Vector3 LegacyCityGroundAnchor = new(994.9773f, 426.16977f, 28.235294f);

    // PlaneRenderer.cs: Tiling; Landscape.CreateCityPlane: new Plane((0, 0, 1), 0.1f), whose D is the height above the anchor.
    private const float LegacyCityPlaneTiling = 20.0f;
    private const float LegacyCityPlaneHeight = 0.1f;

    /// <summary>
    /// The city ground plane, built as RacingGame builds it (Landscape.CreateCityPlane and PlaneRenderer.DrawPlaneVertices): one
    /// quad of half size <c>min(x, y)</c> of the anchor, centred on it, 0.1 above it, with <c>up</c> = (0, 0, 1), <c>right</c> =
    /// (1, 0, 0) and <c>dir</c> = (0, 1, 0), the normal <c>up</c>, the tangent <c>right</c> and the texture coordinates
    /// <c>+-size / 20</c>. The original draws a triangle strip of 4 vertices, (-right - dir), (-right + dir), (right - dir),
    /// (right + dir): the triangles are (0, 1, 2) and (2, 1, 3). The plane does not cast a shadow; it receives the shadows of the
    /// race world, as the original's screen-space shadow mask darkens what is drawn there.
    /// </summary>
    internal static RaceGeneratedModel CreateCityGroundModel(string trackName, Vector3 origin, Guid materialAssetId)
    {
        Vector3 anchor = LegacyCityGroundAnchor;
        float size = Math.Min(anchor.X, anchor.Y);
        float uvExtent = size / LegacyCityPlaneTiling;
        var up = new Vector3(0f, 0f, 1f);
        var right = new Vector3(1f, 0f, 0f);
        var dir = new Vector3(0f, 1f, 0f);
        Vector3 runtimeNormal = ConvertLegacyDirection(up);
        Vector4 runtimeTangent = new(ConvertLegacyDirection(right), 1f);

        var vertices = new VertexPositionNormalTextureTangent[4];
        vertices[0] = CreateCityGroundVertex(anchor, (-right - dir) * size + up * LegacyCityPlaneHeight, -uvExtent, -uvExtent, runtimeNormal, runtimeTangent, origin);
        vertices[1] = CreateCityGroundVertex(anchor, (-right + dir) * size + up * LegacyCityPlaneHeight, -uvExtent, +uvExtent, runtimeNormal, runtimeTangent, origin);
        vertices[2] = CreateCityGroundVertex(anchor, (right - dir) * size + up * LegacyCityPlaneHeight, +uvExtent, -uvExtent, runtimeNormal, runtimeTangent, origin);
        vertices[3] = CreateCityGroundVertex(anchor, (right + dir) * size + up * LegacyCityPlaneHeight, +uvExtent, +uvExtent, runtimeNormal, runtimeTangent, origin);
        uint[] indices = [0, 1, 2, 2, 1, 3];

        var cityMesh = new StaticModelMesh { Name = $"{trackName}.CityGround" };
        cityMesh.SetData(vertices, indices);
        cityMesh.MaterialAssetId = materialAssetId;

        var cityModel = new StaticModel { Name = $"{trackName}.CityGround" };
        cityModel.Meshes.Add(cityMesh);
        cityModel.RootNode = new StaticModelNode
        {
            Name = "CityGroundRoot",
            MeshIndex = 0,
        };

        return new RaceGeneratedModel($"{trackName}.CityGround", cityModel, Matrix.Identity, CastShadows: false, OwnsBuffers: true, ReceiveShadows: true);
    }

    // The plane is drawn with the world matrix Matrix.CreateTranslation(anchor) (PlaneRenderer.Render): the vertex is the offset
    // plus the anchor.
    private static VertexPositionNormalTextureTangent CreateCityGroundVertex(
        Vector3 anchor,
        Vector3 legacyOffset,
        float u,
        float v,
        Vector3 runtimeNormal,
        Vector4 runtimeTangent,
        Vector3 origin)
    {
        Vector3 runtimePosition = ConvertLegacyPoint(anchor + legacyOffset) - origin;
        return new VertexPositionNormalTextureTangent(runtimePosition, runtimeNormal, new Vector2(u, v), runtimeTangent);
    }
}
