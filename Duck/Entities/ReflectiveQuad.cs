using Vortice.Direct3D11;

namespace Duck.Entities;

public class ReflectiveQuad : Quad, IDisposable
{
    public ID3D11ShaderResourceView? Texture;

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Texture);
        GI.Instance.Pipeline.SubmitMirror(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Texture);
    }

    public void Dispose() => Texture?.Dispose();
}