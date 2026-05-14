struct VS_INPUT
{
    float3 Pos : POSITION0;
    
    float3 CurrentPos : INSTANCE_CURRPOS;
    float Age : INSTANCE_AGE;
    float3 PreviousPos : INSTANCE_PREVPOS;
    float MaxAge : INSTANCE_MAXAGE;
    float Texture : INSTANCE_TEXTURE;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float AgeAlpha : COLOR0;
    float Texture : TEX;
    float ClipDist : SV_ClipDistance0;
};