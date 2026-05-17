// #pragma hlsl profile vs_6_6
// #pragma hlsl entry VS

#include "constantBuffers.hlsli"
#include "gPass.hlsli"

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT)0;

    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    output.WorldPos = worldPos.xyz;
    output.Pos = mul(Projection, mul(View, worldPos));
    output.Norm = normalize(mul(input.Norm, (float3x3) ModelInv));
    output.UV = input.UV;

    return output;
}
