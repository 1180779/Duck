using System.Numerics;
using System.Runtime.InteropServices;

using Duck.Entities;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Duck.Graphics;

public struct OpaqueCommand
{
    public Mesh Mesh;
    public Matrix4x4 Transform;
    public Matrix4x4 InvTransform;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public bool CastsShadows;
}

public struct MirrorCommand
{
    public Mesh Mesh;
    public Matrix4x4 Transform;
    public Matrix4x4 InvTransform;
    public Vector4 SurfaceColor;
    public ID3D11ShaderResourceView? Texture;
    public bool CastsShadows;
}

public struct ParticleCommand
{
    public Mesh Mesh;
    public ID3D11Buffer InstanceBuffer;
    public int InstanceCount;
    public ID3D11ShaderResourceView[]? Textures;
}

public class RenderingPipeline : IDisposable
{
    private readonly List<MirrorCommand> _mirrors = new();
    private readonly List<OpaqueCommand> _opaques = new();
    private readonly List<ParticleCommand> _particles = new();

    private ConstantBuffer<ConstantBufferModel>? _modelBuffer;
    private ConstantBuffer<ConstantBufferSurfaceColor>? _colorBuffer;
    private ConstantBuffer<ConstantBufferClipPlane>? _clipPlaneBuffer;

    private ID3D11DepthStencilState? _defaultDepthState;
    private ID3D11DepthStencilState? _noDepthState;
    private ID3D11DepthStencilState? _noDepthWriteState;
    private ID3D11RasterizerState? _cullBackState;
    private ID3D11RasterizerState? _cullFrontState;
    private ID3D11DepthStencilState? _shadowVolumeDepthState;
    private ID3D11DepthStencilState? _lightPassDepthState;
    private ID3D11RasterizerState? _cullNoneState;
    private ID3D11BlendState? _noColorWriteBlendState;
    private ID3D11BlendState? _additiveBlendState;
    private ID3D11BlendState? _alphaBlendState;

    // Mirror-specific
    
    /// Depth test read-only and stencil write
    private ID3D11DepthStencilState? _mirrorStencilWriteState;
    /// Depth test-write and stencil test == ref
    private ID3D11DepthStencilState? _mirrorGPassDepthState;
    private ID3D11DepthStencilState? _mirrorNoDepthWriteState;

    private Camera? _mirrorCamera;

    private static readonly Vector4 NoClipPlane = new(0, 0, 0, 1);

    public void Init()
    {
        var device = GI.Instance.Device;

        var depthDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _defaultDepthState = device.CreateDepthStencilState(depthDesc);

        var noDepthDesc = new DepthStencilDescription
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = false
        };
        _noDepthState = device.CreateDepthStencilState(noDepthDesc);

        var noDepthWriteDesc = new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.LessEqual,
            StencilEnable = false
        };
        _noDepthWriteState = device.CreateDepthStencilState(noDepthWriteDesc);

        var cullBackDesc = new RasterizerDescription
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        _cullBackState = device.CreateRasterizerState(cullBackDesc);

        var cullFrontDesc = new RasterizerDescription
        {
            CullMode = CullMode.Front,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = true
        };
        _cullFrontState = device.CreateRasterizerState(cullFrontDesc);

        var shadowDepthDesc = new DepthStencilDescription
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

        var cullNoneDesc = new RasterizerDescription
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthClipEnable = false
        };
        _cullNoneState = device.CreateRasterizerState(cullNoneDesc);

        var noColorBlendDesc = new BlendDescription();
        noColorBlendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.None;
        _noColorWriteBlendState = device.CreateBlendState(noColorBlendDesc);

        var lightPassDepthDesc = new DepthStencilDescription
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
        
        var mirrorNoDepthWriteDesc = new DepthStencilDescription
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

        var additiveBlendDesc = new BlendDescription();
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

        var alphaBlendDesc = new BlendDescription();
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

        var mirrorStencilWriteDesc = new DepthStencilDescription
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

        var mirrorGPassDepthDesc = new DepthStencilDescription
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
        
        _modelBuffer = new();
        _colorBuffer = new();
        _clipPlaneBuffer = new();
        _mirrorCamera = new Camera(1.0f);
    }

    public void SubmitOpaque(Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null, bool castsShadows = true)
    {
        _opaques.Add(
            new OpaqueCommand {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                CastsShadows = castsShadows
            }
        );
    }

    public void SubmitMirror(Mesh mesh, Matrix4x4 transform, Matrix4x4 invTransform, Vector4 color, ID3D11ShaderResourceView? texture = null, bool castsShadows = true)
    {
        _mirrors.Add(
            new MirrorCommand {
                Mesh = mesh,
                Transform = transform,
                InvTransform = invTransform,
                SurfaceColor = color,
                Texture = texture,
                CastsShadows = castsShadows
            }
        );
    }

    public void SubmitParticle(Mesh mesh, ID3D11Buffer instanceBuffer, int instanceCount, ID3D11ShaderResourceView[]? textures = null)
    {
        _particles.Add(
            new ParticleCommand {
                Mesh = mesh,
                InstanceBuffer = instanceBuffer,
                InstanceCount = instanceCount,
                Textures = textures
            }
        );
    }

    public void Execute(Camera mainCamera)
    {
        var context = GI.Instance.Context;

        context.OMSetRenderTargets(GI.Instance.RenderTargetView);
        context.ClearRenderTargetView(GI.Instance.RenderTargetView, new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        context.ClearDepthStencilView(
            GI.Instance.DepthStencilView,
            DepthStencilClearFlags.Stencil,
            1.0f,
            0
        );
        _modelBuffer?.Bind(0);
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
        RenderShadowVolume(context, mainCamera);
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
        foreach (var mirrorCommand in _mirrors)
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
            RenderMirrorShadowVolume(context, _mirrorCamera);
            RenderMirrorLightPass(context, _mirrorCamera, stencilRef);
            RenderMirrorParticles(context, mainCamera, _mirrorCamera, stencilRef);
            RenderMirrorSurface(context, mainCamera, mirrorCommand, stencilRef);
        }
        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });
        RenderParticles(context, mainCamera);

        ClearQueues();
    }

    private void ClearGBuffer(ID3D11DeviceContext context)
    {
        var clearColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        foreach (var rtv in GI.Instance.GBufferRTVs)
        {
            context.ClearRenderTargetView(rtv, clearColor);
        }
    }

    private void DrawOpaqueBatch(ID3D11DeviceContext context, Mesh? excludeMesh = null)
    {
        var gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        foreach (var cmd in _opaques.Where(cmd => cmd.Mesh != excludeMesh))
        {
            _modelBuffer?.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });
            _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = cmd.SurfaceColor });

            context.PSSetShaderResources(0, [cmd.Texture ?? GI.Instance.DefaultWhiteTextureSRV]);

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }
    }

    private void RenderMirrorStencilPass(ID3D11DeviceContext context)
    {
        uint stencilRef = 0;
        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_noColorWriteBlendState);
        context.RSSetState(_cullNoneState);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        var gPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.GPass);
        gPassShader.Use();

        foreach (var cmd in _mirrors)
        {
            stencilRef++;
            context.OMSetDepthStencilState(_mirrorStencilWriteState, stencilRef);
            _modelBuffer?.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            cmd.Mesh.Bind();
            context.DrawIndexed((uint)cmd.Mesh.IndexCount, 0, 0);
        }

        context.OMSetBlendState(null);
    }

    private void RenderMirrorGPass(ID3D11DeviceContext context, Camera mirrorCamera, Camera mainCamera, MirrorCommand mirrorCommand, uint stencilRef)
    {
        UpdateClipPlane(mainCamera, mirrorCommand);

        context.RSSetState(_cullFrontState);
        context.OMSetDepthStencilState(_mirrorGPassDepthState, stencilRef);
        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        ClearGBuffer(context);
        DrawOpaqueBatch(context, mirrorCommand.Mesh);
    }

    private void UpdateClipPlane(Camera camera, MirrorCommand mirrorCommand)
    {
        var mt = mirrorCommand.Transform;
        Vector3 worldOrigin = new(mt.M41, mt.M42, mt.M43);
        Vector3 worldNormal = Vector3.Normalize(new Vector3(-mt.M31, -mt.M32, -mt.M33));
        float planeD = -Vector3.Dot(worldNormal, worldOrigin);
        float cameraSide = Vector3.Dot(camera.Position, worldNormal) + planeD;

        Vector4 clipPlane = cameraSide >= 0
            ? new Vector4(worldNormal, planeD + 0.001f)
            : new Vector4(-worldNormal, -planeD + 0.001f);

        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = clipPlane });
    }

    private void RenderMirrorShadowVolume(ID3D11DeviceContext context, Camera mirrorCamera)
    {
        // TODO: Implement in free time
    }

    private void RenderMirrorLightPass(ID3D11DeviceContext context, Camera mirrorCamera, uint stencilRef)
    {
        context.OMSetDepthStencilState(_lightPassDepthState, stencilRef);
        PerformLightPass(context);
    }

    private void PerformLightPass(ID3D11DeviceContext context)
    {
        context.RSSetState(_cullBackState);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(null);

        var ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetBlendState(_additiveBlendState);

        var lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, [null, null, null]);
        context.OMSetBlendState(null);
    }

    private void RenderMirrorParticles(ID3D11DeviceContext context, Camera mainCamera, Camera mirrorCamera, uint stencilRef)
    {
        if (_particles.Count == 0)
        {
            return;
        }

        mirrorCamera.UpdateAndBindViewProjBuffer(mainCamera.Position);
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_mirrorNoDepthWriteState, stencilRef);
        context.OMSetBlendState(_additiveBlendState);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var particleShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Particle);
        particleShader.Use();

        foreach (var cmd in _particles)
        {
            if (cmd.InstanceCount == 0)
            {
                continue;
            }

            if (cmd.Textures != null)
            {
                context.PSSetShaderResources(0, cmd.Textures);
                context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });
            }

            cmd.Mesh.Bind();
            context.IASetVertexBuffers(1, new[] { cmd.InstanceBuffer }, new[] { (uint)Marshal.SizeOf<ParticleInstance>() }, new[] { 0u });
            context.DrawIndexedInstanced((uint)cmd.Mesh.IndexCount, (uint)cmd.InstanceCount, 0, 0, 0);
        }

        context.IASetVertexBuffers(1, new ID3D11Buffer[] { null }, new[] { 0u }, new[] { 0u });
        context.OMSetBlendState(null);
        
    }

    private void RenderMirrorSurface(ID3D11DeviceContext context, Camera mainCamera, MirrorCommand mirrorCommand, uint stencilRef)
    {
        mainCamera.UpdateAndBindViewProjBuffer();
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_alphaBlendState);
        context.OMSetDepthStencilState(_lightPassDepthState, stencilRef);
        context.RSSetState(_cullBackState);
        _clipPlaneBuffer?.Update(new ConstantBufferClipPlane { ClipPlane = NoClipPlane });

        var unlitShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.BlinnPhong);
        unlitShader.Use();

        var tex = mirrorCommand.Texture ?? GI.Instance.DefaultWhiteTextureSRV;
        context.PSSetShaderResources(0, [tex]);
        context.PSSetSamplers(0, [GI.Instance.DefaultSampler]);

        _modelBuffer?.Update(new ConstantBufferModel
        {
            Model = mirrorCommand.Transform,
            ModelInv = mirrorCommand.InvTransform
        });
        _colorBuffer?.Update(new ConstantBufferSurfaceColor { SurfaceColor = mirrorCommand.SurfaceColor });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        mirrorCommand.Mesh.Bind();
        context.DrawIndexed((uint)mirrorCommand.Mesh.IndexCount, 0, 0);
        mirrorCommand.Mesh.Unbind();

        context.OMSetBlendState(null);
        context.OMSetDepthStencilState(null, 0);
        context.RSSetState(null);
    }

    private void RenderGPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        ClearGBuffer(context);
        context.ClearDepthStencilView(GI.Instance.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        DrawOpaqueBatch(context);
    }

    private void RenderLightPass(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullBackState);
        context.OMSetDepthStencilState(_noDepthState, 0);
        context.OMSetBlendState(null);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, null);

        var ambientShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.AmbientPass);
        ambientShader.Use();

        context.PSSetShaderResources(0, GI.Instance.GBufferSRVs);
        context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.Draw(3, 0);

        context.OMSetDepthStencilState(_lightPassDepthState, 0);
        context.OMSetBlendState(_additiveBlendState);

        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var lightPassShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.LightPass);
        lightPassShader.Use();

        GI.Instance.LightManager.Bind(3);

        context.Draw(3, 0);

        context.PSSetShaderResources(0, new ID3D11ShaderResourceView[] { null, null, null });
        context.OMSetBlendState(null);
    }

    private void RenderShadowVolume(ID3D11DeviceContext context, Camera camera)
    {
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_shadowVolumeDepthState, 0);
        context.OMSetBlendState(_noColorWriteBlendState);

        context.OMSetRenderTargets(GI.Instance.GBufferRTVs, GI.Instance.DepthStencilView);

        var shadowShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.ShadowVolume);
        shadowShader.Use();

        _modelBuffer.Bind(0);
        GI.Instance.LightManager.Bind(3);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleListAdjacency);

        foreach (var cmd in _opaques.Where(cmd => cmd.CastsShadows))
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            cmd.Mesh.Bind(useAdjacency: true);
            context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
        }

        foreach (var cmd in _mirrors.Where(cmd => cmd.CastsShadows))
        {
            _modelBuffer.Update(new ConstantBufferModel
            {
                Model = cmd.Transform,
                ModelInv = cmd.InvTransform
            });

            cmd.Mesh.Bind(useAdjacency: true);
            context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
        }

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(null);
    }

    private void RenderShadowVolumeDEBUG(ID3D11DeviceContext context, Camera camera)
    {
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);
        context.OMSetBlendState(_additiveBlendState);
        context.OMSetDepthStencilState(_defaultDepthState, 0);

        GI.Instance.LightManager.Bind(3);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleListAdjacency);

        void DrawVolumes(Vector4 debugColor, ID3D11RasterizerState cullState)
        {
            context.RSSetState(cullState);

            _colorBuffer?.Update(new ConstantBufferSurfaceColor
            {
                SurfaceColor = debugColor
            });

            foreach (var cmd in _opaques.Where(cmd => cmd.CastsShadows))
            {
                _modelBuffer?.Update(new ConstantBufferModel
                {
                    Model = cmd.Transform,
                    ModelInv = cmd.InvTransform
                });

                cmd.Mesh.Bind(useAdjacency: true);
                context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
            }

            foreach (var cmd in _mirrors.Where(cmd => cmd.CastsShadows))
            {
                _modelBuffer?.Update(new ConstantBufferModel
                {
                    Model = cmd.Transform,
                    ModelInv = cmd.InvTransform
                });

                cmd.Mesh.Bind(useAdjacency: true);
                context.DrawIndexed((uint)cmd.Mesh.AdjacencyIndexCount, 0, 0);
            }
        }

        // comment one
        DrawVolumes(new Vector4(0.2f, 0.0f, 0.0f, 1.0f), _cullBackState);
        //DrawVolumes(new Vector4(0.0f, 0.2f, 0.0f, 1.0f), _cullFrontState);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(null);
    }

    private void RenderParticles(ID3D11DeviceContext context, Camera camera)
    {
        if (_particles.Count == 0)
        {
            return;
        }

        camera.UpdateAndBindViewProjBuffer();
        context.RSSetState(_cullNoneState);
        context.OMSetDepthStencilState(_noDepthWriteState, 0);
        context.OMSetBlendState(_additiveBlendState);
        context.OMSetRenderTargets(GI.Instance.RenderTargetView, GI.Instance.DepthStencilView);

        var particleShader = GI.Instance.ShaderManager.GetShader(ShaderManager.ShaderType.Particle);
        particleShader.Use();

        foreach (var cmd in _particles)
        {
            if (cmd.InstanceCount == 0)
            {
                continue;
            }

            if (cmd.Textures != null)
            {
                context.PSSetShaderResources(0, cmd.Textures);
                context.PSSetSamplers(0, new[] { GI.Instance.DefaultSampler });
            }

            cmd.Mesh.Bind();
            context.IASetVertexBuffers(1, new[] { cmd.InstanceBuffer }, new[] { (uint)Marshal.SizeOf<ParticleInstance>() }, new[] { 0u });
            context.DrawIndexedInstanced((uint)cmd.Mesh.IndexCount, (uint)cmd.InstanceCount, 0, 0, 0);
        }

        context.IASetVertexBuffers(1, new ID3D11Buffer[] { null }, new[] { 0u }, new[] { 0u });
        context.OMSetBlendState(null);
    }

    private void ClearQueues()
    {
        _opaques.Clear();
        _mirrors.Clear();
        _particles.Clear();
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
}
