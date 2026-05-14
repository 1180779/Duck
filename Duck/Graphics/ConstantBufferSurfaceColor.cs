using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferSurfaceColor
{
    public Vector4 SurfaceColor;
}
