using CasaEngine.Framework.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.UI;

/// <summary>
/// Renders a view with the default pipeline, then sets the alpha of its render target to 1 wherever geometry was drawn,
/// so that an MGUI image can lay the view over the screen (ADR-0008, car selection carousel). CasaEngine's lit shaders
/// write the texture alpha, which RacingGame's car texture uses as its paint mask, so the car came out see-through over
/// a transparent clear. The pass draws one screen quad just in front of the far plane, writing alpha only, where the depth
/// buffer holds something nearer.
/// </summary>
internal sealed class OpaqueGeometryAlphaViewPipeline : IViewRenderPipeline, IDisposable
{
    // Just in front of the cleared depth of 1: passes over any drawn geometry, fails over the cleared background.
    private const float QuadDepth = 0.99999f;

    private static readonly BlendState AlphaOnly = new()
    {
        Name = "OpaqueGeometryAlpha",
        ColorWriteChannels = ColorWriteChannels.Alpha,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.Zero,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.Zero,
    };

    private static readonly DepthStencilState InFrontOfQuad = new()
    {
        Name = "OpaqueGeometryAlphaDepth",
        DepthBufferEnable = true,
        DepthBufferWriteEnable = false,
        DepthBufferFunction = CompareFunction.Greater,
    };

    private static readonly VertexPositionColor[] Quad =
    [
        new(new Vector3(-1f, -1f, QuadDepth), Color.White),
        new(new Vector3(-1f, 1f, QuadDepth), Color.White),
        new(new Vector3(1f, -1f, QuadDepth), Color.White),
        new(new Vector3(1f, 1f, QuadDepth), Color.White),
    ];

    private readonly IViewRenderPipeline _inner;
    private BasicEffect? _effect;

    public OpaqueGeometryAlphaViewPipeline(IViewRenderPipeline? inner = null)
    {
        _inner = inner ?? DefaultViewPipeline.Instance;
    }

    public void RenderView(GraphicsDevice graphicsDevice, RenderView view, in RenderFrame frame, IReadOnlyList<IViewFlushableRenderer> renderers)
    {
        _inner.RenderView(graphicsDevice, view, in frame, renderers);

        _effect ??= new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            World = Matrix.Identity,
            View = Matrix.Identity,
            Projection = Matrix.Identity,
        };

        BlendState blendState = graphicsDevice.BlendState;
        DepthStencilState depthStencilState = graphicsDevice.DepthStencilState;
        RasterizerState rasterizerState = graphicsDevice.RasterizerState;

        graphicsDevice.BlendState = AlphaOnly;
        graphicsDevice.DepthStencilState = InFrontOfQuad;
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Quad, 0, 2);
        }

        graphicsDevice.BlendState = blendState;
        graphicsDevice.DepthStencilState = depthStencilState;
        graphicsDevice.RasterizerState = rasterizerState;
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
    }
}
