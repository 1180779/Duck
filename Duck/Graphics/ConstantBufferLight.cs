using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct ConstantBufferLight
{
    public const int MaxLights = 2;

    [InlineArray(MaxLights)]
    public struct LightArray
    {
        private Vector4 _element;
    }

    [FieldOffset(0)] public LightArray LightPos;
    [FieldOffset(32)] public LightArray LightColor;
}
