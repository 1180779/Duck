using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct ConstantBufferSurfaceColor
{
    [FieldOffset(0)] public Vector4 SurfaceColor;
}
