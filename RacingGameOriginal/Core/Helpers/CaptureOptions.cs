using System;
using System.Globalization;
using System.IO;

namespace RacingGame.Helpers
{
    /// <summary>
    /// Options of the track audit capture mode (--capture-track-audit): the game skips its menus,
    /// renders fixed views of every built-in track, saves them as PNG files next to a views.json
    /// describing the cameras, then quits. Used as the visual reference of the CasaEngine port.
    /// Without the argument nothing here is enabled and the game behaves as usual.
    /// </summary>
    public static class CaptureOptions
    {
        public const string CaptureArgument = "--capture-track-audit";

        /// <summary>True when the game runs the track audit capture instead of its menus.</summary>
        public static bool Enabled { get; private set; }

        /// <summary>Seed of the global random generator, so that the random scenery is the same on every run.</summary>
        public static int Seed { get; private set; } = 1;

        /// <summary>Directory receiving the PNG files and views.json.</summary>
        public static string OutputDirectory { get; private set; } = "";

        /// <summary>Requested back buffer size (windowed). It must fit on the desktop.</summary>
        public static int Width { get; private set; } = 1280;
        public static int Height { get; private set; } = 720;

        /// <summary>
        /// Reads the command line. Unknown arguments are ignored; a malformed capture argument throws.
        /// </summary>
        public static void Parse(string[] args)
        {
            if (args == null)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], CaptureArgument, StringComparison.OrdinalIgnoreCase))
                    Enabled = true;
            }

            if (!Enabled)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--seed", StringComparison.OrdinalIgnoreCase))
                {
                    Seed = int.Parse(RequireValue(args, ref i, arg), CultureInfo.InvariantCulture);
                }
                else if (string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase))
                {
                    OutputDirectory = Path.GetFullPath(RequireValue(args, ref i, arg));
                }
                else if (string.Equals(arg, "--size", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = RequireValue(args, ref i, arg).Split('x', 'X');
                    if (parts.Length != 2)
                        throw new ArgumentException("--size expects WIDTHxHEIGHT, for example 1280x720.");
                    Width = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    Height = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    if (Width <= 0 || Height <= 0)
                        throw new ArgumentException("--size expects positive values.");
                }
            }

            if (OutputDirectory.Length == 0)
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TrackAuditCaptures");
        }

        private static string RequireValue(string[] args, ref int index, string name)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException(name + " expects a value.");
            index++;
            return args[index];
        }
    }
}
