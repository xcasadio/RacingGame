using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Sounds;
using RacingGame.Tracks;
using Model = RacingGame.Graphics.Model;

namespace RacingGame.Landscapes;

/// <summary>
/// Manages landscape object placement, rendering and shadow participation.
/// </summary>
internal sealed class TrackObjectManager : IDisposable
{
    readonly Func<float, float, float> getMapHeight;
    readonly List<LandscapeObject> landscapeObjects = new List<LandscapeObject>();
    readonly List<LandscapeObject> nearTrackObjects = new List<LandscapeObject>();
    LandscapeObject startLightObject = null;

    readonly Model[] landscapeModels = new Model[]
    {
        new Model("StartLight"),
        new Model("StartLight2"),
        new Model("StartLight3"),
        new Model("Blockade"),
        new Model("Blockade2"),
        new Model("Hydrant"),
        new Model("Kaktus"),
        new Model("Kaktus2"),
        new Model("KaktusBenny"),
        new Model("KaktusSeg"),
        new Model("AlphaDeadTree"),
        new Model("AlphaPalm"),
        new Model("AlphaPalm2"),
        new Model("AlphaPalm3"),
        new Model("AlphaPalmSmall"),
        new Model("Laterne"),
        new Model("Laterne2Sides"),
        new Model("Trashcan"),
        new Model("Roadsign"),
        new Model("Roadsign2"),
        new Model("Goal"),
        new Model("Building"),
        new Model("Building2"),
        new Model("Building3"),
        new Model("Building4"),
        new Model("Building5"),
        new Model("OilPump"),
        new Model("OilTanks"),
        new Model("RoadColumnSegment"),
        new Model("Windmill"),
        new Model("Ruin"),
        new Model("RuinHouse"),
        new Model("SandCastle"),
        new Model("Banner"),
        new Model("Banner2"),
        new Model("Banner3"),
        new Model("Banner4"),
        new Model("Banner5"),
        new Model("Banner6"),
        new Model("Sign"),
        new Model("Sign2"),
        new Model("SignWarning"),
        new Model("SignCurveLeft"),
        new Model("SignCurveRight"),
        new Model("SharpRock"),
        new Model("SharpRock2"),
        new Model("Stone4"),
        new Model("Stone5"),
        new Model("AlphaTrain"),
        new Model("GuardRailHolder"),
        new Model("Hotel01"),
        new Model("Hotel02"),
        new Model("Casino01"),
    };

    readonly TrackCombiModels[] combos = new TrackCombiModels[]
    {
        new TrackCombiModels("CombiPalms"),
        new TrackCombiModels("CombiPalms2"),
        new TrackCombiModels("CombiRuins"),
        new TrackCombiModels("CombiRuins2"),
        new TrackCombiModels("CombiStones"),
        new TrackCombiModels("CombiStones2"),
        new TrackCombiModels("CombiOilTanks"),
        new TrackCombiModels("CombiSandCastle"),
        new TrackCombiModels("CombiBuildings"),
        new TrackCombiModels("CombiHotels"),
    };

    public string[] AutoGenerationNames { get; } = new string[]
    {
        "CombiPalms",
        "CombiPalms2",
        "CombiRuins",
        "CombiRuins2",
        "CombiStones",
        "CombiStones2",
        "Kaktus",
        "Kaktus2",
        "KaktusBenny",
        "KaktusSeg",
        "AlphaDeadTree",
        "AlphaPalm",
        "AlphaPalm2",
        "AlphaPalm3",
        "AlphaPalmSmall",
        "Laterne2Sides",
        "Trashcan",
        "OilPump",
        "OilTanks",
        "RoadColumnSegment",
        "Windmill",
        "Ruin",
        "RuinHouse",
        "Sign",
        "Sign2",
        "SharpRock",
        "SharpRock2",
        "Stone4",
        "Stone5",
        "Casino01",
    };

    public TrackObjectManager(Func<float, float, float> setGetMapHeight)
    {
        getMapHeight = setGetMapHeight ?? throw new ArgumentNullException("setGetMapHeight");
    }

    public IReadOnlyList<LandscapeObject> LandscapeObjects
    {
        get
        {
            return landscapeObjects;
        }
    }

    public IReadOnlyList<LandscapeObject> NearTrackObjects
    {
        get
        {
            return nearTrackObjects;
        }
    }

    public LandscapeObject FirstBigBuilding
    {
        get
        {
            for (int num = 0; num < landscapeObjects.Count; num++)
            {
                if (landscapeObjects[num].IsBigBuilding)
                {
                    return landscapeObjects[num];
                }
            }

            return null;
        }
    }

    public void ReplaceStartLightObject(int number)
    {
        if (number < 0 || number >= 3)
        {
            number = 0;
        }

        if (startLightObject != null)
        {
            if (number == 2)
            {
                Sound.Play(Sound.Sounds.Bleep);
            }
            else
            {
                Sound.Play(Sound.Sounds.Beep);
            }

            startLightObject.ChangeModel(landscapeModels[number]);
        }
    }

    public void ResetStartLight()
    {
        if (startLightObject != null)
        {
            startLightObject.ChangeModel(landscapeModels[0]);
        }
    }

    public void KillAllLoadedObjects()
    {
        landscapeObjects.Clear();
        nearTrackObjects.Clear();
        startLightObject = null;
    }

    public void AddObjectToRender(string modelName, Matrix renderMatrix,
        bool isNearTrackForShadowGeneration)
    {
        if (modelName == "OilWell")
        {
            modelName = "OilPump";
        }
        else if (modelName == "PalmSmall")
        {
            modelName = "AlphaPalmSmall";
        }
        else if (modelName == "AlphaPalm4")
        {
            modelName = "AlphaPalmSmall";
        }
        else if (modelName == "Palm")
        {
            modelName = "AlphaPalm";
        }
        else if (modelName == "Casino")
        {
            modelName = "Casino01";
        }
        else if (modelName == "Combi")
        {
            modelName = "CombiPalms";
        }

        if (modelName.ToLower() == "windmill" ||
            modelName.ToLower().Contains("hotel") ||
            modelName.ToLower().Contains("building") ||
            modelName.ToLower().Contains("casino01"))
        {
            isNearTrackForShadowGeneration = true;
        }

        for (int num = 0; num < combos.Length; num++)
        {
            TrackCombiModels combi = combos[num];
            if (combi.Name == modelName)
            {
                combi.AddAllModels(AddObjectFromCombi, renderMatrix);
                return;
            }
        }

        Model foundModel = null;
        for (int num = 0; num < landscapeModels.Length; num++)
        {
            Model model = landscapeModels[num];
            if (model.Name == modelName)
            {
                foundModel = model;
                break;
            }
        }

        if (foundModel != null)
        {
            Vector3 modelPos = renderMatrix.Translation;
            float landscapeHeight = getMapHeight(modelPos.X, modelPos.Y);
            if (modelPos.Z < landscapeHeight)
            {
                modelPos.Z = landscapeHeight;
                renderMatrix.Translation = modelPos;
            }

            if (modelName.StartsWith("Banner") == false &&
                modelName.StartsWith("Sign") == false &&
                modelName.StartsWith("StartLight") == false)
            {
                for (int num = 0; num < landscapeObjects.Count; num++)
                {
                    if (Vector3.DistanceSquared(
                            landscapeObjects[num].Position, modelPos) <
                        foundModel.Size * foundModel.Size / 4)
                    {
                        return;
                    }
                }
            }

            LandscapeObject newObject =
                new LandscapeObject(foundModel,
                    Matrix.CreateScale(1.2f) * renderMatrix);
            landscapeObjects.Add(newObject);

            if (isNearTrackForShadowGeneration)
            {
                nearTrackObjects.Add(newObject);
            }

            if (modelName.StartsWith("StartLight"))
            {
                startLightObject = newObject;
            }
        }
#if DEBUG
        else if (modelName.Contains("Track") == false)
        {
            Log.Write("Landscape model " + modelName + " is not supported and " +
                      "can't be added for rendering!");
        }
#endif
    }

    public void AddObjectToRender(string modelName,
        float rotation, Vector3 trackPos, Vector3 trackRight,
        float distance)
    {
        float objSize = 1;

        for (int num = 0; num < combos.Length; num++)
        {
            TrackCombiModels combi = combos[num];
            if (combi.Name == modelName)
            {
                objSize = combi.Size;
                break;
            }
        }

        for (int num = 0; num < landscapeModels.Length; num++)
        {
            Model model = landscapeModels[num];
            if (model.Name == modelName)
            {
                objSize = model.Size;
                break;
            }
        }

        if (distance > 0 &&
            distance - 10 < objSize)
        {
            distance += objSize;
        }

        if (distance < 0 &&
            distance + 10 > -objSize)
        {
            distance -= objSize;
        }

        AddObjectToRender(modelName,
            Matrix.CreateRotationZ(rotation) *
            Matrix.CreateTranslation(
                trackPos + trackRight * distance + new Vector3(0, 0, -100)), false);
    }

    public void AddObjectToRender(string modelName, Vector3 renderPos)
    {
        AddObjectToRender(modelName, Matrix.CreateTranslation(renderPos), false);
    }

    public void Render()
    {
        for (int num = 0; num < landscapeObjects.Count; num++)
        {
            landscapeObjects[num].Render();
        }
    }

    public void GenerateShadows()
    {
        for (int num = 0; num < nearTrackObjects.Count; num++)
        {
            nearTrackObjects[num].GenerateShadows();
        }
    }

    public void UseShadows()
    {
        if (BaseGame.HighDetail)
        {
            for (int num = 0; num < nearTrackObjects.Count; num++)
            {
                if (nearTrackObjects[num].IsBanner == false)
                {
                    nearTrackObjects[num].UseShadows();
                }
            }
        }
    }

    public void Dispose()
    {
        for (int num = 0; num < landscapeModels.Length; num++)
        {
            landscapeModels[num].Dispose();
        }
    }

    void AddObjectFromCombi(string modelName, Matrix renderMatrix)
    {
        AddObjectToRender(modelName, renderMatrix, false);
    }
}