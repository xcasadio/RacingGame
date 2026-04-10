using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Components;

[DisplayName("Race Checkpoint Trigger")]
public sealed class RaceCheckpointTriggerComponent : SceneComponent
{
    public float HalfWidth { get; set; } = 6.0f;

    public float HalfHeight { get; set; } = 4.0f;

    public float HalfDepth { get; set; } = 2.5f;

    public RaceCheckpointTriggerComponent()
    {
    }

    private RaceCheckpointTriggerComponent(RaceCheckpointTriggerComponent other) : base(other)
    {
        HalfWidth = other.HalfWidth;
        HalfHeight = other.HalfHeight;
        HalfDepth = other.HalfDepth;
    }

    public override EntityComponent Clone()
    {
        return new RaceCheckpointTriggerComponent(this);
    }

    public bool IsTriggered(Vector3 previousPosition, Vector3 currentPosition)
    {
        GetWorldAxes(out Vector3 center, out Vector3 right, out Vector3 up, out Vector3 forward);

        if (TryCrossTriggerPlane(previousPosition, currentPosition, center, right, up, forward))
        {
            return true;
        }

        bool previousInside = ContainsPoint(previousPosition, center, right, up, forward);
        bool currentInside = ContainsPoint(currentPosition, center, right, up, forward);
        if (previousInside || !currentInside)
        {
            return false;
        }

        Vector3 movement = currentPosition - previousPosition;
        return movement.LengthSquared() > 0.0001f && Vector3.Dot(movement, forward) > 0.0f;
    }

    public override BoundingBox GetBoundingBox()
    {
        GetWorldAxes(out Vector3 center, out Vector3 right, out Vector3 up, out Vector3 forward);

        Vector3 rightExtent = right * HalfWidth;
        Vector3 upExtent = up * HalfHeight;
        Vector3 forwardExtent = forward * HalfDepth;

        Vector3[] corners =
        [
            center - rightExtent - upExtent - forwardExtent,
            center - rightExtent - upExtent + forwardExtent,
            center - rightExtent + upExtent - forwardExtent,
            center - rightExtent + upExtent + forwardExtent,
            center + rightExtent - upExtent - forwardExtent,
            center + rightExtent - upExtent + forwardExtent,
            center + rightExtent + upExtent - forwardExtent,
            center + rightExtent + upExtent + forwardExtent,
        ];

        Vector3 min = corners[0];
        Vector3 max = corners[0];
        for (int index = 1; index < corners.Length; index++)
        {
            min = Vector3.Min(min, corners[index]);
            max = Vector3.Max(max, corners[index]);
        }

        return new BoundingBox(min, max);
    }

    private void GetWorldAxes(out Vector3 center, out Vector3 right, out Vector3 up, out Vector3 forward)
    {
        Matrix worldMatrix = WorldMatrixNoScale;
        center = Vector3.Transform(Vector3.Zero, worldMatrix);

        right = NormalizeOrFallback(Vector3.TransformNormal(Vector3.Right, worldMatrix), Vector3.Right);
        up = NormalizeOrFallback(Vector3.TransformNormal(Vector3.Up, worldMatrix), Vector3.Up);
        forward = NormalizeOrFallback(Vector3.TransformNormal(Vector3.Forward, worldMatrix), Vector3.Forward);

        right = NormalizeOrFallback(Vector3.Cross(up, forward), right);
        forward = NormalizeOrFallback(Vector3.Cross(right, up), forward);
    }

    private bool ContainsPoint(Vector3 point, Vector3 center, Vector3 right, Vector3 up, Vector3 forward)
    {
        Vector3 delta = point - center;
        float lateral = Vector3.Dot(delta, right);
        float vertical = Vector3.Dot(delta, up);
        float longitudinal = Vector3.Dot(delta, forward);

        return Math.Abs(lateral) <= HalfWidth
            && Math.Abs(vertical) <= HalfHeight
            && Math.Abs(longitudinal) <= HalfDepth;
    }

    private bool TryCrossTriggerPlane(Vector3 previousPosition, Vector3 currentPosition, Vector3 center, Vector3 right, Vector3 up, Vector3 forward)
    {
        float previousDistance = Vector3.Dot(previousPosition - center, forward);
        float currentDistance = Vector3.Dot(currentPosition - center, forward);
        if (previousDistance >= 0.0f || currentDistance < 0.0f)
        {
            return false;
        }

        float denominator = currentDistance - previousDistance;
        if (Math.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        float amount = -previousDistance / denominator;
        if (amount < 0.0f || amount > 1.0f)
        {
            return false;
        }

        Vector3 crossingPoint = Vector3.Lerp(previousPosition, currentPosition, amount);
        Vector3 delta = crossingPoint - center;
        float lateral = Vector3.Dot(delta, right);
        float vertical = Vector3.Dot(delta, up);
        return Math.Abs(lateral) <= HalfWidth
            && Math.Abs(vertical) <= HalfHeight;
    }

    private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
    {
        if (value.LengthSquared() < 0.0001f)
        {
            value = fallback;
        }

        if (value.LengthSquared() < 0.0001f)
        {
            return Vector3.Forward;
        }

        value.Normalize();
        return value;
    }
}