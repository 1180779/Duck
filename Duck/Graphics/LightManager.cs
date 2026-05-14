using System.Numerics;

namespace Duck.Graphics;

public class LightManager : IDisposable
{
    private ConstantBufferLight.LightArray _positions;
    private ConstantBufferLight.LightArray _colors;
    private int _count;

    private ConstantBuffer<ConstantBufferLight>? _buffer;
    private ConstantBuffer<ConstantBufferLight> Buffer => _buffer ??= new ConstantBuffer<ConstantBufferLight>();

    public void Clear() => _count = 0;

    public void Add(Vector3 position, Vector4 color)
    {
        if (_count < ConstantBufferLight.MaxLights)
        {
            _positions[_count] = new Vector4(position, 1);
            _colors[_count] = color;
            _count++;
        }
    }

    public void Update()
    {
        var data = new ConstantBufferLight { LightPos = _positions, LightColor = _colors };
        Buffer.Update(data);
    }

    public void Bind(int slot)
    {
        Buffer.Bind(slot);
    }

    public void Dispose()
    {
        _buffer?.Dispose();
    }
}