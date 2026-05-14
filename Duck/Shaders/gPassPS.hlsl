#include "constantBuffers.hlsli"
#include "gPass.hlsli"

Texture2D Tex : register(t0);
SamplerState Sampler : register(s0);

PS_OUTPUT PS(PS_INPUT input)
{
    PS_OUTPUT output;
    float4 texColor = Tex.Sample(Sampler, input.UV);
    output.Color = texColor * SurfaceColor;
    output.Normal = float4(normalize(input.Norm), 1.0f);
    output.WorldPos = float4(input.WorldPos, 1.0f);
    
    return output;
}