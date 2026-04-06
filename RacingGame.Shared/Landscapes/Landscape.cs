using RacingGame.Graphics;
using RacingGame.Tracks;
using RacingGame.GameLogic;
namespace RacingGame.Landscapes;

/// <summary>
/// Landscape
/// </summary>
public class Landscape : IDisposable
{
    #region Variables
    /// <summary>
    /// Currently loaded level
    /// </summary>
    RacingGameManager.Level level = RacingGameManager.Level.Beginner;

    readonly TerrainRenderer terrainRenderer;
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
    /// City material
    /// </summary>
    /// <returns>Material</returns>
    public Material CityMaterial
    {
        get
        {
            return terrainRenderer.CityMaterial;
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
        return terrainRenderer.GetMapHeight(x, y);
    }

    /// <summary>
    /// Get map height at a specific point
    /// </summary>
    /// <param name="x">X</param>
    /// <param name="y">Y</param>
    /// <returns>Float</returns>
    public float GetMapHeight(float x, float y)
    {
        return terrainRenderer.GetMapHeight(x, y);
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
        terrainRenderer = new TerrainRenderer();
        trackObjectManager = new TrackObjectManager(GetMapHeight);
        ReloadLevel(setLevel);
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

        RefreshLevelRuntimeState();
    }
    #endregion

    void RefreshLevelRuntimeState()
    {
        replayManager.ResetForTrack(level, track);
        brakeTrackManager.Reset();
        SetCarToStartPosition();
        trackObjectManager.ResetStartLight();
        terrainRenderer.UpdateCityPlane(trackObjectManager.FirstBigBuilding);
    }
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
            track.Dispose();
            trackObjectManager.Dispose();
            terrainRenderer.Dispose();
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
        terrainRenderer.Render();

        // Render track
        track.Render();

        trackObjectManager.Render();

        // Render all brake tracks
        RenderBrakeTracks();
    }

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
        terrainRenderer.RenderShadowReceiver();

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