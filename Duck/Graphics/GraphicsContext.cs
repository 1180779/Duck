using StbImageSharp;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Duck.Graphics;

public sealed class GraphicsContext : IDisposable
{
    public const string ProjectNamespace = "Duck";
    public const string ShadersBasePath = $"{ProjectNamespace}.Shaders.";
    public const string TextureBasePath = $"{ProjectNamespace}.Textures.";

    private static GI? s_instance;

    public readonly ID3D11SamplerState DefaultSampler;
    public readonly ID3D11ShaderResourceView DefaultWhiteTextureSRV;

    public readonly ID3D11RenderTargetView[] GBufferRTVs = new ID3D11RenderTargetView[3];
    public readonly ID3D11ShaderResourceView[] GBufferSRVs = new ID3D11ShaderResourceView[3];

    public GraphicsContext(nint hwnd, uint width, uint height)
    {
        Create.DeviceAndSwapChain deviceAndSwapChain = Create.DeviceContextSwapChain(hwnd, width, height);
        SwapChain = deviceAndSwapChain.SwapChain;
        Device = deviceAndSwapChain.Device;
        Context = deviceAndSwapChain.Context;

        DefaultSampler = Create.SamplerState(Device);
        DefaultWhiteTextureSRV = Create.WhiteTexture(Device);

        using ID3D11Texture2D backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = Device.CreateRenderTargetView(backBuffer);

        Texture2DDescription depthDesc = Create.DepthTextureDesc(width, height);
        using ID3D11Texture2D depthBuffer = Device.CreateTexture2D(depthDesc);
        DepthStencilView = Device.CreateDepthStencilView(depthBuffer);

        Texture2DDescription gBufferDesc = Create.GBufferDesc(width, height);
        for (int i = 0; i < 3; i++)
        {
            using ID3D11Texture2D tex = Device.CreateTexture2D(gBufferDesc);
            GBufferRTVs[i] = Device.CreateRenderTargetView(tex);
            GBufferSRVs[i] = Device.CreateShaderResourceView(tex);
        }
    }

    public ID3D11DeviceContext Context
    {
        get;
    }

    public ID3D11DepthStencilView DepthStencilView
    {
        get;
        set;
    }

    public ID3D11Device Device
    {
        get;
    }

    public uint Height
    {
        get;
        private set;
    }

    public static GI Instance => s_instance ??
                                 throw new InvalidOperationException(
                                     $"{nameof(GraphicsContext)} is not initialized."
                                 );

    public LightManager LightManager
    {
        get;
    } = new();

    public RenderingPipeline Pipeline
    {
        get
        {
            field ??= new RenderingPipeline();
            return field;
        }
    }

    public ID3D11RenderTargetView RenderTargetView
    {
        get;
        set;
    }

    public ShaderManager ShaderManager
    {
        get;
    } = new();

    public IDXGISwapChain SwapChain
    {
        get;
    }

    public uint Width
    {
        get;
        private set;
    }

    public void Dispose()
    {
        DefaultSampler.Dispose();
        DefaultWhiteTextureSRV.Dispose();
        Context.Dispose();
        DepthStencilView.Dispose();
        Device.Dispose();
        LightManager.Dispose();
        Pipeline.Dispose();
        RenderTargetView.Dispose();
        SwapChain.Dispose();
    }

    public static void CreateInstance(nint hwnd, uint width, uint height)
    {
        s_instance ??= new GI(hwnd, width, height);
    }

    public ID3D11ShaderResourceView LoadTextureFromStream(Stream stream)
    {
        ImageResult? image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        Texture2DDescription textureDesc = new()
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        unsafe
        {
            fixed (byte* ptr = image.Data)
            {
                SubresourceData data = new((IntPtr)ptr, (uint)image.Width * 4);
                using ID3D11Texture2D texture = Device.CreateTexture2D(textureDesc, new[] { data });
                return Device.CreateShaderResourceView(texture);
            }
        }
    }

    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || (width == Width && height == Height))
        {
            return;
        }

        Width = width;
        Height = height;

        RenderTargetView.Dispose();
        DepthStencilView.Dispose();
        for (int i = 0; i < 3; i++)
        {
            GBufferRTVs[i].Dispose();
            GBufferSRVs[i].Dispose();
        }

        SwapChain.ResizeBuffers(2, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

        using ID3D11Texture2D backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = Device.CreateRenderTargetView(backBuffer);

        Texture2DDescription depthDesc = Create.DepthTextureDesc(width, height);
        using ID3D11Texture2D depthBuffer = Device.CreateTexture2D(depthDesc);
        DepthStencilView = Device.CreateDepthStencilView(depthBuffer);

        Texture2DDescription gBufferDesc = Create.GBufferDesc(width, height);
        for (int i = 0; i < 3; i++)
        {
            using ID3D11Texture2D tex = Device.CreateTexture2D(gBufferDesc);
            GBufferRTVs[i] = Device.CreateRenderTargetView(tex);
            GBufferSRVs[i] = Device.CreateShaderResourceView(tex);
        }

        Context.RSSetViewport(new Viewport(0, 0, width, height));
    }

    public static class Create
    {
        public static Texture2DDescription DepthTextureDesc(uint width, uint height)
        {
            return new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil
            };
        }

        public static DeviceAndSwapChain DeviceContextSwapChain(nint hwnd, uint width, uint height)
        {
            SwapChainDescription swapChainDesc = new()
            {
                BufferCount = 2,
                BufferDescription = new ModeDescription(width, height, Format.R8G8B8A8_UNorm),
                Windowed = true,
                OutputWindow = hwnd,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                BufferUsage = Usage.RenderTargetOutput
            };

            D3D11.D3D11CreateDeviceAndSwapChain(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                [FeatureLevel.Level_11_0],
                swapChainDesc,
                out IDXGISwapChain? swapChain,
                out ID3D11Device? device,
                out FeatureLevel? _,
                out ID3D11DeviceContext? context
            );

            return new DeviceAndSwapChain
            {
                Device = device ?? throw new ArgumentNullException(nameof(device)),
                SwapChain = swapChain ?? throw new ArgumentNullException(nameof(swapChain)),
                Context = context ?? throw new ArgumentNullException(nameof(context))
            };
        }

        public static Texture2DDescription GBufferDesc(uint width, uint height)
        {
            return new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            };
        }

        public static ID3D11SamplerState SamplerState(ID3D11Device device)
        {
            SamplerDescription samplerDesc = new()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunc = ComparisonFunction.Never,
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };

            return device.CreateSamplerState(samplerDesc);
        }

        public static ID3D11ShaderResourceView WhiteTexture(ID3D11Device device)
        {
            Texture2DDescription texDesc = new()
            {
                Width = 1,
                Height = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource
            };
            byte[] white = [255, 255, 255, 255];

            ID3D11ShaderResourceView whiteTexture;
            unsafe
            {
                fixed (byte* p = white)
                {
                    SubresourceData initData = new((nint)p, 4);
                    using ID3D11Texture2D tex = device.CreateTexture2D(texDesc, [initData]);
                    whiteTexture = device.CreateShaderResourceView(tex);
                }
            }

            return whiteTexture;
        }

        public struct DeviceAndSwapChain
        {
            public required ID3D11DeviceContext Context;
            public required ID3D11Device Device;
            public required IDXGISwapChain SwapChain;
        }
    }
}