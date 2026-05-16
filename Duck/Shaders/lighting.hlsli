// #pragma hlsl profile lib_6_6

#define NLIGHTS 2

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}
