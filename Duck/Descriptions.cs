using Vortice.Direct3D11;

namespace Duck;

public static class Descriptions
{
    public static DepthStencilDescription NewDepthStencilDescription()
    {
        return new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = false,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new()
            {
                StencilFunc = ComparisonFunction.Always,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFailOp = StencilOperation.Keep
            },
            BackFace = new()
            {
                StencilFunc = ComparisonFunction.Always,
                StencilDepthFailOp = StencilOperation.Keep,
                StencilPassOp = StencilOperation.Keep,
                StencilFailOp = StencilOperation.Keep
            }
        };
    }

    public static RasterizerDescription NewRasterizerDescription()
    {
        return new RasterizerDescription
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.Back,
            DepthBias = 0,
            DepthBiasClamp = 0.0f,
            SlopeScaledDepthBias = 0.0f,
            DepthClipEnable = true,
            ScissorEnable = false,
            MultisampleEnable = false,
            AntialiasedLineEnable = false,
        };
    }

    public static BlendDescription NewBlendDescription()
    {
        BlendDescription bsDesc = new();
        bsDesc.RenderTarget[0].SourceBlend = Blend.One;
        bsDesc.RenderTarget[0].DestinationBlend = Blend.Zero;
        bsDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        bsDesc.RenderTarget[0].SourceBlendAlpha = Blend.One;
        bsDesc.RenderTarget[0].DestinationBlendAlpha = Blend.Zero;
        bsDesc.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
        bsDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;
        return bsDesc;
    }
}