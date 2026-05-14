#define NLIGHTS 2

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}