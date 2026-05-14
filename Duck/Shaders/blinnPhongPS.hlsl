#include "constantBuffers.hlsli"
#include "blinnPhong.hlsli"

#define NLIGHTS 2

Texture2D    Tex     : register(t0);
SamplerState Sampler : register(s0);

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}

static const float ks = 0.3;
static const float kd = 0.7;
static const float ka = 0.1;
static const float m = 20.0;

float4 phong(float3 worldPos, float3 norm, float3 view, float2 uv)
{
    view = normalize(view);
    norm = normalize(norm);
    float4 sampled = Tex.Sample(Sampler, uv);
    float3 sampled3 = sampled.rgb;
    float3 color = sampled3 * ka;
    for (int k = 0; k < NLIGHTS; ++k)
    {
        float3 lightVec = normalize(LightPos[k].xyz - worldPos);
        float3 halfVec = normalize(view + lightVec);
        color += LightColor[k].xyz * kd * sampled3 * saturate(dot(norm, lightVec));
        color += LightColor[k].xyz* ks * pow(saturate(dot(norm, halfVec)), m);
    }

    return saturate(float4(color, sampled.a));
}

float4 PS(PS_INPUT input, bool isFrontFace : SV_IsFrontFace) : SV_Target
{
    float3 norm = isFrontFace ? input.Norm : -input.Norm;
    float2 uv = isFrontFace ? input.UV : float2(1.0f, 1.0f) - input.UV;
    
    return SurfaceColor * phong(input.WorldPos, norm, input.View, uv);
}
