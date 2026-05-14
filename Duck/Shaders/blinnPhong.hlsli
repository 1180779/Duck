
struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
    float3 WorldPos : POSITION0;
    float3 View : VIEWVEC0;
    float2 UV : TEXCOORD0;
    float ClipDist : SV_ClipDistance0;
};
