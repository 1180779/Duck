struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

PS_INPUT VS(uint id : SV_VertexID)
{
    PS_INPUT output;
    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Pos = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    return output;
}