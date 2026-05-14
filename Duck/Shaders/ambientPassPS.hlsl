Texture2D ColorMap : register(t0);
SamplerState Sampler : register(s0);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(PS_INPUT input) : SV_Target
{
    float4 albedoData = ColorMap.Sample(Sampler, input.TexCoord);
    
    clip(albedoData.a - 0.001f);

    float3 color = albedoData.rgb;
    float ka = 0.2;
    return float4(color * ka, 1.0f);
}