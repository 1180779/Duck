using System.Numerics;

using Duck.Entities;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Duck.Graphics;

public struct OpaqueCommand
{
    public Matrix4x4 InvTransform;
    public Mesh Mesh;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public Matrix4x4 Transform;
}

public struct MirrorCommand
{
    public Matrix4x4 InvTransform;
    public Mesh Mesh;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public Matrix4x4 Transform;
}

public sealed class RenderingPipeline : IDisposable
{
    private static readonly Vector4 NoClipPlane = new(0, 0, 0, 1);
    private readonly List<MirrorCommand> _mirrors = [];
    private readonly List<OpaqueCommand> _opaques = [];
    private readonly ID3D11BlendState? _additiveBlendState;
    private readonly ID3D11BlendState? _alphaBlendState;
    private readonly ConstantBuffer<ConstantBufferClipPlane>? _clipPlaneBuffer;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor>? _colorBuffer;
    private readonly ID3D11RasterizerState? _cullBackState;
    private readonly ID3D11RasterizerState? _cullFrontState;
    private readonly ID3D11RasterizerState? _cullNoneState;

    private readonly ID3D11DepthStencilState? _defaultDepthState;
    private readonly ID3D11DepthStencilState? _lightPassDepthState;

    private Camera? _mirrorCamera;

    /// Depth test-write and stencil test == ref
    private readonly ID3D11DepthStencilState? _mirrorGPassDepthState;

    private readonly ID3D11DepthStencilState? _mirrorNoDepthWriteState;

    // Mirror-specific

    /// Depth test read-only and stencil write
    private readonly ID3D11DepthStencilState? _mirrorStencilWriteState;

    private readonly ConstantBuffer<ConstantBufferModel>? _modelBuffer;
    private readonly ID3D11BlendState? _noColorWriteBlendState;
    private readonly ID3D11DepthStencilState? _noDepthState;
    private readonly ID3D11DepthStencilState? _noDepthWriteState;
    private readonly ID3D11DepthStencilState? _shadowVolumeDepthState;

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
        _cullFrontState = device.CreateRasterizerState(cullFrontDesc);

        DepthStencilDescription shadowDepthDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Decrement,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Always
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Increment,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Always
            }
        };
        _shadowVolumeDepthState = device.CreateDepthStencilState(shadowDepthDesc);

        RasterizerDescription cullNoneDesc = new()
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = false
        };
        _cullNoneState = device.CreateRasterizerState(cullNoneDesc);

        BlendDescription noColorBlendDesc = new();
        noColorBlendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.None;
        _noColorWriteBlendState = device.CreateBlendState(noColorBlendDesc);

        DepthStencilDescription lightPassDepthDesc = new()
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0x00,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            }
        };
        _lightPassDepthState = device.CreateDepthStencilState(lightPassDepthDesc);

        DepthStencilDescription mirrorNoDepthWriteDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0x00,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            }
        };
        _mirrorNoDepthWriteState = device.CreateDepthStencilState(mirrorNoDepthWriteDesc);

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

        DepthStencilDescription mirrorStencilWriteDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Replace,
                StencilFunc = ComparisonFunction.Always
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Replace,
                StencilFunc = ComparisonFunction.Always
            }
        };
        _mirrorStencilWriteState = device.CreateDepthStencilState(mirrorStencilWriteDesc);

        DepthStencilDescription mirrorGPassDepthDesc = new()
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = true,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0x00,
            FrontFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            },
            BackFace = new DepthStencilOperationDescription
            {
                StencilFailOp = StencilOperation.Keep,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFunc = ComparisonFunction.Equal
            }
        };
        _mirrorGPassDepthState = device.CreateDepthStencilState(mirrorGPassDepthDesc);

        _modelBuffer = new ConstantBuffer<ConstantBufferModel>();
        _colorBuffer = new ConstantBuffer<ConstantBufferSurfaceColor>();
        _clipPlaneBuffer = new ConstantBuffer<ConstantBufferClipPlane>();
        _mirrorCamera = new Camera(1.0f);
    }

    public void Dispose()
    {
        _modelBuffer?.Dispose();
        _colorBuffer?.Dispose();
        _clipPlaneBuffer?.Dispose();
        _mirrorCamera?.Dispose();
        _defaultDepthState?.Dispose();
        _noDepthState?.Dispose();
        _noDepthWriteState?.Dispose();
        _cullBackState?.Dispose();
        _cullFrontState?.Dispose();
        _shadowVolumeDepthState?.Dispose();
        _cullNoneState?.Dispose();
        _noColorWriteBlendState?.Dispose();
        _lightPassDepthState?.Dispose();
        _additiveBlendState?.Dispose();
        _alphaBlendState?.Dispose();
        _mirrorStencilWriteState?.Dispose();
        _mirrorGPassDepthState?.Dispose();
        _mirrorNoDepthWriteState?.Dispose();
    }

    public void Execute(Camera mainCamera)
    {
        ID3D11DeviceContext context = GI.Instance.Context;

        context.OMSetRenderTargets(GI.Instance.RenderTargetView);
        context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f));
        context.ClearDepthStencilView(
            GI.Instance.DepthStencilView,
            DepthStencilClearFlags.Stencil,
            1.0f,
            0
        );
        _modelBuffer?.Bind();
        _colorBuffer?.Bind(2);
        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });
        _clipPlaneBuffer?.Bind(4);

        context.ClearDepthStencilView(
            GI.Instance.DepthStencilView,
            DepthStencilClearFlags.Stencil,
            1.0f,
            0
        );
        mainCamera.UpdateAndBindViewProjBuffer();
        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });
        RenderGPass(context, mainCamera);
        RenderLightPass(context, mainCamera);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(null);
        context.ClearDepthStencilView(
            GI.Instance.DepthStencilView,
            DepthStencilClearFlags.Stencil,
            1.0f,
            0
        );
        mainCamera.UpdateAndBindViewProjBuffer();
        RenderMirrorStencilPass(context);

        uint stencilRef = 0;
        foreach (MirrorCommand mirrorCommand in _mirrors)
        {
            stencilRef++;
            _mirrorCamera ??= new Camera(1.0f);
            _mirrorCamera.UpdateAsMirror(mainCamera, mirrorCommand.Transform);
            _mirrorCamera.UpdateAndBindViewProjBuffer();

            context.ClearDepthStencilView(
                GI.Instance.DepthStencilView,
                DepthStencilClearFlags.Depth,
                1.0f,
                0
            );

            RenderMirrorGPass(context, _mirrorCamera, mainCamera, mirrorCommand, stencilRef);
            RenderMirrorLightPass(context, _mirrorCamera, stencilRef);
            RenderMirrorSurface(context, mainCamera, mirrorCommand, stencilRef);
        }

        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });

        ClearQueues();
    }

    public void SubmitMirror(
        Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null
    )
    {
        _mirrors.Add(
            new MirrorCommand
            {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture
            }
        );
    }

    public void SubmitOpaque(
        Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null
    )
    {
        _opaques.Add(
            new OpaqueCommand
            {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture
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
        _mirrors.Clear();
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

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void PerformLightPass(ID3D11DeviceContext context)
    {
        context.RSSetState(_cullBackState);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(null);

        Shader ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetBlendState(_additiveBlendState);

        Shader lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, [null!, null!, null!]);
        context.OMSetBlendState(null);
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

        context.OMSetDepthStencilState(_lightPassDepthState);
        context.OMSetBlendState(_additiveBlendState);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        Shader lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, [null!, null!, null!]);
        context.OMSetBlendState(null);
    }

    private void RenderMirrorGPass(
        ID3D11DeviceContext context, Camera mirrorCamera, Camera mainCamera, MirrorCommand mirrorCommand,
        uint stencilRef
    )
    {
        UpdateClipPlane(mainCamera, mirrorCommand);

        context.RSSetState(_cullFrontState);
        context.OMSetDepthStencilState(_mirrorGPassDepthState, stencilRef);
        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        ClearGBuffer(context);
        DrawOpaqueBatch(context, mirrorCommand.Mesh);
    }

    private void RenderMirrorLightPass(ID3D11DeviceContext context, Camera mirrorCamera, uint stencilRef)
    {
        context.OMSetDepthStencilState(_lightPassDepthState, stencilRef);
        PerformLightPass(context);
    }

    private void RenderMirrorStencilPass(ID3D11DeviceContext context)
    {
        uint stencilRef = 0;
        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_noColorWriteBlendState);
        context.RSSetState(_cullNoneState);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        Shader gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();

        foreach (MirrorCommand cmd in _mirrors)
        {
            stencilRef++;
            context.OMSetDepthStencilState(_mirrorStencilWriteState, stencilRef);
            _modelBuffer?.Update(new ConstantBufferModel { Model = cmd.Transform, ModelInv = cmd.InvTransform });

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }

        context.OMSetBlendState(null);
    }

    private void RenderMirrorSurface(
        ID3D11DeviceContext context, Camera mainCamera, MirrorCommand mirrorCommand, uint stencilRef
    )
    {
        mainCamera.UpdateAndBindViewProjBuffer();
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_alphaBlendState);
        context.OMSetDepthStencilState(_lightPassDepthState, stencilRef);
        context.RSSetState(_cullBackState);
        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });

        Shader unlitShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        unlitShader.Use();

        ID3D11ShaderResourceView tex = mirrorCommand.Texture ?? GI.Instance.DefaultWhiteTextureSRV;
        context.PSSetShaderResources(0, [tex]);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        _modelBuffer?.Update(new ConstantBufferModel
            {
                Model = mirrorCommand.Transform, ModelInv = mirrorCommand.InvTransform
            }
        );
        _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = mirrorCommand.SurfaceColor });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        mirrorCommand.Mesh.Bind();
        context.DrawIndexed((uint)mirrorCommand.Mesh.IndexCount, 0, 0);
        mirrorCommand.Mesh.Unbind();

        context.OMSetBlendState(null);
        context.OMSetDepthStencilState(null);
        context.RSSetState(null);
    }

    private void UpdateClipPlane(Camera camera, MirrorCommand mirrorCommand)
    {
        Matrix4x4 mt = mirrorCommand.Transform;
        Vector3 worldOrigin = new(mt.M41, mt.M42, mt.M43);
        Vector3 worldNormal = Vector3.Normalize(new Vector3(-mt.M31, -mt.M32, -mt.M33));
        float planeD = -Vector3.Dot(worldNormal, worldOrigin);
        float cameraSide = Vector3.Dot(camera.Position, worldNormal) + planeD;

        Vector4 clipPlane = cameraSide >= 0
            ? new Vector4(worldNormal, planeD + 0.001f)
            : new Vector4(-worldNormal, -planeD + 0.001f);

        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = clipPlane });
    }
}