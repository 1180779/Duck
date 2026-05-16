using System.Text;

using SharpGen.Runtime;

using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Duck.Graphics;

public sealed class Shader : IDisposable
{
    public Shader(string vsPath, string? psPath, InputElementDescription[] inputElements, string? gsPath = null)
    {
        ID3D11Device device = GI.Instance.Device;

        int lastDot = vsPath.LastIndexOf('.');
        int secondLastDot = lastDot > 0 ? vsPath.LastIndexOf('.', lastDot - 1) : -1;
        string resourcePrefix = secondLastDot >= 0 ? vsPath[..(secondLastDot + 1)] : string.Empty;

        using EmbeddedInclude include = new(resourcePrefix);

        try
        {
            ReadOnlyMemory<byte> vsBlob =
                Compiler.Compile(Resources.ReadResource(vsPath), [], include, "VS", vsPath, "vs_5_0");
            VertexShader = device.CreateVertexShader(vsBlob.Span);

            if (!string.IsNullOrEmpty(psPath))
            {
                ReadOnlyMemory<byte> psBlob =
                    Compiler.Compile(Resources.ReadResource(psPath), [], include, "PS", psPath, "ps_5_0");
                PixelShader = device.CreatePixelShader(psBlob.Span);
            }

            if (!string.IsNullOrEmpty(gsPath))
            {
                ReadOnlyMemory<byte> gsBlob =
                    Compiler.Compile(Resources.ReadResource(gsPath), [], include, "GS", gsPath, "gs_5_0");
                GeometryShader = device.CreateGeometryShader(gsBlob.Span);
            }

            if (inputElements is { Length: > 0 })
            {
                InputLayout = device.CreateInputLayout(inputElements, vsBlob.Span);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Shader compilation failed:\n{ex.Message}", ex);
        }
    }

    public ID3D11GeometryShader? GeometryShader
    {
        get;
    }

    public ID3D11InputLayout? InputLayout
    {
        get;
    }

    public ID3D11PixelShader? PixelShader
    {
        get;
    }

    public ID3D11VertexShader VertexShader
    {
        get;
    }

    public void Dispose()
    {
        VertexShader.Dispose();
        PixelShader?.Dispose();
        GeometryShader?.Dispose();
        InputLayout?.Dispose();
    }

    public void Use()
    {
        ID3D11DeviceContext context = GI.Instance.Context;

        context.IASetInputLayout(InputLayout);
        context.VSSetShader(VertexShader);
        context.PSSetShader(PixelShader);
        context.GSSetShader(GeometryShader);
    }

    private sealed class EmbeddedInclude(string prefix) : CallbackBase, Include
    {
        public Stream Open(IncludeType type, string fileName, Stream? parentStream)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(Resources.ReadResource(prefix + fileName)));
        }

        public void Close(Stream stream)
        {
            stream.Dispose();
        }
    }
}