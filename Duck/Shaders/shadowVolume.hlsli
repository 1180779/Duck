struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

struct GS_INPUT
{
    float3 WorldPos : POSITION0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
};

