using System.Runtime.InteropServices;
using CasaEngine.Core.Log;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Turns on seamless cube map filtering (GL_TEXTURE_CUBE_MAP_SEAMLESS, core since OpenGL 3.2) in the OpenGL context of
/// MonoGame DesktopGL, which never enables it. Without it each face is filtered as a separate 2D texture and its edge texels
/// follow the wrap mode of the texture: with Wrap they blend with the opposite edge of the same face, which drew a line of
/// ground-coloured pixels along the edges between the top face and the side faces of the race sky.
/// </summary>
/// <remarks>
/// Setting a Clamp sampler state does not fix it: under OpenGL, MonoGame writes the sampler state into the texture object, one
/// slot after the other, and the sky cube is also the cars' reflection cube, left bound to another slot with a Wrap state that
/// overwrites the Clamp of the sky's slot. Seamless filtering ignores the wrap mode at the face edges and blends across the
/// faces, as Direct3D 10 and later always do. It is a context state: one call, on the thread that owns the context.
/// </remarks>
internal static class OpenGlSeamlessCubeMaps
{
    private const uint TextureCubeMapSeamless = 0x884F;

    public static void Enable()
    {
        try
        {
            GlEnable(TextureCubeMapSeamless);
            if (!GlIsEnabled(TextureCubeMapSeamless))
            {
                Logs.WriteWarning("Seamless cube map filtering is not supported by the OpenGL context: the sky may show lines along the edges of its faces.");
            }
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            Logs.WriteWarning($"Seamless cube map filtering cannot be enabled: {exception.Message}");
        }
    }

    [DllImport("opengl32.dll", EntryPoint = "glEnable")]
    private static extern void GlEnable(uint capability);

    [DllImport("opengl32.dll", EntryPoint = "glIsEnabled")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool GlIsEnabled(uint capability);
}
