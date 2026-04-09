using System;
using System.Linq;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.UI;

internal sealed class CarSelectionPreviewRenderer
{
    private const int PreviewWidth = 360;
    private const int PreviewHeight = 220;
    private const float RotationSpeed = 0.48f;

    private static readonly Vector3 CameraPosition = new(-1.55f, 1.25f, -4.9f);
    private static readonly Vector3 CameraTarget = new(0f, 0.42f, 0.1f);

    private readonly RacingGameCasaEngineGame _game;
    private readonly BasicEffect _effect;
    private readonly RenderTarget2D _renderTarget;
    private readonly MGTextureData _textureData;
    private readonly VertexPositionNormalTexture[] _shadowPlaneVertices;
    private readonly short[] _shadowPlaneIndices = [0, 1, 2, 0, 2, 3];

    internal CarSelectionPreviewRenderer(RacingGameCasaEngineGame game)
    {
        _game = game;
        _renderTarget = new RenderTarget2D(game.GraphicsDevice, PreviewWidth, PreviewHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        _textureData = new MGTextureData(_renderTarget);
        _effect = new BasicEffect(game.GraphicsDevice)
        {
            LightingEnabled = true,
            PreferPerPixelLighting = true,
            VertexColorEnabled = false,
        };

        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.38f, -0.86f, -0.34f));
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 0.96f, 0.90f);
        _effect.DirectionalLight0.SpecularColor = new Vector3(0.88f, 0.84f, 0.80f);

        _effect.DirectionalLight1.Enabled = true;
        _effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0.58f, -0.24f, 0.78f));
        _effect.DirectionalLight1.DiffuseColor = new Vector3(0.18f, 0.24f, 0.32f);
        _effect.DirectionalLight1.SpecularColor = new Vector3(0.08f, 0.08f, 0.08f);

        _effect.DirectionalLight2.Enabled = false;

        _shadowPlaneVertices =
        [
            new VertexPositionNormalTexture(new Vector3(-1f, 0f, -1f), Vector3.Up, Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3(1f, 0f, -1f), Vector3.Up, Vector2.UnitX),
            new VertexPositionNormalTexture(new Vector3(1f, 0f, 1f), Vector3.Up, Vector2.One),
            new VertexPositionNormalTexture(new Vector3(-1f, 0f, 1f), Vector3.Up, Vector2.UnitY),
        ];
    }

    internal MGTextureData TextureData => _textureData;

    internal void Update(GameTime gameTime, int selectedCarIndex, int selectedCarColorIndex)
    {
        StaticModel? model = LegacyCarVisualFactory.LoadConfiguredCarModel(_game.AssetContentManager, selectedCarIndex, selectedCarColorIndex);
        if (model == null)
        {
            Clear();
            return;
        }

        BoundingBox bounds = LegacyCarVisualFactory.GetCarBounds(_game.AssetContentManager);
        float rotation = (float)gameTime.TotalGameTime.TotalSeconds * RotationSpeed;
        Render(model, bounds, rotation);
    }

    private void Clear()
    {
        GraphicsDevice graphicsDevice = _game.GraphicsDevice;
        RenderTargetBinding[] previousRenderTargets = graphicsDevice.GetRenderTargets();
        Viewport previousViewport = graphicsDevice.Viewport;

        graphicsDevice.SetRenderTarget(_renderTarget);
        graphicsDevice.Viewport = new Viewport(0, 0, PreviewWidth, PreviewHeight);
        graphicsDevice.Clear(Color.Transparent);

        if (previousRenderTargets.Length == 0)
        {
            graphicsDevice.SetRenderTarget(null);
        }
        else
        {
            graphicsDevice.SetRenderTargets(previousRenderTargets);
        }

        graphicsDevice.Viewport = previousViewport;
    }

    private void Render(StaticModel model, BoundingBox bounds, float rotation)
    {
        GraphicsDevice graphicsDevice = _game.GraphicsDevice;
        RenderTargetBinding[] previousRenderTargets = graphicsDevice.GetRenderTargets();
        Viewport previousViewport = graphicsDevice.Viewport;
        BlendState previousBlendState = graphicsDevice.BlendState;
        DepthStencilState previousDepthStencilState = graphicsDevice.DepthStencilState;
        RasterizerState previousRasterizerState = graphicsDevice.RasterizerState;
        SamplerState previousSamplerState = graphicsDevice.SamplerStates[0];

        try
        {
            graphicsDevice.SetRenderTarget(_renderTarget);
            graphicsDevice.Viewport = new Viewport(0, 0, PreviewWidth, PreviewHeight);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            Matrix view = Matrix.CreateLookAt(CameraPosition, CameraTarget, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(28f), PreviewWidth / (float)PreviewHeight, 0.1f, 100f);
            Matrix world = CreateCarWorldMatrix(bounds, rotation);

            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            DrawShadowPlane(view, projection);

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            DrawModel(model, model.RootNode, Matrix.Identity, world, view, projection, transparentPass: false);

            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            DrawModel(model, model.RootNode, Matrix.Identity, world, view, projection, transparentPass: true);
        }
        finally
        {
            if (previousRenderTargets.Length == 0)
            {
                graphicsDevice.SetRenderTarget(null);
            }
            else
            {
                graphicsDevice.SetRenderTargets(previousRenderTargets);
            }

            graphicsDevice.Viewport = previousViewport;
            graphicsDevice.BlendState = previousBlendState;
            graphicsDevice.DepthStencilState = previousDepthStencilState;
            graphicsDevice.RasterizerState = previousRasterizerState;
            graphicsDevice.SamplerStates[0] = previousSamplerState;
        }
    }

    private void DrawShadowPlane(Matrix view, Matrix projection)
    {
        _effect.TextureEnabled = false;
        _effect.Texture = null;
        _effect.LightingEnabled = false;
        _effect.World = Matrix.CreateScale(1.9f, 1f, 2.9f) * Matrix.CreateTranslation(0f, 0.02f, 0.12f);
        _effect.View = view;
        _effect.Projection = projection;
        _effect.DiffuseColor = new Vector3(0.04f, 0.05f, 0.07f);
        _effect.Alpha = 0.44f;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _game.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _shadowPlaneVertices, 0, _shadowPlaneVertices.Length, _shadowPlaneIndices, 0, 2);
        }

        _effect.LightingEnabled = true;
    }

    private void DrawModel(
        StaticModel model,
        StaticModelNode? node,
        Matrix parentTransform,
        Matrix modelWorld,
        Matrix view,
        Matrix projection,
        bool transparentPass)
    {
        if (node == null)
        {
            return;
        }

        Matrix absoluteTransform = node.LocalTransform * parentTransform;
        if (node.MeshIndex >= 0 && node.MeshIndex < model.Meshes.Count)
        {
            DrawMesh(model.Meshes[node.MeshIndex], absoluteTransform * modelWorld, view, projection, transparentPass);
        }

        for (int index = 0; index < node.Children.Count; index++)
        {
            DrawModel(model, node.Children[index], absoluteTransform, modelWorld, view, projection, transparentPass);
        }
    }

    private void DrawMesh(StaticModelMesh mesh, Matrix world, Matrix view, Matrix projection, bool transparentPass)
    {
        VertexPositionNormalTexture[] vertices = mesh.GetVertices() as VertexPositionNormalTexture[] ?? mesh.GetVertices().ToArray();
        int[] indices = mesh.GetIndices().Select(index => unchecked((int)index)).ToArray();
        if (vertices.Length == 0 || indices.Length == 0)
        {
            return;
        }

        if (mesh.SubMeshes.Count == 0)
        {
            DrawMaterialRange(mesh.Material, vertices, indices, 0, indices.Length / 3, 0, world, view, projection, transparentPass);
            return;
        }

        for (int index = 0; index < mesh.SubMeshes.Count; index++)
        {
            SubMesh subMesh = mesh.SubMeshes[index];
            DrawMaterialRange(subMesh.Material ?? mesh.Material, vertices, indices, subMesh.IndexStart, subMesh.PrimitiveCount, subMesh.VertexOffset, world, view, projection, transparentPass);
        }
    }

    private void DrawMaterialRange(
        MaterialBase? material,
        VertexPositionNormalTexture[] vertices,
        int[] indices,
        int indexStart,
        int primitiveCount,
        int vertexOffset,
        Matrix world,
        Matrix view,
        Matrix projection,
        bool transparentPass)
    {
        if (primitiveCount <= 0 || !ShouldDrawMaterial(material, transparentPass))
        {
            return;
        }

        ConfigureEffect(material, world, view, projection);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _game.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, vertexOffset, vertices.Length - vertexOffset, indices, indexStart, primitiveCount);
        }
    }

    private void ConfigureEffect(MaterialBase? material, Matrix world, Matrix view, Matrix projection)
    {
        _effect.World = world;
        _effect.View = view;
        _effect.Projection = projection;
        _effect.TextureEnabled = false;
        _effect.Texture = null;
        _effect.Alpha = 1f;
        _effect.DiffuseColor = Vector3.One;
        _effect.AmbientLightColor = new Vector3(0.18f, 0.19f, 0.22f);
        _effect.EmissiveColor = Vector3.Zero;
        _effect.SpecularColor = new Vector3(0.28f);
        _effect.SpecularPower = 16f;

        if (material is not LitDiffuseMaterial litMaterial)
        {
            return;
        }

        _effect.TextureEnabled = litMaterial.BasColor != null;
        _effect.Texture = litMaterial.BasColor;
        _effect.Alpha = litMaterial.DiffuseColor.A / 255f;
        Vector3 previewDiffuseColor = litMaterial.DiffuseColor.ToVector3();
        if (litMaterial.TintStrength > 0.001f)
        {
            previewDiffuseColor = Vector3.Lerp(previewDiffuseColor, litMaterial.TintColor, 0.45f * litMaterial.TintStrength);
        }

        _effect.DiffuseColor = previewDiffuseColor;
        _effect.AmbientLightColor = litMaterial.AmbientColor == Vector3.Zero
            ? new Vector3(0.18f, 0.19f, 0.22f)
            : Vector3.Clamp(litMaterial.AmbientColor + new Vector3(0.08f), Vector3.Zero, Vector3.One);
        _effect.EmissiveColor = litMaterial.EmissiveColor;
        _effect.SpecularColor = litMaterial.SpecularColor;
        _effect.SpecularPower = litMaterial.SpecularPower;
    }

    private static bool ShouldDrawMaterial(MaterialBase? material, bool transparentPass)
    {
        bool isTransparent = material != null
            && material.Queue != RenderQueue.AlphaTest
            && (material.IsTransparent
                || material.Queue >= RenderQueue.Transparent
                || ReferenceEquals(material.BlendState, BlendState.AlphaBlend)
                || ReferenceEquals(material.BlendState, BlendState.NonPremultiplied)
                || ReferenceEquals(material.BlendState, BlendState.Additive));

        return transparentPass ? isTransparent : !isTransparent;
    }

    private static Matrix CreateCarWorldMatrix(BoundingBox bounds, float rotation)
    {
        Vector3 boundsCenter = (bounds.Min + bounds.Max) * 0.5f;
        float scale = LegacyCarVisualFactory.ComputeUniformScale(bounds);
        float liftY = (bounds.Max.Y - bounds.Min.Y) * 0.5f * scale;

        return Matrix.CreateTranslation(-boundsCenter)
            * Matrix.CreateScale(scale)
            * Matrix.CreateFromQuaternion(LegacyCarVisualFactory.LegacyCarFacingCorrection)
            * Matrix.CreateRotationY(rotation)
            * Matrix.CreateTranslation(0f, liftY, 0f);
    }
}