using System.Runtime.CompilerServices;

namespace RacingGame.Shaders;

/// <summary>
/// Restores the default values declared in the legacy .fx files.
/// MonoGame does not apply effect parameter default values on GL platforms
/// (docs.monogame.net, "Custom Effects", "Effect Writing Tips"), so a DesktopGL
/// effect starts with zeroed parameters where DirectX started from the declared
/// values. The values below are the global, non-static initializers of
/// RacingGame/Content/Shaders/*.fx; keep them in sync with those files.
/// </summary>
public static class LegacyEffectDefaults
{
    // Content-loaded effects are shared instances: the defaults are the initial state of an
    // instance, exactly like on DirectX, so they are applied once and never reset later changes.
    private static readonly ConditionalWeakTable<Effect, object> AppliedEffects = new();

    /// <summary>
    /// Apply the declared defaults of <paramref name="shaderName"/> (file name without
    /// extension) to <paramref name="effect"/>, once per effect instance.
    /// </summary>
    public static void ApplyOnce(Effect effect, string shaderName)
    {
        if (effect == null || !MarkApplied(effect))
        {
            return;
        }

        switch (shaderName)
        {
            case "LandscapeNormalMapping":
                ApplyLandscapeNormalMapping(effect);
                break;
            case "LightingShader":
                ApplyLightingShader(effect);
                break;
            case "NormalMapping":
                ApplyNormalMapping(effect);
                break;
            case "PostScreenGlow":
                ApplyPostScreenGlow(effect);
                break;
            case "PostScreenMenu":
                ApplyPostScreenMenu(effect);
                break;
            case "PostScreenShadowBlur":
                ApplyPostScreenShadowBlur(effect);
                break;
            case "PreScreenSkyCubeMapping":
                ApplyPreScreenSkyCubeMapping(effect);
                break;
            case "ReflectionSimpleGlass":
                ApplyReflectionSimpleGlass(effect);
                break;
            case "ShadowMap":
                ApplyShadowMap(effect);
                break;
        }
    }

    /// <summary>
    /// Apply the declared defaults to an effect of a model built by the content pipeline, once
    /// per effect instance. Model materials only use NormalMapping.fx or ReflectionSimpleGlass.fx;
    /// the source shader is recognized by its techniques. Other effects (BasicEffect for
    /// materials without an effect instance) are left untouched.
    /// </summary>
    public static void ApplyOnceToModelEffect(Effect effect)
    {
        if (effect == null)
        {
            return;
        }

        if (effect.Techniques["Diffuse"] != null)
        {
            ApplyOnce(effect, "NormalMapping");
        }
        else if (effect.Techniques["ReflectionSpecular20"] != null)
        {
            ApplyOnce(effect, "ReflectionSimpleGlass");
        }
    }

    private static bool MarkApplied(Effect effect)
    {
        if (AppliedEffects.TryGetValue(effect, out _))
        {
            return false;
        }

        AppliedEffects.Add(effect, null);
        return true;
    }

    private static void ApplyLandscapeNormalMapping(Effect effect)
    {
        Set(effect, "DetailFactor", 24f);
    }

    private static void ApplyLightingShader(Effect effect)
    {
        Set(effect, "shadowCarColor", new Vector4(1.0f, 1.0f, 1.0f, 0.125f));
    }

    private static void ApplyNormalMapping(Effect effect)
    {
        Set(effect, "lightDir", new Vector3(-0.65f, 0.65f, -0.39f));
        Set(effect, "ambientColor", new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        Set(effect, "diffuseColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        Set(effect, "specularColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        Set(effect, "shininess", 16.0f);
        Set(effect, "alphaFactor", 0.66f);
        Set(effect, "fresnelBias", 0.5f);
        Set(effect, "fresnelPower", 1.5f);
        Set(effect, "reflectionAmount", 1.0f);
        Set(effect, "UseAlpha", true);
        Set(effect, "carHueColor", new Vector3(0.0f, 0.0f, 0.0f));
    }

    private static void ApplyPostScreenGlow(Effect effect)
    {
        Set(effect, "DownsampleMultiplicator", 0.25f);
        Set(effect, "ClearColor", new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        Set(effect, "ClearDepth", 1.0f);
        Set(effect, "GlowIntensity", 0.7f);
        Set(effect, "HighlightThreshold", 0.975f);
        Set(effect, "HighlightIntensity", 0.4f);
        Set(effect, "radialBlurScaleFactor", -0.004f);
        Set(effect, "downsampleScale", 0.25f);
        Set(effect, "weights7", new[] { 0.05f, 0.1f, 0.2f, 0.3f, 0.2f, 0.1f, 0.05f });
        Set(effect, "BlurWidth", 8.0f);
    }

    private static void ApplyPostScreenMenu(Effect effect)
    {
        Set(effect, "DownsampleMultiplicator", 0.25f);
        Set(effect, "ClearColor", new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        Set(effect, "ClearDepth", 1.0f);
        Set(effect, "GlowIntensity", 0.25f);
        Set(effect, "HighlightThreshold", 0.925f);
        Set(effect, "HighlightIntensity", 0.145f);
        Set(effect, "BlurWidth", 4.0f);
        Set(effect, "downsampleScale", 0.25f);
        Set(effect, "Speed", 0.0032f);
        Set(effect, "Speed2", 0.0016f);
        Set(effect, "ScratchIntensity", 0.605f);
        Set(effect, "IS", 0.031f);
        Set(effect, "weights7", new[] { 0.05f, 0.1f, 0.2f, 0.3f, 0.2f, 0.1f, 0.05f });
    }

    private static void ApplyPostScreenShadowBlur(Effect effect)
    {
        Set(effect, "ClearColor", new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        Set(effect, "ClearDepth", 1.0f);
        Set(effect, "BlurWidth", 1.25f);
        Set(effect, "BlurWidth20", 1.5f);
    }

    private static void ApplyPreScreenSkyCubeMapping(Effect effect)
    {
        Set(effect, "ambientColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
    }

    private static void ApplyReflectionSimpleGlass(Effect effect)
    {
        Set(effect, "lightDir", new Vector3(1.0f, -1.0f, 1.0f));
        Set(effect, "ambientColor", new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
        Set(effect, "diffuseColor", new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
        Set(effect, "specularColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        Set(effect, "shininess", 24.0f);
        Set(effect, "alphaFactor", 0.66f);
        Set(effect, "fresnelBias", 0.5f);
        Set(effect, "fresnelPower", 1.5f);
        Set(effect, "reflectionAmount", 1.0f);
    }

    private static void ApplyShadowMap(Effect effect)
    {
        Set(effect, "nearPlane", 2.0f);
        Set(effect, "farPlane", 8.0f);
        Set(effect, "depthBias", 0.0025f);
        Set(effect, "shadowMapDepthBias", -0.0005f);
        Set(effect, "ShadowColor", new Vector4(0.25f, 0.26f, 0.27f, 1.0f));
        Set(effect, "lightDir", new Vector3(1.0f, -1.0f, 1.0f));
        Set(effect, "shadowMapTexelSize", new Vector2(1.0f / 1024.0f, 1.0f / 1024f));
        Set(effect, "FilterTaps", new[]
        {
            new Vector2(-0.84052f, -0.073954f),
            new Vector2(-0.326235f, -0.40583f),
            new Vector2(-0.698464f, 0.457259f),
            new Vector2(-0.203356f, 0.6205847f),
            new Vector2(0.96345f, -0.194353f),
            new Vector2(0.473434f, -0.480026f),
            new Vector2(0.519454f, 0.767034f),
            new Vector2(0.185461f, -0.8945231f),
            new Vector2(0.507351f, 0.064963f),
            new Vector2(-0.321932f, 0.5954349f),
        });
    }

    // Parameters removed by the effect compiler (unused in every technique) are simply skipped.
    private static void Set(Effect effect, string name, float value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, bool value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, Vector2 value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, Vector3 value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, Vector4 value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, float[] value) => effect.Parameters[name]?.SetValue(value);

    private static void Set(Effect effect, string name, Vector2[] value) => effect.Parameters[name]?.SetValue(value);
}
