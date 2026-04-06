namespace RacingGame.Landscapes;

/// <summary>
/// Placeholder for landscape object responsibilities while the facade is frozen.
/// </summary>
internal sealed class TrackObjectManager : IDisposable
{
    static readonly IReadOnlyList<LandscapeObject> EmptyObjects = Array.Empty<LandscapeObject>();

    public string[] AutoGenerationNames { get; } = Array.Empty<string>();

    public IReadOnlyList<LandscapeObject> LandscapeObjects
    {
        get
        {
            return EmptyObjects;
        }
    }

    public IReadOnlyList<LandscapeObject> NearTrackObjects
    {
        get
        {
            return EmptyObjects;
        }
    }

    public LandscapeObject FirstBigBuilding
    {
        get
        {
            return null;
        }
    }

    public void ReplaceStartLightObject(int number)
    {
    }

    public void ResetStartLight()
    {
    }

    public void KillAllLoadedObjects()
    {
    }

    public void AddObjectToRender(string modelName, Matrix renderMatrix,
        bool isNearTrackForShadowGeneration)
    {
    }

    public void AddObjectToRender(string modelName,
        float rotation, Vector3 trackPos, Vector3 trackRight,
        float distance)
    {
    }

    public void AddObjectToRender(string modelName, Vector3 renderPos)
    {
    }

    public void Render()
    {
    }

    public void GenerateShadows()
    {
    }

    public void UseShadows()
    {
    }

    public void Dispose()
    {
    }
}