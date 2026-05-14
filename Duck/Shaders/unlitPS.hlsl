#include "constantBuffers.hlsli"

Texture2D    Tex     : register(t0);
SamplerState Sampler : register(s0);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV  : TEXCOORD0;
};

float4 PS(PS_INPUT input) : SV_Target
{
    float4 texColor = Tex.Sample(Sampler, input.UV);
    return texColor * SurfaceColor;
}
