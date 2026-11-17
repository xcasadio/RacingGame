using CasaEngine.Framework.Rendering;
using CasaEngine.Framework.Rendering.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Draws the legacy sky cube, then the wrapped view pipeline. It is a scene-phase pipeline (<see cref="IScenePhaseViewPipeline"/>,
/// like the engine's SkyBackgroundViewPipeline): in the linear HDR colour pipeline the sky is part of the scene, tonemapped and
/// anti-aliased with it, and the interface of the wrapped pipeline is composed afterwards. The sky shader decodes its display
/// colours when <c>ColorPipelineIsLinear</c> is bound from the frame.
/// </summary>
internal sealed class LegacySkyCubeViewPipeline : IViewRenderPipeline, IScenePhaseViewPipeline
{
    // Initial value of SkyCubeScale in LegacySkyCube.fx. MonoGame does not apply HLSL parameter initializers under
    // OpenGL (DesktopGL): the scale stayed 0 and the sky cube collapsed to a point.
    private const float SkyCubeScale = 100.0f;

    private static readonly RasterizerState SkyRasterizerState = new()
    {
        CullMode = CullMode.None,
        FillMode = FillMode.Solid,
        MultiSampleAntiAlias = true,
    };

    private readonly IViewRenderPipeline _inner;
    private readonly Effect _effect;
    private readonly TextureCube _skyCube;
    private readonly Color _skyTintColor;

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private GraphicsDevice? _graphicsDevice;

    public LegacySkyCubeViewPipeline(Effect effect, TextureCube skyCube, Color skyTintColor, IViewRenderPipeline? inner = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _skyCube = skyCube ?? throw new ArgumentNullException(nameof(skyCube));
        _skyTintColor = skyTintColor;
        _inner = inner ?? DefaultViewPipeline.Instance;

        EffectTechnique? technique = _effect.Techniques["SkyCube"];
        if (technique != null)
        {
            _effect.CurrentTechnique = technique;
        }
    }

    public void RenderView(
        GraphicsDevice graphicsDevice,
        RenderView view,
        in RenderFrame frame,
        IReadOnlyList<IViewFlushableRenderer> renderers)
    {
        EnsureGeometry(graphicsDevice);
        DrawSkyCube(graphicsDevice, in frame);
        _inner.RenderView(graphicsDevice, view, in frame, renderers);
    }

    /// <summary>True when the wrapped pipeline can render its scene alone; otherwise this pipeline cannot split its scene either.</summary>
    public bool SupportsScenePhase => _inner is IScenePhaseViewPipeline { SupportsScenePhase: true };

    /// <summary>The sky, then the scene of the wrapped pipeline without its interface.</summary>
    public void RenderScene(
        GraphicsDevice graphicsDevice,
        RenderView view,
        in RenderFrame frame,
        IReadOnlyList<IViewFlushableRenderer> renderers)
    {
        if (_inner is not IScenePhaseViewPipeline { SupportsScenePhase: true } innerScene)
        {
            throw new InvalidOperationException("The wrapped view pipeline cannot render its scene without its interface.");
        }

        EnsureGeometry(graphicsDevice);
        DrawSkyCube(graphicsDevice, in frame);
        innerScene.RenderScene(graphicsDevice, view, in frame, renderers);
    }

    private void EnsureGeometry(GraphicsDevice graphicsDevice)
    {
        if (ReferenceEquals(_graphicsDevice, graphicsDevice)
            && _vertexBuffer != null
            && _indexBuffer != null)
        {
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _graphicsDevice = graphicsDevice;
        _vertexBuffer = new VertexBuffer(graphicsDevice, VertexPosition.VertexDeclaration, SkyVertices.Length, BufferUsage.None);
        _vertexBuffer.SetData(SkyVertices);

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, SkyIndices.Length, BufferUsage.None);
        _indexBuffer.SetData(SkyIndices);
    }

    private void DrawSkyCube(GraphicsDevice graphicsDevice, in RenderFrame frame)
    {
        if (_vertexBuffer == null || _indexBuffer == null)
        {
            return;
        }

        Matrix viewWithoutTranslation = frame.View;
        viewWithoutTranslation.Translation = Vector3.Zero;
        Matrix viewProjection = viewWithoutTranslation * frame.Projection;

        graphicsDevice.BlendState = BlendState.Opaque;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = SkyRasterizerState;
        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        _effect.Parameters["ViewProjection"]?.SetValue(viewProjection);
        _effect.Parameters["SkyTintColor"]?.SetValue(_skyTintColor.ToVector4());
        _effect.Parameters["SkyCubeScale"]?.SetValue(SkyCubeScale);
        _effect.Parameters["SkyCube"]?.SetValue(_skyCube);
        _effect.Parameters[ShaderParameterNames.ColorPipelineIsLinear]?.SetValue(frame.Environment.LinearColorActive ? 1.0f : 0.0f);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                baseVertex: 0,
                startIndex: 0,
                primitiveCount: SkyIndices.Length / 3);
        }

        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
    }

    private static readonly VertexPosition[] SkyVertices =
    [
        new(new Vector3(-1f, -1f,  1f)),
        new(new Vector3( 1f, -1f,  1f)),
        new(new Vector3( 1f,  1f,  1f)),
        new(new Vector3(-1f,  1f,  1f)),
        new(new Vector3(-1f, -1f, -1f)),
        new(new Vector3(-1f,  1f, -1f)),
        new(new Vector3( 1f,  1f, -1f)),
        new(new Vector3( 1f, -1f, -1f)),
        new(new Vector3(-1f,  1f, -1f)),
        new(new Vector3(-1f,  1f,  1f)),
        new(new Vector3( 1f,  1f,  1f)),
        new(new Vector3( 1f,  1f, -1f)),
        new(new Vector3(-1f, -1f, -1f)),
        new(new Vector3( 1f, -1f, -1f)),
        new(new Vector3( 1f, -1f,  1f)),
        new(new Vector3(-1f, -1f,  1f)),
        new(new Vector3( 1f, -1f, -1f)),
        new(new Vector3( 1f,  1f, -1f)),
        new(new Vector3( 1f,  1f,  1f)),
        new(new Vector3( 1f, -1f,  1f)),
        new(new Vector3(-1f, -1f, -1f)),
        new(new Vector3(-1f, -1f,  1f)),
        new(new Vector3(-1f,  1f,  1f)),
        new(new Vector3(-1f,  1f, -1f)),
    ];

    private static readonly ushort[] SkyIndices =
    [
        0, 1, 2,
        0, 2, 3,
        4, 5, 6,
        4, 6, 7,
        8, 9, 10,
        8, 10, 11,
        12, 13, 14,
        12, 14, 15,
        16, 17, 18,
        16, 18, 19,
        20, 21, 22,
        20, 22, 23,
    ];
}