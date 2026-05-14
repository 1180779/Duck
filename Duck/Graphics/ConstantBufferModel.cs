using System.Numerics;
using System.Runtime.InteropServices;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct ConstantBufferModel
{
    public Matrix4x4 Model;
    public Matrix4x4 ModelInv;
}