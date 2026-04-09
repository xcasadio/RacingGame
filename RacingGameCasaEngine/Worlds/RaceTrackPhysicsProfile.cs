using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Worlds;

internal sealed class RaceTrackPhysicsProfile
{
    private readonly RoadSegment[] _segments;

    public RaceTrackPhysicsProfile(IReadOnlyList<RaceTrackPhysicsPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < 2)
        {
            _segments = [];
            return;
        }

        _segments = new RoadSegment[points.Count - 1];
        for (int index = 0; index < points.Count - 1; index++)
        {
            _segments[index] = new RoadSegment(index, points[index], points[index + 1]);
        }
    }

    public bool TrySample(Vector3 position, int segmentHint, out RaceTrackSurfaceSample sample)
    {
        if (_segments.Length == 0)
        {
            sample = default;
            return false;
        }

        if (segmentHint >= 0
            && segmentHint < _segments.Length
            && TrySampleRange(
                position,
                Math.Max(0, segmentHint - 16),
                Math.Min(_segments.Length - 1, segmentHint + 16),
                out sample))
        {
            return true;
        }

        return TrySampleRange(position, 0, _segments.Length - 1, out sample);
    }

    private bool TrySampleRange(Vector3 position, int startIndex, int endIndex, out RaceTrackSurfaceSample sample)
    {
        bool found = false;
        float bestDistanceSquared = float.MaxValue;
        sample = default;

        for (int index = startIndex; index <= endIndex; index++)
        {
            RaceTrackSurfaceSample candidate = _segments[index].Sample(position);
            if (!found || candidate.DistanceSquared < bestDistanceSquared)
            {
                sample = candidate;
                bestDistanceSquared = candidate.DistanceSquared;
                found = true;
            }
        }

        return found;
    }

    private readonly struct RoadSegment
    {
        public RoadSegment(int index, RaceTrackPhysicsPoint start, RaceTrackPhysicsPoint end)
        {
            Index = index;
            Start = start;
            End = end;
        }

        private int Index { get; }

        private RaceTrackPhysicsPoint Start { get; }

        private RaceTrackPhysicsPoint End { get; }

        public RaceTrackSurfaceSample Sample(Vector3 position)
        {
            Vector3 segment = End.Center - Start.Center;
            float segmentLengthSquared = segment.LengthSquared();
            float amount = segmentLengthSquared > 0.0001f
                ? Math.Clamp(Vector3.Dot(position - Start.Center, segment) / segmentLengthSquared, 0f, 1f)
                : 0f;

            Vector3 center = Vector3.Lerp(Start.Center, End.Center, amount);
            Vector3 up = NormalizeOrFallback(Vector3.Lerp(Start.Up, End.Up, amount), Vector3.Up);
            Vector3 right = NormalizeOrFallback(Vector3.Lerp(Start.Right, End.Right, amount), Vector3.Right);
            Vector3 forward = NormalizeOrFallback(Vector3.Lerp(Start.Forward, End.Forward, amount), segment);

            right = NormalizeOrFallback(Vector3.Cross(up, forward), right);
            forward = NormalizeOrFallback(Vector3.Cross(right, up), forward);

            float halfWidth = MathHelper.Lerp(Start.HalfWidth, End.HalfWidth, amount);
            Vector3 localOffset = position - center;
            float lateralOffset = Vector3.Dot(localOffset, right);
            float clampedLateralOffset = Math.Clamp(lateralOffset, -halfWidth, halfWidth);
            Vector3 supportPoint = center + right * clampedLateralOffset;
            float distanceSquared = Vector3.DistanceSquared(position, supportPoint);

            return new RaceTrackSurfaceSample(
                Index,
                center,
                forward,
                up,
                right,
                halfWidth,
                lateralOffset,
                supportPoint,
                distanceSquared);
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
}

internal readonly struct RaceTrackPhysicsPoint
{
    public RaceTrackPhysicsPoint(Vector3 center, Vector3 forward, Vector3 up, Vector3 right, float halfWidth)
    {
        Center = center;
        Forward = forward;
        Up = up;
        Right = right;
        HalfWidth = halfWidth;
    }

    public Vector3 Center { get; }

    public Vector3 Forward { get; }

    public Vector3 Up { get; }

    public Vector3 Right { get; }

    public float HalfWidth { get; }
}

internal readonly struct RaceTrackSurfaceSample
{
    public RaceTrackSurfaceSample(
        int segmentIndex,
        Vector3 center,
        Vector3 forward,
        Vector3 up,
        Vector3 right,
        float halfWidth,
        float lateralOffset,
        Vector3 supportPoint,
        float distanceSquared)
    {
        SegmentIndex = segmentIndex;
        Center = center;
        Forward = forward;
        Up = up;
        Right = right;
        HalfWidth = halfWidth;
        LateralOffset = lateralOffset;
        SupportPoint = supportPoint;
        DistanceSquared = distanceSquared;
    }

    public int SegmentIndex { get; }

    public Vector3 Center { get; }

    public Vector3 Forward { get; }

    public Vector3 Up { get; }

    public Vector3 Right { get; }

    public float HalfWidth { get; }

    public float LateralOffset { get; }

    public Vector3 SupportPoint { get; }

    public float DistanceSquared { get; }

    public bool IsWithinRoadBounds => Math.Abs(LateralOffset) <= HalfWidth + 0.001f;
}