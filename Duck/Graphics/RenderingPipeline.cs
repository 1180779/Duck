using System.Numerics;

using Duck.Entities;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Duck.Graphics;

public struct WaterCommand
{
    public ID3D11ShaderResourceView? EnvCubeTexture;
    public Matrix4x4 InvTransform;
    public Mesh Mesh;
    public ID3D11ShaderResourceView? NormTexture;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public Matrix4x4 Transform;
}

public struct EnvCommand
{
    public ID3D11ShaderResourceView CubeTexture;
    public Mesh Mesh;
    public Vector4 SurfaceColor;
    public Matrix4x4 Transform;
}

public struct OpaqueCommand
{
    public Matrix4x4 InvTransform;
    public Mesh Mesh;
    public ID3D11ShaderResourceView? NormTexture;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public Matrix4x4 Transform;
}

public sealed class RenderingPipeline : IDisposable
{
    private readonly ID3D11BlendState? _additiveBlendState;
    private readonly ID3D11BlendState? _alphaBlendState;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor>? _colorBuffer;
    private readonly ID3D11RasterizerState? _cullBackState;
    private readonly ID3D11DepthStencilState? _defaultDepthState;
    private readonly List<EnvCommand> _envs = [];
    private readonly ConstantBuffer<ConstantBufferModel>? _modelBuffer;
    private readonly ID3D11DepthStencilState? _noDepthState;
    private readonly ID3D11DepthStencilState? _noDepthWriteState;
    private readonly List<OpaqueCommand> _opaques = [];
    private readonly List<WaterCommand> _waters = [];

    public RenderingPipeline()
    {
        ID3D11Device device = GI.Instance.Device;

        DepthStencilDescription depthDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _defaultDepthState = device.CreateDepthStencilState(depthDesc);

        DepthStencilDescription noDepthDesc = new()
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = false
        };
        _noDepthState = device.CreateDepthStencilState(noDepthDesc);

        DepthStencilDescription noDepthWriteDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _noDepthWriteState = device.CreateDepthStencilState(noDepthWriteDesc);

        RasterizerDescription cullBackDesc = new()
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        _cullBackState = device.CreateRasterizerState(cullBackDesc);

        RasterizerDescription cullFrontDesc = new()
        {
            CullMode = CullMode.Front,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        device.CreateRasterizerState(cullFrontDesc);

        BlendDescription additiveBlendDesc = new();
        additiveBlendDesc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.One,
            DestinationBlend = Blend.One,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.Zero,
            DestinationBlendAlpha = Blend.One,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _additiveBlendState = device.CreateBlendState(additiveBlendDesc);

        BlendDescription alphaBlendDesc = new();
        alphaBlendDesc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = true,
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _alphaBlendState = device.CreateBlendState(alphaBlendDesc);

        _modelBuffer = new ConstantBuffer<ConstantBufferModel>();
        _colorBuffer = new ConstantBuffer<ConstantBufferSurfaceColor>();
    }

    public ID3D11ShaderResourceView? EnvCubeTexture
    {
        get;
        set;
    }

    public void Dispose()
    {
        _modelBuffer?.Dispose();
        _colorBuffer?.Dispose();
        _defaultDepthState?.Dispose();
        _noDepthState?.Dispose();
        _noDepthWriteState?.Dispose();
        _cullBackState?.Dispose();
        _additiveBlendState?.Dispose();
        _alphaBlendState?.Dispose();
    }

    public void Execute(Camera mainCamera)
    {
        ID3D11DeviceContext context = GI.Instance.Context;

        context.OMSetRenderTargets(GI.Instance.RenderTargetView);
        context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f));
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);

        _modelBuffer?.Bind();
        _colorBuffer?.Bind(2);

        RenderGPass(context, mainCamera);
        RenderLightPass(context, mainCamera);
        RenderEnvs(context, mainCamera);
        RenderWaters(context);

        ClearQueues();
    }

    public void SubmitEnv(
        Mesh mesh, Matrix4x4 transform, Vector4 color, ID3D11ShaderResourceView cubeTexture
    )
    {
        _envs.Add(
            new EnvCommand { Mesh = mesh, Transform = transform, SurfaceColor = color, CubeTexture = cubeTexture }
        );
    }

    public void SubmitOpaque(
        Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null,
        ID3D11ShaderResourceView? normTexture = null
    )
    {
        _opaques.Add(
            new OpaqueCommand
            {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                NormTexture = normTexture
            }
        );
    }

    public void SubmitWater(
        Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null,
        ID3D11ShaderResourceView? normTexture = null, ID3D11ShaderResourceView? envCubeTexture = null
    )
    {
        _waters.Add(
            new WaterCommand
            {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                NormTexture = normTexture,
                EnvCubeTexture = envCubeTexture
            }
        );
    }

    private void ClearGBuffer(ID3D11DeviceContext context)
    {
        Color4 clearColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        foreach (ID3D11RenderTargetView rtv in GI.Instance.GBufferRTVs)
        {
            context.ClearRenderTargetView(rtv, clearColor);
        }
    }

    private void ClearQueues()
    {
        _opaques.Clear();
        _envs.Clear();
        _waters.Clear();
    }

    private void DrawOpaqueBatch(ID3D11DeviceContext context, Mesh? excludeMesh = null)
    {
        Shader gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        foreach (OpaqueCommand cmd in _opaques.Where(cmd => cmd.Mesh != excludeMesh))
        {
            _modelBuffer?.Update(new ConstantBufferModel { Model = cmd.Transform, ModelInv = cmd.InvTransform });
            _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = cmd.SurfaceColor });

            context.PSSetShaderResources(0, [cmd.Texture ?? GI.Instance.DefaultWhiteTextureSRV]);
            context.PSSetShaderResources(1, [cmd.NormTexture ?? GI.Instance.DefaultNormTexture]);

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void RenderEnvs(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState);
        context.OMSetBlendState(null);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        Shader envShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Env);
        envShader.Use();
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        foreach (EnvCommand cmd in _envs)
        {
            _modelBuffer?.Update(new ConstantBufferModel { Model = cmd.Transform });
            _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = cmd.SurfaceColor });

            context.PSSetShaderResources(0, [cmd.CubeTexture]);

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void RenderGPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        ClearGBuffer(context);
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        DrawOpaqueBatch(context);
    }

    private void RenderLightPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_noDepthState);
        context.OMSetBlendState(null);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView);

        Shader ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetDepthStencilState(_noDepthState);
        context.OMSetBlendState(_additiveBlendState);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        Shader lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, [null!, null!, null!]);
        context.OMSetBlendState(null);
    }

    private void RenderWaters(ID3D11DeviceContext context)
    {
        context.RSSetState(null);
        context.OMSetDepthStencilState(_defaultDepthState);
        context.OMSetBlendState(null);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        Shader waterShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Water);
        waterShader.Use();
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        foreach (WaterCommand cmd in _waters)
        {
            _modelBuffer?.Update(new ConstantBufferModel { Model = cmd.Transform });
            _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = cmd.SurfaceColor });

            context.PSSetShaderResources(0, [cmd.Texture ?? GI.Instance.DefaultWhiteTextureSRV]);
            context.PSSetShaderResources(1, [cmd.NormTexture ?? GI.Instance.DefaultNormTexture]);

            ID3D11ShaderResourceView envCube = cmd.EnvCubeTexture ??
                                               EnvCubeTexture ??
                                               throw new Exception("Env cube texture not available");
            context.PSSetShaderResources(2,
                [
                    envCube
                ]
            );

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }
}