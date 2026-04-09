using CasaEngine.Framework.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class LegacySkyCubeViewPipeline : IViewRenderPipeline
{
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
        _effect.Parameters["SkyCube"]?.SetValue(_skyCube);

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