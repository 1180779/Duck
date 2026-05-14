using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferClipPlane
{
    public Vector4 ClipPlane;
}
