// #pragma hlsl profile lib_6_6

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
    float3 WorldPos : POSITION0;
    float3 View : VIEWVEC0;
    float2 UV : TEXCOORD0;
};
