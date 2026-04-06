using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Shaders;
using RacingGame.Tracks;
using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Sounds;
namespace RacingGame.Landscapes;

/// <summary>
/// Landscape
/// </summary>
public class Landscape : IDisposable
{
    #region Constants
    /// <summary>
    /// Grid width and height
    /// </summary>
    const int GridWidth = 257,
        GridHeight = 257;

    const float MapWidthFactor = 10,
        MapHeightFactor = 10,
        MapZScale = 300.0f;
    #endregion

    #region Variables
    /// <summary>
    /// Currently loaded level
    /// </summary>
    RacingGameManager.Level level = RacingGameManager.Level.Beginner;

    readonly TrackObjectManager trackObjectManager;
    readonly ReplayManager replayManager = new ReplayManager();
    readonly BrakeTrackManager brakeTrackManager = new BrakeTrackManager();

    internal string[] autoGenerationNames
    {
        get
        {
            return trackObjectManager.AutoGenerationNames;
        }
    }

    /// <summary>
    /// Vertices
    /// </summary>
    TangentVertex[] vertices = new TangentVertex[GridWidth * GridHeight];
    /// <summary>
    /// Matrix
    /// </summary>
    Material mat = new Material(
        //new Color(62, 62, 62), // ambient
        //new Color(240, 240, 240), // diffuse
        //new Color(24, 24, 24), // specular
        new Color(88, 88, 88), // ambient (bright day)
        new Color(234, 234, 234), // diffuse (also bright)
        new Color(33, 33, 33), // specular (unused anyway)
        "Landscape",
        "LandscapeNormal",
        "",
        "LandscapeDetail");

    /// <summary>
    /// City material for displaying an extra material whereever the ground
    /// is flat. This makes the ground look much better at such locations,
    /// especially where the city is at.
    /// </summary>
    Material cityMat = new Material(
        new Color(32, 32, 32),
        new Color(200, 200, 200),
        new Color(128, 128, 128),
        "CityGround",
        "CityGroundNormal", "", "");

    /// <summary>
    /// City material
    /// </summary>
    /// <returns>Material</returns>
    public Material CityMaterial
    {
        get
        {
            return cityMat;
        }
    }

    /// <summary>
    /// Replace start light object, 0=red, 1=yellow, 2=green.
    /// </summary>
    /// <param name="number">Number</param>
    public void ReplaceStartLightObject(int number)
    {
        trackObjectManager.ReplaceStartLightObject(number);
    }

    /// <summary>
    /// Kill all loaded objects.
    /// </summary>
    public void KillAllLoadedObjects()
    {
        trackObjectManager.KillAllLoadedObjects();
    }

    /// <summary>
    /// Add object to render.
    /// </summary>
    /// <param name="modelName">Model name</param>
    /// <param name="renderMatrix">Render matrix</param>
    /// <param name="isNearTrackForShadowGeneration">Is near track for shadow generation</param>
    public void AddObjectToRender(string modelName, Matrix renderMatrix,
        bool isNearTrackForShadowGeneration)
    {
        trackObjectManager.AddObjectToRender(
            modelName, renderMatrix, isNearTrackForShadowGeneration);
    }

    /// <summary>
    /// Add object to render.
    /// </summary>
    /// <param name="modelName">Model name</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="trackPos">Track position</param>
    /// <param name="trackRight">Track right</param>
    /// <param name="distance">Distance</param>
    public void AddObjectToRender(string modelName,
        float rotation, Vector3 trackPos, Vector3 trackRight,
        float distance)
    {
        trackObjectManager.AddObjectToRender(
            modelName, rotation, trackPos, trackRight, distance);
    }

    /// <summary>
    /// Add object to render.
    /// </summary>
    /// <param name="modelName">Model name</param>
    /// <param name="renderPos">Render position</param>
    public void AddObjectToRender(string modelName, Vector3 renderPos)
    {
        trackObjectManager.AddObjectToRender(modelName, renderPos);
    }

    /// <summary>
    /// City planes we render additionally to the landscape.
    /// Each city plane is just 2 triangles and the cityMat material, very
    /// fast and easy stuff.
    /// </summary>
    PlaneRenderer cityPlane = null;

    /// <summary>
    /// Vertex buffer for our landscape
    /// </summary>
    VertexBuffer vertexBuffer;
    /// <summary>
    /// Index buffer for our landscape
    /// </summary>
    IndexBuffer indexBuffer;

    /// <summary>
    /// Map heights
    /// </summary>
    float[,] mapHeights = null;

    /// <summary>
    /// Track for our landscape, can be TrackBeginner, TrackAdvanced and
    /// TrackExpert, which will be selected in the menu.
    /// </summary>
    Track track = null;

    /// <summary>
    /// Compare checkpoint time to the bestReplay times.
    /// </summary>
    /// <param name="checkpointNum">Checkpoint num</param>
    /// <returns>Time in milliseconds we improved</returns>
    public int CompareCheckpointTime(int checkpointNum)
    {
        return replayManager.CompareCheckpointTime(checkpointNum);
    }

    /// <summary>
    /// Start new lap, checks if the newReplay is good and
    /// can be stored as best replay :)
    /// </summary>
    public void StartNewLap()
    {
        replayManager.StartNewLap();
    }

    /// <summary>
    /// New replay
    /// </summary>
    public Replay NewReplay
    {
        get
        {
            return replayManager.NewReplay;
        }
    }

    #endregion

    #region Properties
    /// <summary>
    /// Current track name
    /// </summary>
    /// <returns>String</returns>
    public string CurrentTrackName
    {
        get
        {
            return level.ToString();
        }
    }

    /// <summary>
    /// Track length
    /// </summary>
    /// <returns>Float</returns>
    public float TrackLength
    {
        get
        {
            return track.Length;
        }
    }

    /// <summary>
    /// Remember checkpoint segment positions for easier checkpoint checking.
    /// </summary>
    public List<int> CheckpointSegmentPositions
    {
        get
        {
            return track.CheckpointSegmentPositions;
        }
    }

    /// <summary>
    /// Best replay for the best lap time showing the player driving.
    /// </summary>
    public Replay BestReplay
    {
        get
        {
            return replayManager.BestReplay;
        }
    }
    #endregion

    #region Get map height
    /// <summary>
    /// Get map height at a specific point, int based and not as percise as
    /// the float version, which interpolates between our grid points.
    /// </summary>
    /// <param name="x">X</param>
    /// <param name="y">Y</param>
    /// <returns>Float</returns>
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

    /// <summary>
    /// This functions keeps sure we keep in 0-max range,
    /// simple modulate (%) will do this only correctly for positiv values!
    /// </summary>
    private static int ModulateValueInRange(float val, int max)
    {
        if (val < 0.0f)
        {
            return (max - 1) - ((int)(-val) % max);
        }
        else
        {
            return (int)val % max;
        }
    }

    /// <summary>
    /// Get map height at a specific point
    /// </summary>
    /// <param name="x">X</param>
    /// <param name="y">Y</param>
    /// <returns>Float</returns>
    public float GetMapHeight(float x, float y)
    {
        // Rescale to our current dimensions
        x /= MapWidthFactor;
        y /= MapHeightFactor;

        // Interpolate the current position
        int
            // size-1 is because we need +1 for interpolating
            ix = ModulateValueInRange(x, GridWidth - 1),
            iy = ModulateValueInRange(y, GridHeight - 1);

        // Get the position ON the current tile (0.0-1.0)!!!
        float
            fX = x - ((float)((int)x)),
            fY = y - ((float)((int)y));

        int ix2 = (ix + 1) % (GridWidth - 1);
        int iy2 = (iy + 1) % (GridHeight - 1);

        if (fX + fY < 1) // opt. version
        {
            // we are on triangle 1 !!
            //     ------- (f_tile_width-mx)/f_tile_width
            //  0__v___1
            //  |     /
            //  |    /
            //  |---/--- (f_tile_height-my)/f_tile_height
            //  |  /
            //  | /
            //  3/
            return
                mapHeights[ix, iy] + // 0
                fX * (mapHeights[ix2, iy] - mapHeights[ix, iy]) + // 1
                fY * (mapHeights[ix, iy2] - mapHeights[ix, iy]); // 3
        }
        // we are on triangle 1 !!
        // calc height (as above, but a bit more difficult for triangle 1)
        //        1
        //       /|
        //      / |
        //     /  |  my/f_tile_height (fX)
        //    /   |
        //   /    |
        //  3_____2
        //     ^---  mx/f_tile_width  (fY)
        return
            mapHeights[ix2, iy2] + // 2
            (1.0f - fY) * (mapHeights[ix2, iy] - mapHeights[ix2, iy2]) +    // 1
            (1.0f - fX) * (mapHeights[ix, iy2] - mapHeights[ix2, iy2]); // 3
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Create landscape.
    /// This constructor should only be called
    /// from the RacingGame main class!
    /// </summary>
    /// <param name="setLevel">Level we want to load</param>
    internal Landscape(RacingGameManager.Level setLevel)
    {
        byte[] heights = new byte[GridWidth * GridHeight];
        #region Load map height data
        using (Stream file = TitleContainer.OpenStream(
                   "Content\\LandscapeHeights.data"))
        {

            file.Read(heights, 0, GridWidth * GridHeight);
        }

        mapHeights = new float[GridWidth, GridHeight];
    trackObjectManager = new TrackObjectManager(GetMapHeight);
        #endregion

        #region Build tangent vertices
        // Build our tangent vertices
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // Step 1: Calculate position
                int index = x + y * GridWidth;
                Vector3 pos = CalcLandscapePos(x, y, heights);//texData);
                mapHeights[x, y] = pos.Z;
                vertices[index].pos = pos;

                //if (x == 0)
                //    Log.Write("vertices " + y + ": " + pos);

                // Step 2: Calculate all edge vectors (for normals and tangents)
                // This involves quite complicated optimizations and mathematics,
                // hard to explain with just a comment. Read my book :D
                Vector3 edge1 = pos - CalcLandscapePos(x, y + 1, heights);
                Vector3 edge2 = pos - CalcLandscapePos(x + 1, y, heights);
                Vector3 edge3 = pos - CalcLandscapePos(x - 1, y + 1, heights);
                Vector3 edge4 = pos - CalcLandscapePos(x + 1, y + 1, heights);
                Vector3 edge5 = pos - CalcLandscapePos(x - 1, y - 1, heights);

                // Step 3: Calculate normal based on the edges (interpolate
                // from 3 cross products we build from our edges).
                vertices[index].normal = Vector3.Normalize(
                    Vector3.Cross(edge2, edge1) +
                    Vector3.Cross(edge4, edge3) +
                    Vector3.Cross(edge3, edge5));

                // Step 4: Set tangent data, just use edge1
                vertices[index].tangent = Vector3.Normalize(edge1);

                // Step 5: Set texture coordinates, use full 0.0f to 1.0f range!
                vertices[index].uv = new Vector2(
                    //x / (float)(GridWidth - 1),
                    //y / (float)(GridHeight - 1));
                    y / (float)(GridHeight - 1),
                    x / (float)(GridWidth - 1));
            }
        }

        #endregion

        #region Smooth normals
        // Smooth all normals, first copy them over, then smooth everything
        Vector3[,] normalsForSmoothing = new Vector3[GridWidth, GridHeight];
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int index = x + y * GridWidth;
                normalsForSmoothing[x, y] = vertices[index].normal;
            }
        }

        // Time to smooth to normals we just saved
        for (int x = 1; x < GridWidth - 1; x++)
        {
            for (int y = 1; y < GridHeight - 1; y++)
            {
                int index = x + y * GridWidth;

                // Smooth 3x3 normals, but still use old normal to 40% (5 of 13)
                Vector3 normal = vertices[index].normal * 4;
                for (int xAdd = -1; xAdd <= 1; xAdd++)
                {
                    for (int yAdd = -1; yAdd <= 1; yAdd++)
                    {
                        normal += normalsForSmoothing[x + xAdd, y + yAdd];
                    }
                }

                vertices[index].normal = Vector3.Normalize(normal);

                // Also recalculate tangent to let it stay 90 degrees on the normal
                Vector3 helperVector = Vector3.Cross(
                    vertices[index].normal,
                    vertices[index].tangent);
                vertices[index].tangent = Vector3.Cross(
                    helperVector,
                    vertices[index].normal);
            }
        }

        #endregion

        #region Set vertex buffer
        // Set vertex buffer
        // fix
        //vertexBuffer = new VertexBuffer(
        //    BaseGame.Device,
        //    typeof(TangentVertex),
        //    vertices.Length,
        //    ResourceUsage.WriteOnly,
        //    ResourceManagementMode.Automatic);
        //vertexBuffer.SetData(vertices);
        vertexBuffer = new VertexBuffer(
            BaseGame.Device,
            typeof(TangentVertex),
            vertices.Length, 
            BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);
        #endregion

        #region Calc index buffer
        // Calc index buffer (Note: have to use uint, ushort is not sufficiant
        // in our case because we have MANY vertices ^^)
        uint[] indices = new uint[(GridWidth - 1) * (GridHeight - 1) * 6];
        int currentIndex = 0;
        for (int x = 0; x < GridWidth - 1; x++)
        {
            for (int y = 0; y < GridHeight - 1; y++)
            {
                // Set landscape data (Note: Right handed)
                indices[currentIndex + 0] = (uint)(x * GridHeight + y);
                indices[currentIndex + 2] =
                    (uint)((x + 1) * GridHeight + (y + 1));
                indices[currentIndex + 1] = (uint)((x + 1) * GridHeight + y);
                indices[currentIndex + 3] =
                    (uint)((x + 1) * GridHeight + (y + 1));
                indices[currentIndex + 5] = (uint)(x * GridHeight + y);
                indices[currentIndex + 4] = (uint)(x * GridHeight + (y + 1));

                // Add indices
                currentIndex += 6;
            }
        }

        #endregion

        #region Set index buffer
        // fix
        //indexBuffer = new IndexBuffer(
        //    BaseGame.Device,
        //    typeof(uint),
        //    (GridWidth - 1) * (GridHeight - 1) * 6,
        //    ResourceUsage.WriteOnly,
        //    ResourceManagementMode.Automatic);

        indexBuffer = new IndexBuffer(
            BaseGame.Device,
            typeof(uint),
            (GridWidth - 1) * (GridHeight - 1) * 6,
            BufferUsage.WriteOnly);

        indexBuffer.SetData(indices);
        #endregion

        #region Load track (and replay inside ReloadLevel method)
        // Load track based on the level selection and set car pos with
        // help of the ReloadLevel method.
        ReloadLevel(setLevel);
        #endregion

        #region Add city planes
        // Just set one giant plane for the whole city!
        LandscapeObject cityObject = trackObjectManager.FirstBigBuilding;
        if (cityObject != null)
        {
            cityPlane = new PlaneRenderer(
                cityObject.Position,
                new Plane(new Vector3(0, 0, 1), 0.1f),
                cityMat, Math.Min(cityObject.Position.X, cityObject.Position.Y));
        }
        #endregion
    }

    #region Reload level
    /// <summary>
    /// Reload level
    /// </summary>
    /// <param name="setLevel">Level</param>
    internal void ReloadLevel(RacingGameManager.Level setLevel)
    {
        level = setLevel;

        // Load track based on the level selection, do this after
        // we got all the height data because the track might be adjusted.
        if (track == null)
        {
            track = new Track("Track" + level.ToString(), this);
        }
        else
        {
            track.Reload("Track" + level.ToString(), this);
        }

        replayManager.ResetForTrack(level, track);

        brakeTrackManager.Reset();

        // Set car at start pos
        SetCarToStartPosition();

        // Begin game with red start light
        trackObjectManager.ResetStartLight();
    }
    #endregion

    #region Calc landscape position
    /// <summary>
    /// Calc landscape position
    /// </summary>
    /// <param name="x">X</param>
    /// <param name="y">Y</param>
    /// <returns>Vector 3</returns>
    private static Vector3 CalcLandscapePos(int x, int y, byte[] heights)
    {
        // Make sure we stay on the valid map data
        int mapX = x < 0 ? 0 : x >= GridWidth ? GridWidth - 1 : x;
        int mapY = y < 0 ? 0 : y >= GridHeight ? GridHeight - 1 : y;

        // Get height
        float heightPercent = heights[mapX + mapY * GridWidth] / 255.0f;

        // Build landscape position vector
        return new Vector3(
            x * MapWidthFactor,
            y * MapHeightFactor,
            heightPercent * MapZScale);
    }
    #endregion
    #endregion

    #region Dispose
    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    /// <param name="disposing">Disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            trackObjectManager.Dispose();
            mat.Dispose();
            cityMat.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            track.Dispose();
        }
    }
    #endregion

    #region Set car to start pos
    /// <summary>
    /// Set car to start pos
    /// </summary>
    public void SetCarToStartPosition()
    {
        RacingGameManager.Player.SetCarPosition(
            track.StartPosition, track.StartDirection, track.StartUpVector);
        // Camera is set in zooming in method of the Player class.
    }
    #endregion

    #region Render
    /// <summary>
    /// Render landscape (just at the origin)
    /// </summary>
    public void Render()
    {
        // Make sure z buffer is on
        BaseGame.Device.DepthStencilState = DepthStencilState.Default;

        BaseGame.WorldMatrix = Matrix.Identity;

        // Render landscape (pretty easy with all the data we got here)
        ShaderEffect.landscapeNormalMapping.Render(
            mat, "DiffuseWithDetail20",
            new BaseGame.RenderHandler(RenderLandscapeVertices));

        cityPlane.Render();

        // Render track
        track.Render();

        trackObjectManager.Render();

        // Render all brake tracks
        RenderBrakeTracks();
    }

    #region RenderLandscapeVertices
    /// <summary>
    /// Render landscape vertices
    /// </summary>
    private void RenderLandscapeVertices()
    {
        BaseGame.Device.SetVertexBuffer(vertexBuffer);
        BaseGame.Device.Indices = indexBuffer;
        BaseGame.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, (GridWidth - 1) * (GridHeight - 1) * 2);
    }
    #endregion
    #endregion

    #region Generate and use shadow for the landscape
    /// <summary>
    /// Generate shadow
    /// </summary>
    public void GenerateShadow()
    {
        // Don't generate shadow for the landscape, it only receives shadow!

        // Just generate shadows for the road.
        track.GenerateShadow();

        trackObjectManager.GenerateShadows();
    }

    /// <summary>
    /// Use shadow
    /// </summary>
    public void UseShadow()
    {
        // Receive shadow on the landscape, just render it out.
        ShaderEffect.shadowMapping.UpdateCalcShadowWorldMatrix(Matrix.Identity);

        // Render shadows for palms and other objects near the road.
        RenderLandscapeVertices();

        // Also receive shadows for all landscape objects that near our road.
        // This is not really required (still looks good without it), but
        // sometimes objects may have lookthrough-shadows or windmills
        // are usually a problem. This fixes this or makes it at least less
        // noticable.
        trackObjectManager.UseShadows();

        // And the track receives shadow too
        track.UseShadow();
    }
    #endregion

    #region GetTrackPositionMatrix and UpdateCarTrackPosition
    /// <summary>
    /// Get track position matrix, used for the game background and unit tests.
    /// </summary>
    /// <param name="carTrackPos">Car track position</param>
    /// <param name="roadWidth">Road width</param>
    /// <param name="nextRoadWidth">Next road width</param>
    /// <returns>Matrix</returns>
    public Matrix GetTrackPositionMatrix(float carTrackPos,
        out float roadWidth, out float nextRoadWidth)
    {
        return track.GetTrackPositionMatrix(carTrackPos,
            out roadWidth, out nextRoadWidth);
    }

    /// <summary>
    /// Get track position matrix
    /// </summary>
    /// <param name="trackSegmentNum">Track segment number</param>
    /// <param name="trackSegmentPercent">Track segment percent</param>
    /// <param name="roadWidth">Road width</param>
    /// <param name="nextRoadWidth">Next road width</param>
    /// <returns>Matrix</returns>
    public Matrix GetTrackPositionMatrix(
        int trackSegmentNum, float trackSegmentPercent,
        out float roadWidth, out float nextRoadWidth)
    {
        return track.GetTrackPositionMatrix(
            trackSegmentNum, trackSegmentPercent,
            out roadWidth, out nextRoadWidth);
    }

    /// <summary>
    /// Update car track position
    /// </summary>
    /// <param name="carPos">Car position</param>
    /// <param name="trackSegmentNumber">Track segment number</param>
    /// <param name="trackPositionPercent">Track position percent</param>
    public void UpdateCarTrackPosition(
        Vector3 carPos,
        ref int trackSegmentNumber, ref float trackPositionPercent)
    {
        track.UpdateCarTrackPosition(carPos,
            ref trackSegmentNumber, ref trackPositionPercent);
    }
    #endregion

    #region Add and render brake tracks
    /// <summary>
    /// Add brake track
    /// </summary>
    /// <param name="position">Position</param>
    /// <param name="dir">Dir vector</param>
    /// <param name="right">Right vector</param>
    public void AddBrakeTrack(CarPhysics car)
    {
        brakeTrackManager.AddBrakeTrack(car);
    }

    /// <summary>
    /// Render brake tracks
    /// </summary>
    public void RenderBrakeTracks()
    {
        brakeTrackManager.Render();
    }
    #endregion
}