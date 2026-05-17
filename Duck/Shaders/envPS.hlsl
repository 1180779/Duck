// #pragma hlsl profile ps_6_6
// #pragma hlsl entry PS

#include "env.hlsli"

sampler samp;
TextureCube envMap;

float4 PS(PS_INPUT input) : SV_TARGET
{
    float3 color = envMap.Sample(samp, input.TexCoord).rgb;
    color = pow(color, 0.4545f);
    return float4(color, 1.0f);
}
