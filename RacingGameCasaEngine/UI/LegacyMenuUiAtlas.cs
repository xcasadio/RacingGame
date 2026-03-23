using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace RacingGameCasaEngine.UI;

internal static class LegacyMenuUiAtlas
{
    public static readonly Rectangle MenuBackground = new(0, 0, 1024, 640);
    public static readonly Rectangle RacingGameLogo = new(0, 649, 1024, 374);
    public static readonly Rectangle MenuButtonPlay = new(0, 0, 212, 212);
    public static readonly Rectangle MenuButtonHighscores = new(212, 0, 212, 212);
    public static readonly Rectangle MenuButtonOptions = new(424, 0, 212, 212);
    public static readonly Rectangle MenuButtonHelp = new(636, 0, 212, 212);
    public static readonly Rectangle MenuButtonQuit = new(212, 240, 212, 212);
    public static readonly Rectangle TrackButtonBeginner = new(0, 480, 212, 352);
    public static readonly Rectangle TrackButtonAdvanced = new(212, 480, 212, 352);
    public static readonly Rectangle TrackButtonExpert = new(424, 480, 212, 352);
    public static readonly Rectangle BottomButtonA = new(0, 872, 212, 92);
    public static readonly Rectangle BottomButtonB = new(212, 872, 212, 92);
}