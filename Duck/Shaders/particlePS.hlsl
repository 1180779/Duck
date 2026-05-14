#include "particle.hlsli"

Texture2D Texture : register(t0);
Texture2D Texture2 : register(t1);
SamplerState Sampler : register(s0);

float4 PS(PS_INPUT input) : SV_Target
{
    float4 texColor = lerp(Texture.Sample(Sampler, input.UV), Texture2.Sample(Sampler, input.UV), input.Texture);
    float3 innerColor = float3(1, 1, 1);
    float3 outerColor = lerp(float3(1.0f, 1.0f, 0), float3(2.0f, 0.2f, 0), input.AgeAlpha);
    float3 particleColor = lerp(innerColor, outerColor, saturate(abs(input.UV.y - 0.5)+0.4));
    float finalAlpha = texColor.a * input.AgeAlpha;
    
    return float4(saturate(particleColor * texColor.rgb), saturate(finalAlpha + 0.2f));
}