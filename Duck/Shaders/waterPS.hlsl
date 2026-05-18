// #pragma hlsl profile ps_6_6
// #pragma hlsl entry PS

#include "constantBuffers.hlsli"
#include "water.hlsli"

Texture2D Tex : register(t0);
Texture2D NormTex : register(t1);
TextureCube EnvMap : register(t2);
SamplerState Sampler : register(s0);

void intersectOneComponent(float p0c, float dc, inout float tmin, inout float tmax) 
{
    const float bounds[2] = { -5.0, 5.0 };

    float divc = 1.0f / dc;
    if (divc >= 0) 
    {
        tmin = (bounds[0] - p0c) * divc;
        tmax = (bounds[1] - p0c) * divc;
    }
    else 
    {
        tmin = (bounds[1] - p0c) * divc;
        tmax = (bounds[0] - p0c) * divc;
    }
}

float intersectRayT(float3 p0, float3 d)
{
    float3 tmin, tmax;
    intersectOneComponent(p0.x, d.x, tmin.x, tmax.x);
    intersectOneComponent(p0.y, d.y, tmin.y, tmax.y);
    
    if (tmin.y > tmin.x)
        tmin.x = tmin.y;
    if (tmax.y < tmax.x)
        tmax.x = tmax.y;
    intersectOneComponent(p0.z, d.z, tmin.z, tmax.z);
    
    if (tmin.z > tmin.x)
        tmin.x = tmin.z;
    if (tmax.z < tmax.x)
        tmax.x = tmax.z;

    return tmax.x;
}


float3 intersectRay(float3 p0, float3 d)
{
    float t = intersectRayT(p0, d);
    float3 boxPos = p0 + d * t;
    return boxPos;
}

float fresnel(float n1, float n2, float3 n, float3 v) 
{
    float F0 = pow((n2 - n1) / (n1 + n2), 2.0f);
    float cosPhi = max(dot(n, v), 0.0f);
    return F0 + (1 - F0) * pow((1 - cosPhi), 5);
}

float4 PS(PS_INPUT input) : SV_TARGET
{
    const float BOX_SIZE = 5.0f;
    float4 texColor = Tex.Sample(Sampler, input.UV);

    if (any(abs(input.WorldPos.xz) >= BOX_SIZE))
        return texColor;

    float3 viewVec = normalize(CamPos - input.WorldPos);

    float refractionCoeff = 3.0f/4.0f;
    float3 norm = normalize(NormTex.Sample(Sampler, input.UV).xyz * 2.0f - 1.0f);
    if (dot(norm, viewVec) < 0) {
        norm = -norm;
        refractionCoeff = 1.0f / refractionCoeff;
    }

    float3 reflected = reflect(-viewVec, norm);
    float3 refracted = refract(-viewVec, norm, refractionCoeff);
    float3 reflectedInter = intersectRay(input.WorldPos, reflected);
    float3 refractedInter = intersectRay(input.WorldPos, refracted);

    float fresnelCoeff = fresnel(1.0f, 4.0f/3.0f, norm, viewVec);
    float3 reflectedCol = pow(EnvMap.Sample(Sampler, reflectedInter).rgb, 0.4545f);
    float3 refractedCol = pow(EnvMap.Sample(Sampler, refractedInter).rgb, 0.4545f);
    float4 color = float4(lerp(refractedCol, reflectedCol, fresnelCoeff), 1.0f);

    color.r = lerp(texColor.r, color.r, SurfaceColor.r);
    color.g = lerp(texColor.g, color.g, SurfaceColor.g);
    color.b = lerp(texColor.b, color.b, SurfaceColor.b);
    color.a = lerp(texColor.a, color.a, SurfaceColor.a);
    return color;
}
