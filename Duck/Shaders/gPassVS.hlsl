#include "constantBuffers.hlsli"
#include "gPass.hlsli"

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT) 0;

    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    output.WorldPos = (float3) worldPos;
    output.Pos = mul(Projection, mul(View, worldPos));
    output.Norm = normalize(mul(input.Norm, (float3x3) ModelInv));
    output.UV = float2((input.Pos.x + 1.0f) * 0.5f, (1.0f - input.Pos.y) * 0.5f);
    output.ClipDist = dot(worldPos.xyz, ClipPlane.xyz) + ClipPlane.w;

    return output;
}