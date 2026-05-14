using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace Duck.Graphics;

public class ConstantBuffer<T> : IDisposable where T : unmanaged
{
    public ID3D11Buffer Buffer { get; private set; }

    public ConstantBuffer()
    {
        var device = GI.Instance.Device;

        var cbDesc = new BufferDescription()
        {
            ByteWidth = (((uint)Unsafe.SizeOf<T>() + 15) / 16) * 16,
            BindFlags = BindFlags.ConstantBuffer,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write
        };

        Buffer = device.CreateBuffer(cbDesc);
    }

    public unsafe void Update(T data)
    {
        var context = GI.Instance.Context;
        var mappedResource = context.Map(Buffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        Unsafe.CopyBlock(mappedResource.DataPointer.ToPointer(), Unsafe.AsPointer(ref data), (uint)Unsafe.SizeOf<T>());
        context.Unmap(Buffer, 0);
    }

    public void Bind(int slot = 0)
    {
        var context = GI.Instance.Context;
        context.VSSetConstantBuffer((uint)slot, Buffer);
        context.PSSetConstantBuffer((uint)slot, Buffer);
        context.GSSetConstantBuffer((uint)slot, Buffer);
    }

    public void Dispose()
    {
        Buffer?.Dispose();
    }
}