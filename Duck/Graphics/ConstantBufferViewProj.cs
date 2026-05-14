using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferViewProj
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Vector3 Pos;
}