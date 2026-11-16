using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Measures the scene the port built for a track against the original's scene.json (written by RacingGame.exe
/// --capture-track-audit next to views.json): the generated road points and every placed model, both in the
/// original's coordinates (Z up). Writes scene-measure-&lt;Track&gt;.txt and returns a one-line summary.
/// </summary>
internal static class ReferenceSceneMeasure
{
    private const float PointTolerance = 0.05f;
    private const float ObjectMatchDistance = 1f;
    private const int WorstLines = 12;

    private readonly record struct ReferencePoint(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Direction, float RoadWidth);

    internal static string Measure(string sceneFilePath, string trackName, ReferenceSceneSnapshot port, string outputDirectory)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(sceneFilePath));
        JsonElement? track = null;
        foreach (JsonElement candidate in document.RootElement.GetProperty("tracks").EnumerateArray())
        {
            if (string.Equals(candidate.GetProperty("name").GetString(), trackName, StringComparison.OrdinalIgnoreCase))
            {
                track = candidate;
                break;
            }
        }

        if (track == null)
        {
            return $"scene.json has no track named {trackName}";
        }

        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"Scene measure for {trackName} (original's coordinates, Z up, metres)");
        report.AppendLine();
        string pointSummary = MeasurePoints(track.Value, port, report);
        report.AppendLine();
        string objectSummary = MeasureObjects(track.Value, port, report);

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, $"scene-measure-{trackName}.txt"), report.ToString());
        return $"{pointSummary}; {objectSummary}";
    }

    private static string MeasurePoints(JsonElement track, ReferenceSceneSnapshot port, StringBuilder report)
    {
        var reference = new List<ReferencePoint>();
        foreach (JsonElement point in track.GetProperty("trackPoints").EnumerateArray())
        {
            reference.Add(new ReferencePoint(
                ReadVector(point.GetProperty("position")),
                ReadVector(point.GetProperty("right")),
                ReadVector(point.GetProperty("up")),
                ReadVector(point.GetProperty("direction")),
                point.GetProperty("roadWidth").GetSingle()));
        }

        report.AppendLine("## Road points");
        report.AppendLine(CultureInfo.InvariantCulture, $"original {reference.Count}, port {port.RoadPoints.Count}");
        int count = Math.Min(reference.Count, port.RoadPoints.Count);
        if (count == 0)
        {
            return "road points: none to compare";
        }

        float maxDistance = 0f;
        double sumDistance = 0d;
        int firstBeyond = -1;
        int beyondCount = 0;
        float maxRightAngle = 0f;
        float maxUpAngle = 0f;
        float maxWidthDelta = 0f;
        var lines = new List<(float Distance, string Text)>();
        for (int index = 0; index < count; index++)
        {
            ReferencePoint expected = reference[index];
            ReferenceRoadPoint actual = port.RoadPoints[index];
            Vector3 delta = actual.Position - expected.Position;
            float distance = delta.Length();
            maxDistance = Math.Max(maxDistance, distance);
            sumDistance += distance;
            maxRightAngle = Math.Max(maxRightAngle, AngleDegrees(expected.Right, actual.Right));
            maxUpAngle = Math.Max(maxUpAngle, AngleDegrees(expected.Up, actual.Up));
            maxWidthDelta = Math.Max(maxWidthDelta, Math.Abs(expected.RoadWidth - actual.RoadWidth));
            if (distance > PointTolerance)
            {
                beyondCount++;
                if (firstBeyond < 0)
                {
                    firstBeyond = index;
                }
            }

            lines.Add((distance, string.Create(CultureInfo.InvariantCulture,
                $"  [{index}] distance {distance:0.000} (along right {Vector3.Dot(delta, SafeNormalize(expected.Right)):0.000}, up {Vector3.Dot(delta, SafeNormalize(expected.Up)):0.000}, direction {Vector3.Dot(delta, SafeNormalize(expected.Direction)):0.000}), right {AngleDegrees(expected.Right, actual.Right):0.00} deg, up {AngleDegrees(expected.Up, actual.Up):0.00} deg, width {expected.RoadWidth:0.000} -> {actual.RoadWidth:0.000}")));
        }

        report.AppendLine(CultureInfo.InvariantCulture,
            $"position: max {maxDistance:0.000}, mean {sumDistance / count:0.000}, {beyondCount} point(s) beyond {PointTolerance:0.00}, first at index {firstBeyond}");
        report.AppendLine(CultureInfo.InvariantCulture,
            $"right vector: max {maxRightAngle:0.00} deg; up vector: max {maxUpAngle:0.00} deg; road width: max delta {maxWidthDelta:0.000}");
        if (firstBeyond >= 0)
        {
            report.AppendLine("first points beyond the tolerance:");
            for (int index = firstBeyond; index < Math.Min(count, firstBeyond + WorstLines); index++)
            {
                report.AppendLine(lines[index].Text);
            }

            report.AppendLine("worst points:");
            foreach ((float _, string text) in lines.OrderByDescending(line => line.Distance).Take(WorstLines))
            {
                report.AppendLine(text);
            }
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"road points {reference.Count}/{port.RoadPoints.Count}, max {maxDistance:0.000} m, {beyondCount} beyond {PointTolerance:0.00} m (first {firstBeyond}), right {maxRightAngle:0.00} deg, up {maxUpAngle:0.00} deg");
    }

    private static string MeasureObjects(JsonElement track, ReferenceSceneSnapshot port, StringBuilder report)
    {
        var reference = new List<ReferencePlacedObject>();
        foreach (JsonElement placed in track.GetProperty("objects").EnumerateArray())
        {
            JsonElement m = placed.GetProperty("matrix");
            reference.Add(new ReferencePlacedObject(
                placed.GetProperty("model").GetString() ?? string.Empty,
                new Matrix(
                    m[0].GetSingle(), m[1].GetSingle(), m[2].GetSingle(), m[3].GetSingle(),
                    m[4].GetSingle(), m[5].GetSingle(), m[6].GetSingle(), m[7].GetSingle(),
                    m[8].GetSingle(), m[9].GetSingle(), m[10].GetSingle(), m[11].GetSingle(),
                    m[12].GetSingle(), m[13].GetSingle(), m[14].GetSingle(), m[15].GetSingle()),
                ReadOptionalFlag(placed, "castsShadow"),
                ReadOptionalFlag(placed, "receivesShadow")));
        }

        report.AppendLine("## Placed objects");
        report.AppendLine(CultureInfo.InvariantCulture, $"original {reference.Count}, port {port.PlacedObjects.Count}");
        report.AppendLine("model: original / port / matched within 1 m / max distance of the matched / max rotation delta / max scale ratio delta");

        var used = new bool[port.PlacedObjects.Count];
        bool flagsKnown = reference.Count > 0 && reference.All(placed => placed.CastsShadow.HasValue && placed.ReceivesShadow.HasValue);
        int castMismatches = 0;
        int receiveMismatches = 0;
        int flaggedPairs = 0;
        var flagLines = new List<string>();
        var missing = new List<string>();
        int matchedTotal = 0;
        foreach (IGrouping<string, ReferencePlacedObject> group in reference
                     .GroupBy(placed => placed.ModelName, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            int portCount = port.PlacedObjects.Count(placed => string.Equals(placed.ModelName, group.Key, StringComparison.OrdinalIgnoreCase));
            int matched = 0;
            float maxDistance = 0f;
            float maxRotation = 0f;
            float maxScaleDelta = 0f;
            foreach (ReferencePlacedObject expected in group)
            {
                int nearest = -1;
                float nearestDistance = float.MaxValue;
                for (int index = 0; index < port.PlacedObjects.Count; index++)
                {
                    if (used[index] || !string.Equals(port.PlacedObjects[index].ModelName, group.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(port.PlacedObjects[index].LegacyTransform.Translation, expected.LegacyTransform.Translation);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = index;
                    }
                }

                if (nearest < 0 || nearestDistance > ObjectMatchDistance)
                {
                    Vector3 p = expected.LegacyTransform.Translation;
                    missing.Add(string.Create(CultureInfo.InvariantCulture,
                        $"  {group.Key} at ({p.X:0.0}, {p.Y:0.0}, {p.Z:0.0}), nearest same model in the port: {(nearest < 0 ? "none" : nearestDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m")}"));
                    continue;
                }

                used[nearest] = true;
                matched++;
                Matrix actual = port.PlacedObjects[nearest].LegacyTransform;
                if (flagsKnown)
                {
                    ReferencePlacedObject actualObject = port.PlacedObjects[nearest];
                    flaggedPairs++;
                    bool castDiffers = actualObject.CastsShadow != expected.CastsShadow;
                    bool receiveDiffers = actualObject.ReceivesShadow != expected.ReceivesShadow;
                    castMismatches += castDiffers ? 1 : 0;
                    receiveMismatches += receiveDiffers ? 1 : 0;
                    if (castDiffers || receiveDiffers)
                    {
                        Vector3 q = expected.LegacyTransform.Translation;
                        flagLines.Add(string.Create(CultureInfo.InvariantCulture,
                            $"  {group.Key} at ({q.X:0.0}, {q.Y:0.0}, {q.Z:0.0}): casts original {expected.CastsShadow} / port {actualObject.CastsShadow}, receives original {expected.ReceivesShadow} / port {actualObject.ReceivesShadow}"));
                    }
                }

                maxDistance = Math.Max(maxDistance, nearestDistance);
                maxRotation = Math.Max(maxRotation, Math.Max(
                    AngleDegrees(expected.LegacyTransform.Right, actual.Right),
                    Math.Max(
                        AngleDegrees(expected.LegacyTransform.Up, actual.Up),
                        AngleDegrees(expected.LegacyTransform.Forward, actual.Forward))));
                float expectedScale = expected.LegacyTransform.Right.Length();
                if (expectedScale > 0f)
                {
                    maxScaleDelta = Math.Max(maxScaleDelta, Math.Abs(actual.Right.Length() / expectedScale - 1f));
                }
            }

            matchedTotal += matched;
            report.AppendLine(CultureInfo.InvariantCulture,
                $"  {group.Key}: {group.Count()} / {portCount} / {matched} / {maxDistance:0.000} / {maxRotation:0.00} deg / {maxScaleDelta:0.000}");
        }

        report.AppendLine();
        string flagSummary;
        if (flagsKnown)
        {
            report.AppendLine(CultureInfo.InvariantCulture,
                $"shadow flags (castsShadow / receivesShadow, original's runtime state against the components the port created), {flaggedPairs} matched objects: {castMismatches} cast mismatch(es), {receiveMismatches} receive mismatch(es)");
            foreach (string line in flagLines.Take(WorstLines))
            {
                report.AppendLine(line);
            }

            flagSummary = $", shadow flags: {castMismatches} cast and {receiveMismatches} receive mismatch(es) of {flaggedPairs}";
        }
        else
        {
            report.AppendLine("shadow flags: not in this scene.json (written before the capture exported castsShadow and receivesShadow)");
            flagSummary = string.Empty;
        }

        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"original objects without a match in the port: {missing.Count}");
        foreach (string line in missing)
        {
            report.AppendLine(line);
        }

        var extra = new List<string>();
        for (int index = 0; index < port.PlacedObjects.Count; index++)
        {
            if (!used[index])
            {
                Vector3 p = port.PlacedObjects[index].LegacyTransform.Translation;
                extra.Add(string.Create(CultureInfo.InvariantCulture, $"  {port.PlacedObjects[index].ModelName} at ({p.X:0.0}, {p.Y:0.0}, {p.Z:0.0})"));
            }
        }

        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"port objects without a match in the original: {extra.Count}");
        foreach (string line in extra)
        {
            report.AppendLine(line);
        }

        return $"objects {reference.Count}/{port.PlacedObjects.Count}, {matchedTotal} matched, {missing.Count} missing in the port, {extra.Count} extra in the port{flagSummary}";
    }

    private static float AngleDegrees(Vector3 a, Vector3 b)
    {
        if (a.LengthSquared() < 1e-12f || b.LengthSquared() < 1e-12f)
        {
            return 0f;
        }

        float dot = MathHelper.Clamp(Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b)), -1f, 1f);
        return MathHelper.ToDegrees(MathF.Acos(dot));
    }

    private static Vector3 SafeNormalize(Vector3 vector)
    {
        return vector.LengthSquared() < 1e-12f ? Vector3.Zero : Vector3.Normalize(vector);
    }

    private static bool? ReadOptionalFlag(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
    }

    private static Vector3 ReadVector(JsonElement element)
    {
        return new Vector3(element[0].GetSingle(), element[1].GetSingle(), element[2].GetSingle());
    }
}
