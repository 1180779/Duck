using System.Numerics;

using Duck.Graphics;

using Vortice.Direct3D11;

namespace Duck.Entities;

public sealed class Duck : Entity, IDisposable
{
    public readonly Transform Transform = new();
    private readonly Mesh _mesh;

    public Duck(Vector3 position, Vector4? color = null, Vector3 meshPivot = default)
    {
        DuckParser.DuckParser parser = new();
        parser.Load("duck.txt");
        _mesh = parser.BuildMesh(meshPivot);

        Transform.Position = position;
        Transform.Scale = (float)(1.0 / 200.0);
        if (color.HasValue)
        {
            Color = color.Value;
        }
    }

    public Vector4 Color
    {
        get;
    } = new(1.0f);

    public ID3D11ShaderResourceView? Texture
    {
        get;
        init;
    }

    public void Dispose()
    {
        _mesh.Dispose();
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Texture);
    }
}