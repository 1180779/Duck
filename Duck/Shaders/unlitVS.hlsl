#include "constantBuffers.hlsli"

struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

struct PS_INPUT
{
    float4 Pos      : SV_POSITION;
    float3 Norm     : NORMAL;
    float2 UV       : TEXCOORD0;
    float  ClipDist : SV_ClipDistance0;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT) 0;

    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    float4 viewPos = mul(View, worldPos);
    output.Pos = mul(Projection, viewPos);

    output.Norm = mul(input.Norm, (float3x3) ModelInv);
    output.UV = float2((input.Pos.x + 1.0f) * 0.5f, (1.0f - input.Pos.y) * 0.5f);
    output.ClipDist = dot(worldPos.xyz, ClipPlane.xyz) + ClipPlane.w;

    return output;
}
