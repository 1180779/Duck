using System.Numerics;
using Duck.Graphics;

namespace Duck.Entities;

public class Quad : Entity
{
    protected readonly Mesh _mesh;

    public readonly Transform Transform = new();
    public Vector4 Color
    {
        get;
    } = new(1.0f, 1.0f, 1.0f, 0.5f);

    public Quad()
    {
        var vertices = new Vertex[]
        {
            new(new Vector3(-1, -1, 0), new Vector3(0, 0, -1)),
            new(new Vector3(-1,  1, 0), new Vector3(0, 0, -1)),
            new(new Vector3( 1,  1, 0), new Vector3(0, 0, -1)),
            new(new Vector3( 1, -1, 0), new Vector3(0, 0, -1)),
            new(new Vector3(-1, -1, 0), new Vector3(0, 0, 1)),
            new(new Vector3(-1,  1, 0), new Vector3(0, 0, 1)),
            new(new Vector3( 1,  1, 0), new Vector3(0, 0, 1)),
            new(new Vector3( 1, -1, 0), new Vector3(0, 0, 1))
        };

        var indices = new uint[]
        {
            0, 1, 2,
            0, 2, 3,
            4, 6, 5,
            4, 7, 6,
        };
        var adjIndices = AdjacencyHelper.Build(vertices, indices);
        _mesh = new Mesh(vertices, indices, adjIndices);
    }

    public override void Render(Camera camera)
    {
       GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}