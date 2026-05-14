using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferLight
{
    public const int MaxLights = 2;
    
    [InlineArray(MaxLights)]
    public struct LightArray
    {
        private Vector4 _element;
    }
    
    public LightArray LightPos;
    public LightArray LightColor;
}
