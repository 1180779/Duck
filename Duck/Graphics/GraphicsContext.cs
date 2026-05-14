using StbImageSharp;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Duck.Graphics;

public class GraphicsContext
{
    public const string TextureBasePath = "GK2PUMA.Textures.";
    private static GraphicsContext s_instance;
    public static GraphicsContext Instance => s_instance ??= new GraphicsContext();

    public ID3D11Device Device { get; set; }
    public ID3D11DeviceContext Context { get; set; }
    public IDXGISwapChain SwapChain { get; set; }
    public ID3D11RenderTargetView RenderTargetView { get; set; }
    public ID3D11DepthStencilView DepthStencilView { get; set; }
    public ShaderManager ShaderManager { get; set; } = new ShaderManager();
    public LightManager LightManager { get; } = new();
    public RenderingPipeline Pipeline { get; } = new();

    public ID3D11RenderTargetView[] GBufferRTVs = new ID3D11RenderTargetView[3];
    public ID3D11ShaderResourceView[] GBufferSRVs = new ID3D11ShaderResourceView[3];
    public ID3D11SamplerState DefaultSampler;
    public ID3D11ShaderResourceView DefaultWhiteTextureSRV;

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    
    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || (width == Width && height == Height))
        {
            return;
        }

        Width = width;
        Height = height;

        RenderTargetView?.Dispose();
        DepthStencilView?.Dispose();
        for (int i = 0; i < 3; i++)
        {
            GBufferRTVs[i]?.Dispose();
            GBufferSRVs[i]?.Dispose();
        }

        SwapChain.ResizeBuffers(2, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

        using var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = Device.CreateRenderTargetView(backBuffer);

        var depthDesc = new Texture2DDescription
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

        using var depthBuffer = Device.CreateTexture2D(depthDesc);
        DepthStencilView = Device.CreateDepthStencilView(depthBuffer);

        var gBufferDesc = new Texture2DDescription
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

        for (int i = 0; i < 3; i++)
        {
            using var tex = Device.CreateTexture2D(gBufferDesc);
            GBufferRTVs[i] = Device.CreateRenderTargetView(tex);
            GBufferSRVs[i] = Device.CreateShaderResourceView(tex);
        }

        if (DefaultSampler == null)
        {
            var samplerDesc = new SamplerDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunc = ComparisonFunction.Never,
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };
            DefaultSampler = Device.CreateSamplerState(samplerDesc);
        }

        if (DefaultWhiteTextureSRV == null)
        {
            var texDesc = new Texture2DDescription
            {
                Width = 1, Height = 1, MipLevels = 1, ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource
            };
            byte[] white = [255, 255, 255, 255];
            unsafe
            {
                fixed (byte* p = white)
                {
                    var initData = new SubresourceData((nint)p, 4, 0);
                    using var tex = Device.CreateTexture2D(texDesc, [initData]);
                    DefaultWhiteTextureSRV = Device.CreateShaderResourceView(tex);
                }
            }
        }

        Context.RSSetViewport(new Viewport(0, 0, width, height));
    }

    public ID3D11ShaderResourceView LoadTextureFromStream(Stream stream)
    {
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var textureDesc = new Texture2DDescription
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
                var data = new SubresourceData((IntPtr)ptr, (uint)image.Width * 4);
                using var texture = Device.CreateTexture2D(textureDesc, new[] { data });
                return Device.CreateShaderResourceView(texture);
            }
        }
    }

}