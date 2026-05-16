using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Explicit, Size = 128)]
public struct ConstantBufferModel
{
    [FieldOffset(0)] public Matrix4x4 Model;
    [FieldOffset(64)] public Matrix4x4 ModelInv;
}