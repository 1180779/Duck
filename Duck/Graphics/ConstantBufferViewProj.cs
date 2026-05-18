using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Explicit, Size = 144)]
public struct ConstantBufferViewProj
{
    [FieldOffset(0)] public Matrix4x4 View;
    [FieldOffset(64)] public Matrix4x4 Projection;
    [FieldOffset(128)] public Vector3 CamPos;
}