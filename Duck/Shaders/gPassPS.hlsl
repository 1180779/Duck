// #pragma hlsl profile ps_6_6
// #pragma hlsl entry PS

#include "constantBuffers.hlsli"
#include "gPass.hlsli"

Texture2D Tex : register(t0);
Texture2D NormTex : register(t1);
SamplerState Sampler : register(s0);

float3 normalMapping(float3 N, float3 T, float3 tn)
{
    float3 B = normalize(cross(N, T));
    T = cross(B, N);
    // [T B N] * tn = 
    // = [T B N] * [tn.x tn.y tn.z] = [
    //  T.x * tn.x + B.x * tn.y + N.x * tn.z, 
    //  T.y * tn.x + B.y * tn.y + N.y * tn.z, 
    //  T.z * tn.x + B.z * tn.y + N.z * tn.z
    // ] = 
    // = T * tn.x + B * tn.y + N * tn.z
    return normalize(T * tn.x + B * tn.y + N * tn.z);
}

PS_OUTPUT PS(PS_INPUT input)
{
    PS_OUTPUT output;
    
    float3 N = normalize(input.Norm);
    float3 dPdx = ddx(input.WorldPos);
    float3 dPdy = ddy(input.WorldPos);
    float2 dtdx = ddx(input.UV);
    float2 dtdy = ddy(input.UV);
    float3 T = normalize(dPdx * dtdy.y - dPdy * dtdx.y);

    float3 tn = NormTex.Sample(Sampler, input.UV).rgb;
    tn = tn * 2.0f - 1.0f;
    tn.y = -tn.y;

    float3 norm = normalMapping(N, T, tn);
    float4 texColor = Tex.Sample(Sampler, input.UV);
    output.Color = texColor * SurfaceColor;
    output.Normal = float4(norm, 1.0f);
    output.WorldPos = float4(input.WorldPos, 1.0f);
    
    return output;
}
