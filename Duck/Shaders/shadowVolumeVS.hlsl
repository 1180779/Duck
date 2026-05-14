#include "constantBuffers.hlsli"
#include "shadowVolume.hlsli" 

GS_INPUT VS(VS_INPUT input)
{
    GS_INPUT output;
    output.WorldPos = mul(Model, float4(input.Pos, 1.0f)).xyz;
    return output;
}