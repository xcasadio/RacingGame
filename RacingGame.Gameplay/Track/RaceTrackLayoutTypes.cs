using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Track;

/// <summary>A point of the road spline, in legacy space.</summary>
internal readonly record struct RaceTrackRoadPoint(
    Vector3 Position,
    Vector3 Right,
    Vector3 Up,
    Vector3 Direction,
    float TextureU,
    float RoadWidth);

/// <summary>A point of a guard rail, in legacy space.</summary>
internal readonly record struct RaceTrackRailPoint(Vector3 Position, Vector3 Right, Vector3 Direction, Vector3 Up);

/// <summary>A model placed at a world transform in runtime space.</summary>
internal readonly record struct RaceTrackModelPlacement(string ModelName, Matrix Transform);

/// <summary>
/// A column, in legacy space: its top <paramref name="Position"/> on the road, the point spaces of its top and
/// bottom, and the landscape height under it.
/// </summary>
internal readonly record struct RaceTrackColumnPlacement(Vector3 Position, Matrix TopSpace, Matrix BottomSpace, float GroundHeight);

internal sealed record RaceCheckpointTriggerDefinition(
    Vector3 Position,
    Quaternion Orientation,
    float HalfWidth,
    float HalfHeight,
    float HalfDepth);

internal sealed record RaceTrackStartPose(
    Vector3 Position,
    Quaternion Orientation,
    Vector3 Forward,
    Vector3 Up);
