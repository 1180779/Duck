#include "particle.hlsli"

cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
    float3 CamPos;
}

cbuffer ClipPlaneBuffer : register(b4)
{
    float4 ClipPlane;
}

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;

    float3 velocity = input.CurrentPos - input.PreviousPos;
    float3 dir = normalize(velocity);

    float3 basePos = input.PreviousPos;

    float3 cameraPos = CamPos;
    float3 viewDir = normalize(cameraPos - basePos);

    float3 Ydir = normalize(cross(viewDir, dir));

    float XThickness = 0.4f;
    float YThickness = 0.04f;

    float centeredV = input.Pos.y * 2.0f - 1.0f;
    float3 pos = basePos + (input.Pos.x * XThickness * dir) + (centeredV * YThickness * Ydir);

    float4 v = mul(View, float4(pos, 1));
    output.Pos = mul(Projection, v);
    
    output.UV = input.Pos.xy;
    output.AgeAlpha = saturate(1.0f - (input.Age / input.MaxAge));
    output.Texture = input.Texture;
    output.ClipDist = dot(pos.xyz, ClipPlane.xyz) + ClipPlane.w;
    
    return output;
}