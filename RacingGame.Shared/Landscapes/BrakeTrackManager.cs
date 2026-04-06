using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Shaders;

namespace RacingGame.Landscapes;

/// <summary>
/// Manages brake track generation and rendering.
/// </summary>
internal sealed class BrakeTrackManager
{
    Vector3 lastAddedTrackPos = new Vector3(-1000, -1000, -1000);
    const int MaxBrakeTrackVertices = 6 * 140;
    const float RaiseBreakTracksAmount = 0.2f;

    readonly List<TangentVertex> brakeTracksVertices = new List<TangentVertex>();
    TangentVertex[] brakeTracksVerticesArray = null;

    public void Reset()
    {
        brakeTracksVertices.Clear();
        brakeTracksVerticesArray = null;
        lastAddedTrackPos = new Vector3(-1000, -1000, -1000);
    }

    public void AddBrakeTrack(CarPhysics car)
    {
        Vector3 position = car.CarPosition + car.CarDirection * 1.25f;

        if (Vector3.DistanceSquared(position, lastAddedTrackPos) < 0.024f ||
            brakeTracksVertices.Count > MaxBrakeTrackVertices)
        {
            return;
        }

        lastAddedTrackPos = position;

        const float width = 2.4f;
        const float length = 4.5f;
        float maxDist =
            (float)Math.Sqrt(width * width + length * length) / 2 - 0.35f;

        for (int num = 0; num < brakeTracksVertices.Count; num++)
        {
            if (Vector3.DistanceSquared(brakeTracksVertices[num].pos, position) <
                maxDist * maxDist)
            {
                return;
            }
        }

        position += Vector3.Normalize(car.CarUpVector) * RaiseBreakTracksAmount;

        TangentVertex[] newVertices = new TangentVertex[]
        {
            new TangentVertex(
                position - car.CarRight * width / 2 - car.CarDirection * length / 2, 0, 0,
                car.CarUpVector, car.CarRight),
            new TangentVertex(
                position - car.CarRight * width / 2 + car.CarDirection * length / 2, 0, 5,
                car.CarUpVector, car.CarRight),
            new TangentVertex(
                position + car.CarRight * width / 2 + car.CarDirection * length / 2, 1, 5,
                car.CarUpVector, car.CarRight),
            new TangentVertex(
                position - car.CarRight * width / 2 - car.CarDirection * length / 2, 0, 0,
                car.CarUpVector, car.CarRight),
            new TangentVertex(
                position + car.CarRight * width / 2 + car.CarDirection * length / 2, 1, 5,
                car.CarUpVector, car.CarRight),
            new TangentVertex(
                position + car.CarRight * width / 2 - car.CarDirection * length / 2, 1, 0,
                car.CarUpVector, car.CarRight),
        };

        brakeTracksVertices.AddRange(newVertices);
        brakeTracksVerticesArray = brakeTracksVertices.ToArray();
    }

    public void Render()
    {
        if (brakeTracksVerticesArray == null)
        {
            return;
        }

        BaseGame.SetAlphaBlendingEnabled(true);
        BaseGame.WorldMatrix = Matrix.Identity;
        ShaderEffect.lighting.Render(
            RacingGameManager.BrakeTrackMaterial,
            "Diffuse20",
            delegate
            {
                BaseGame.Device.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    brakeTracksVerticesArray, 0, brakeTracksVerticesArray.Length / 3);
            });
    }
}