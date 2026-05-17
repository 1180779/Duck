// #pragma hlsl profile vs_6_6
// #pragma hlsl entry VS

#include "constantBuffers.hlsli"
#include "env.hlsli"

struct VS_INPUT
{
    float3 Pos : POSITION;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT)0;
    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    output.Pos = mul(Projection, mul(View, worldPos));
    output.TexCoord = input.Pos;
    return output;
}
