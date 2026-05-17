using System.Numerics;

using Duck.Graphics;

using Vortice.Direct3D11;

namespace Duck.Entities;

public class InsideCube : Entity
{
    public const float HalfSize = 5.0f;

    public readonly Transform Transform = new();
    private readonly Mesh _mesh;

    public InsideCube()
    {
        Vertex[] vertices =
        [
            new(new Vector3(-5, -5, 5), new Vector3(0, 0, -1), new Vector2(0, 1)),
            new(new Vector3(-5, 5, 5), new Vector3(0, 0, -1), new Vector2(0, 0)),
            new(new Vector3(5, 5, 5), new Vector3(0, 0, -1), new Vector2(1, 0)),
            new(new Vector3(5, -5, 5), new Vector3(0, 0, -1), new Vector2(1, 1)),
            new(new Vector3(-5, -5, -5), new Vector3(0, 0, 1), new Vector2(1, 1)),
            new(new Vector3(5, -5, -5), new Vector3(0, 0, 1), new Vector2(0, 1)),
            new(new Vector3(5, 5, -5), new Vector3(0, 0, 1), new Vector2(0, 0)),
            new(new Vector3(-5, 5, -5), new Vector3(0, 0, 1), new Vector2(1, 0)),
            new(new Vector3(-5, -5, -5), new Vector3(1, 0, 0), new Vector2(0, 1)),
            new(new Vector3(-5, 5, -5), new Vector3(1, 0, 0), new Vector2(0, 0)),
            new(new Vector3(-5, 5, 5), new Vector3(1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(-5, -5, 5), new Vector3(1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(5, -5, -5), new Vector3(-1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(5, -5, 5), new Vector3(-1, 0, 0), new Vector2(0, 1)),
            new(new Vector3(5, 5, 5), new Vector3(-1, 0, 0), new Vector2(0, 0)),
            new(new Vector3(5, 5, -5), new Vector3(-1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(-5, 5, -5), new Vector3(0, -1, 0), new Vector2(0, 0)),
            new(new Vector3(5, 5, -5), new Vector3(0, -1, 0), new Vector2(1, 0)),
            new(new Vector3(5, 5, 5), new Vector3(0, -1, 0), new Vector2(1, 1)),
            new(new Vector3(-5, 5, 5), new Vector3(0, -1, 0), new Vector2(0, 1)),
            new(new Vector3(-5, -5, -5), new Vector3(0, 1, 0), new Vector2(0, 1)),
            new(new Vector3(-5, -5, 5), new Vector3(0, 1, 0), new Vector2(0, 0)),
            new(new Vector3(5, -5, 5), new Vector3(0, 1, 0), new Vector2(1, 0)),
            new(new Vector3(5, -5, -5), new Vector3(0, 1, 0), new Vector2(1, 1))
        ];

        uint[] indices =
        [
            0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 8, 10, 11, 12, 13, 14, 12, 14, 15, 16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        ];

        _mesh = new Mesh(vertices, indices);
    }

    public Vector4 Color
    {
        get;
    } = new(0.7f, 0.7f, 0.7f, 1.0f);

    public required ID3D11ShaderResourceView CubeTexture
    {
        get;
        init;
    }

    public override void Render(Camera camera)
    {
        // GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
        GI.Instance.Pipeline.SubmitEnv(_mesh, Transform.ModelMatrix, Color, CubeTexture);
    }
}