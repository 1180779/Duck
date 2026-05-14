#include "lighting.hlsli"
#include "shadowVolume.hlsli"

cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
    float3 CamPos;
}

float4 Transform(float3 vert)
{
    float4 v = mul(View, float4(vert, 1.0));
    return mul(Projection, v);
}

static const float EPS = 0.01;
static const float SHADOW_LENGTH = 100.0;

void EmitQuad(float3 edgeStart, float3 edgeEnd, float3 startDir, float3 endDir, inout TriangleStream<PS_INPUT> output)
{
    PS_INPUT o;
    float3 lPos = LightPos[0].xyz;

    float4 s0 = Transform(edgeStart + startDir * EPS);
    float4 s1 = Transform(edgeStart + startDir * SHADOW_LENGTH);

    float4 e0 = Transform(edgeEnd + endDir * EPS);
    float4 e1 = Transform(edgeEnd + endDir * SHADOW_LENGTH);

    o.Pos = s0;
    output.Append(o);
    o.Pos = s1;
    output.Append(o);
    o.Pos = e0;
    output.Append(o);
    o.Pos = e1;
    output.Append(o);

    output.RestartStrip();
}

[maxvertexcount(18)]
void GS(triangleadj GS_INPUT input[6], inout TriangleStream<PS_INPUT> output)
{
    float3 lPos = LightPos[0].xyz;

    float3 v0 = input[0].WorldPos;
    float3 v2 = input[2].WorldPos;
    float3 v4 = input[4].WorldPos;

    float3 e1 = v2 - v0;
    float3 e2 = v4 - v0;
    float3 e3 = input[1].WorldPos - v0;
    float3 e4 = input[3].WorldPos - v2;
    float3 e5 = v4 - v2;
    float3 e6 = input[5].WorldPos - v0;

    float3 faceNorm = cross(e1, e2);
    

    if (dot(faceNorm, v0-lPos) >= 0.0)
    {
        return;
    }
    float3 lightDir[3] = { normalize(v0 - lPos), normalize(v2 - lPos), normalize(v4 - lPos) };
    
    float3 norm1 = cross(e3, e1);
    if (dot(norm1, lightDir[0]) >= 0.0)
    {
        EmitQuad(v0, v2, lightDir[0], lightDir[1], output);
    }

    float3 norm2 = cross(e4, e5);
    if (dot(norm2, lightDir[1]) >= 0.0)
    {
        EmitQuad(v2, v4, lightDir[1], lightDir[2], output);
    }

    float3 norm3 = cross(e2, e6);
    if (dot(norm3, lightDir[2]) >= 0.0)
    {
        EmitQuad(v4, v0, lightDir[2], lightDir[0], output);
    }

    PS_INPUT o;
    
    o.Pos = Transform(v0 + lightDir[0] * EPS);
    output.Append(o);
    o.Pos = Transform(v2 + lightDir[1] * EPS);
    output.Append(o);
    o.Pos = Transform(v4 + lightDir[2] * EPS);
    output.Append(o);
    output.RestartStrip();

    o.Pos = Transform(v2 + lightDir[1] * SHADOW_LENGTH);
    output.Append(o);
    o.Pos = Transform(v0 + lightDir[0] * SHADOW_LENGTH);
    output.Append(o);
    o.Pos = Transform(v4 + lightDir[2] * SHADOW_LENGTH);
    output.Append(o);
    output.RestartStrip();
}
