#include "lighting.hlsli"

Texture2D ColorMap : register(t0);
Texture2D NormalMap : register(t1);
Texture2D WorldPosMap : register(t2);
SamplerState Sampler : register(s0);

cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
    float3 CamPos;
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

//https://www.shadertoy.com/view/4djSRW
float GetDitherNoise(float2 p)
{
    float x = sin(dot(p, float2(12.9898, 78.233))) * 43758.5453;
    return x - floor(x);
}

float4 PS(PS_INPUT input) : SV_Target
{
    float4 albedoData = ColorMap.Sample(Sampler, input.TexCoord);
    
    clip(albedoData.a - 0.001f);
    
    float3 albedo = albedoData.rgb;
    float3 normal = NormalMap.Sample(Sampler, input.TexCoord).xyz;
    float3 worldPos = WorldPosMap.Sample(Sampler, input.TexCoord).xyz;

    float3 cameraPos = CamPos;
    float3 viewDir = normalize(cameraPos - worldPos);

    float kd = 0.8;
    float ks = 0.3;
    float m = 20.0;

    float3 color = float3(0, 0, 0);
    for (int k = 0; k < NLIGHTS; ++k)
    {
        float3 lightVec = normalize(LightPos[k].xyz - worldPos);
        float3 halfVec = normalize(viewDir + lightVec);
        
        color += LightColor[k].xyz * kd * albedo * saturate(dot(normal, lightVec));
        color += LightColor[k].xyz * ks * pow(saturate(dot(normal, halfVec)), m);
    }
    float dither = GetDitherNoise(input.Pos.xy) - 0.5f;
    
    color += dither * (1.0f / 255.0f);
    
    return float4(color, 1.0f);
}