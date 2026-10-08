using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// Reads the legacy .Track and .CombiModel files without XmlSerializer. XmlSerializer generates a dynamic assembly
/// that references the type it serializes, which a gameplay assembly loaded by the editor (a collectible load context)
/// cannot allow. The file formats are those XmlSerializer wrote and read before: same element names, same members, the
/// members of a structure set in the order of the file, the numbers parsed as XmlConvert parses them.
/// <para>
/// The width helpers and the CombiModel items are read under the element names of the files (WidthHelper, CombiObject),
/// as the original game reads them. The road helpers are still looked up by the name of their class (LegacyRoadHelper),
/// as XmlSerializer did; the shipped files name them RoadHelper, so they are not read yet.
/// </para>
/// </summary>
internal static class LegacyTrackFileReader
{
    internal static LegacyTrackLayout ReadTrack(Stream stream)
    {
        XElement root = XDocument.Load(stream).Root ?? throw new InvalidDataException("The track file has no root element.");
        if (root.Name.LocalName != "TrackData")
        {
            throw new InvalidDataException($"The track file's root element is '{root.Name.LocalName}', not 'TrackData'.");
        }

        var layout = new LegacyTrackLayout();
        foreach (XElement element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "TrackPoints":
                    foreach (XElement item in element.Elements("Vector3"))
                    {
                        layout.TrackPoints.Add(ReadVector3(item));
                    }

                    break;
                case "WidthHelpers":
                    foreach (XElement item in element.Elements("WidthHelper"))
                    {
                        layout.WidthHelpers.Add(ReadWidthHelper(item));
                    }

                    break;
                case "RoadHelpers":
                    foreach (XElement item in element.Elements(nameof(LegacyRoadHelper)))
                    {
                        layout.RoadHelpers.Add(ReadRoadHelper(item));
                    }

                    break;
                case "NeutralsObjects":
                    foreach (XElement item in element.Elements("NeutralObject"))
                    {
                        layout.NeutralsObjects.Add(ReadNeutralObject(item));
                    }

                    break;
            }
        }

        return layout;
    }

    internal static List<LegacyCombiObject> ReadCombiObjects(Stream stream)
    {
        XElement root = XDocument.Load(stream).Root ?? throw new InvalidDataException("The CombiModel file has no root element.");
        if (root.Name.LocalName != "ArrayOfCombiObject")
        {
            throw new InvalidDataException($"The CombiModel file's root element is '{root.Name.LocalName}', not 'ArrayOfCombiObject'.");
        }

        var objects = new List<LegacyCombiObject>();
        foreach (XElement item in root.Elements("CombiObject"))
        {
            var combiObject = new LegacyCombiObject();
            foreach (XElement member in item.Elements())
            {
                switch (member.Name.LocalName)
                {
                    case "modelName":
                        combiObject.modelName = member.Value;
                        break;
                    case "matrix":
                        combiObject.matrix = ReadMatrix(member);
                        break;
                }
            }

            objects.Add(combiObject);
        }

        return objects;
    }

    private static LegacyWidthHelper ReadWidthHelper(XElement element)
    {
        var helper = new LegacyWidthHelper();
        foreach (XElement member in element.Elements())
        {
            switch (member.Name.LocalName)
            {
                case "pos":
                    helper.pos = ReadVector3(member);
                    break;
                case "scale":
                    helper.scale = ReadSingle(member);
                    break;
            }
        }

        return helper;
    }

    private static LegacyRoadHelper ReadRoadHelper(XElement element)
    {
        var helper = new LegacyRoadHelper();
        foreach (XElement member in element.Elements())
        {
            switch (member.Name.LocalName)
            {
                case "type":
                    helper.type = member.Value;
                    break;
                case "pos":
                    helper.pos = ReadVector3(member);
                    break;
            }
        }

        return helper;
    }

    private static LegacyNeutralObject ReadNeutralObject(XElement element)
    {
        var neutralObject = new LegacyNeutralObject();
        foreach (XElement member in element.Elements())
        {
            switch (member.Name.LocalName)
            {
                case "modelName":
                    neutralObject.modelName = member.Value;
                    break;
                case "matrix":
                    neutralObject.matrix = ReadMatrix(member);
                    break;
            }
        }

        return neutralObject;
    }

    private static Vector3 ReadVector3(XElement element)
    {
        var vector = default(Vector3);
        foreach (XElement member in element.Elements())
        {
            switch (member.Name.LocalName)
            {
                case "X":
                    vector.X = ReadSingle(member);
                    break;
                case "Y":
                    vector.Y = ReadSingle(member);
                    break;
                case "Z":
                    vector.Z = ReadSingle(member);
                    break;
            }
        }

        return vector;
    }

    // The matrix is a structure of fields (M11..M44) and of properties (Up, Down...) that write the same fields:
    // the members are set one after the other, in the order of the file, so the last writer of a field wins.
    private static Matrix ReadMatrix(XElement element)
    {
        var matrix = default(Matrix);
        foreach (XElement member in element.Elements())
        {
            switch (member.Name.LocalName)
            {
                case "M11": matrix.M11 = ReadSingle(member); break;
                case "M12": matrix.M12 = ReadSingle(member); break;
                case "M13": matrix.M13 = ReadSingle(member); break;
                case "M14": matrix.M14 = ReadSingle(member); break;
                case "M21": matrix.M21 = ReadSingle(member); break;
                case "M22": matrix.M22 = ReadSingle(member); break;
                case "M23": matrix.M23 = ReadSingle(member); break;
                case "M24": matrix.M24 = ReadSingle(member); break;
                case "M31": matrix.M31 = ReadSingle(member); break;
                case "M32": matrix.M32 = ReadSingle(member); break;
                case "M33": matrix.M33 = ReadSingle(member); break;
                case "M34": matrix.M34 = ReadSingle(member); break;
                case "M41": matrix.M41 = ReadSingle(member); break;
                case "M42": matrix.M42 = ReadSingle(member); break;
                case "M43": matrix.M43 = ReadSingle(member); break;
                case "M44": matrix.M44 = ReadSingle(member); break;
                case "Up": matrix.Up = ReadVector3(member); break;
                case "Down": matrix.Down = ReadVector3(member); break;
                case "Right": matrix.Right = ReadVector3(member); break;
                case "Left": matrix.Left = ReadVector3(member); break;
                case "Forward": matrix.Forward = ReadVector3(member); break;
                case "Backward": matrix.Backward = ReadVector3(member); break;
                case "Translation": matrix.Translation = ReadVector3(member); break;
            }
        }

        return matrix;
    }

    private static float ReadSingle(XElement element)
    {
        return XmlConvert.ToSingle(element.Value.Trim());
    }
}
