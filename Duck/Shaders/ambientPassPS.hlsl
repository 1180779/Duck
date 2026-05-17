// #pragma hlsl profile ps_6_6
// #pragma hlsl entry PS

// #define DEBUG_COLOR
// #define DEBUG_NORMALS
// #define DEBUG_WORLDPOS

Texture2D ColorMap : register(t0);
Texture2D NormalMap : register(t1);
Texture2D WorldPosMap: register(t2);
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

#if defined(DEBUG_COLOR)
    return float4(albedoData.rgb, 1.0f);
#elif defined(DEBUG_NORMALS)
    float3 n = NormalMap.Sample(Sampler, input.TexCoord).xyz;
    return float4(n * 0.5f + 0.5f, 1.0f);
#elif defined(DEBUG_WORLDPOS)
    return float4(WorldPosMap.Sample(Sampler, input.TexCoord).xyz, 1.0f);
#else
    float3 color = albedoData.rgb;
    float ka = 0.2;
    return float4(color * ka, 1.0f);
#endif
}
