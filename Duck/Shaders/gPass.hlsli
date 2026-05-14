struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
    float3 WorldPos : POSITION0;
    float2 UV : TEXCOORD0;
    float ClipDist : SV_ClipDistance0;
};

struct PS_OUTPUT
{
    float4 Color : SV_Target0;
    float4 Normal : SV_Target1;
    float4 WorldPos : SV_Target2;
};