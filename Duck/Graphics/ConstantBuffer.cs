using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Duck.Graphics;

public class ConstantBuffer<T> : IDisposable where T : unmanaged
{
    public ConstantBuffer()
    {
        ID3D11Device device = GI.Instance.Device;

        BufferDescription cbDesc = new()
        {
            ByteWidth = ((uint)Unsafe.SizeOf<T>() + 15) / 16 * 16,
            BindFlags = BindFlags.ConstantBuffer,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write
        };

        Buffer = device.CreateBuffer(cbDesc);
    }

    public ID3D11Buffer Buffer
    {
        get;
    }

    public void Dispose()
    {
        Buffer?.Dispose();
    }

    public void Bind(int slot = 0)
    {
        ID3D11DeviceContext context = GI.Instance.Context;
        context.VSSetConstantBuffer((uint)slot, Buffer);
        context.PSSetConstantBuffer((uint)slot, Buffer);
        context.GSSetConstantBuffer((uint)slot, Buffer);
    }

    public unsafe void Update(T data)
    {
        ID3D11DeviceContext context = GI.Instance.Context;
        MappedSubresource mappedResource = context.Map(Buffer, 0, MapMode.WriteDiscard);
        Unsafe.CopyBlock(mappedResource.DataPointer.ToPointer(), Unsafe.AsPointer(ref data), (uint)Unsafe.SizeOf<T>());
        context.Unmap(Buffer, 0);
    }
}