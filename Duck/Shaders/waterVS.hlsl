// #pragma hlsl profile vs_6_6
// #pragma hlsl entry VS

#include "constantBuffers.hlsli"
#include "water.hlsli"

struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
    float2 UV : TEXCOORD0;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT)0;
    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    output.WorldPos = worldPos.xyz;
    output.Pos = mul(Projection, mul(View, worldPos));
    output.Norm = normalize(mul(float4(input.Norm, 0.0f), ModelInv).xyz);
    output.UV = input.UV;
    return output;
}
