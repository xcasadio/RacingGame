using Model = RacingGame.Graphics.Model;

namespace RacingGame.Landscapes;

/// <summary>
/// Landscape object.
/// </summary>
internal class LandscapeObject
{
    Model model;
    Matrix matrix;
    bool isBanner = false;

    public void ChangeModel(Model setNewModel)
    {
        model = setNewModel;
    }

    public bool IsBigBuilding
    {
        get
        {
            return model.Name.ToLower().Contains("hotel") ||
                   model.Name.ToLower().Contains("building");
        }
    }

    public bool IsBanner
    {
        get
        {
            return isBanner;
        }
    }

    public Vector3 Position
    {
        get
        {
            return matrix.Translation;
        }
    }

    public float Size
    {
        get
        {
            return model.Size;
        }
    }

    public LandscapeObject(Model setModel, Matrix setMatrix)
    {
        if (setModel == null)
        {
            throw new ArgumentNullException("setModel");
        }

        model = setModel;
        matrix = setMatrix;
        isBanner = model.Name.ToLower().Contains("banner") ||
                   model.Name.ToLower().Contains("sign");
    }

    public void Render()
    {
        model.Render(matrix);
    }

    public void GenerateShadows()
    {
        model.GenerateShadow(matrix);
    }

    public void UseShadows()
    {
        model.UseShadow(matrix);
    }
}