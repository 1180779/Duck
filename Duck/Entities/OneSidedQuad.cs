using System.Numerics;

using Duck.Graphics;

namespace Duck.Entities;

public class OneSidedQuad : Entity
{
    public readonly Transform Transform = new();
    protected readonly Mesh _mesh;

    public OneSidedQuad()
    {
        Vertex[] vertices =
        [
            new(new Vector3(-1, -1, 0), new Vector3(0, 0, -1), new Vector2(0, 1)),
            new(new Vector3(-1, 1, 0), new Vector3(0, 0, -1), new Vector2(0, 0)),
            new(new Vector3(1, 1, 0), new Vector3(0, 0, -1), new Vector2(1, 0)),
            new(new Vector3(1, -1, 0), new Vector3(0, 0, -1), new Vector2(1, 1))
        ];

        uint[] indices = [0, 1, 2, 0, 2, 3];
        _mesh = new Mesh(vertices, indices);
    }

    public Vector4 Color
    {
        get;
        init;
    } = new(1.0f, 1.0f, 1.0f, 0.5f);

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}