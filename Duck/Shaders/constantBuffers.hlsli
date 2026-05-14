
cbuffer ConstantBuffer : register(b0)
{
    matrix Model;
    matrix ModelInv;
}

cbuffer ConstantBuffer : register(b1)
{
    matrix View;
    matrix Projection;
}

cbuffer ConstantBuffer : register(b2)
{
    float4 SurfaceColor;
}

cbuffer ClipPlaneBuffer : register(b4)
{
    float4 ClipPlane;
}
