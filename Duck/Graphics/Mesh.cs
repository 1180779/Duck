using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Duck.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex(Vector3 position, Vector3 normal)
{
    public const int Stride = 24;

    public Vector3 Normal = normal;
    public Vector3 Position = position;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPosition(Vector3 position)
{
    public const int Stride = 12;

    public Vector3 Position = position;
}

public class Mesh : IDisposable
{
    /// <summary>
    ///     Vertex buffer stride
    /// </summary>
    public readonly uint Stride;

    public Mesh(Vertex[] vertices, uint[] indices)
    {
        ID3D11Device device = GI.Instance.Device;

        VertexBuffer = device.CreateBuffer(vertices, BindFlags.VertexBuffer);
        IndexBuffer = device.CreateBuffer(indices, BindFlags.IndexBuffer);
        IndexCount = indices.Length;

        Stride = Vertex.Stride;
    }

    public Mesh(VertexPosition[] vertices, uint[] indices)
    {
        ID3D11Device device = GI.Instance.Device;

        VertexBuffer = device.CreateBuffer(vertices, BindFlags.VertexBuffer);
        IndexBuffer = device.CreateBuffer(indices, BindFlags.IndexBuffer);
        IndexCount = indices.Length;

        Stride = VertexPosition.Stride;
    }

    public ID3D11Buffer IndexBuffer
    {
        get;
    }

    public int IndexCount
    {
        get;
        private set;
    }

    public ID3D11Buffer VertexBuffer
    {
        get;
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }

    public void Bind()
    {
        ID3D11DeviceContext context = GI.Instance.Context;

        context.IASetVertexBuffer(0, VertexBuffer, Stride);
        context.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
    }

    public void Unbind()
    {
        ID3D11DeviceContext context = GI.Instance.Context;
        context.IASetVertexBuffer(0, null!, 0);
        context.IASetIndexBuffer(null, Format.Unknown, 0);
    }
}