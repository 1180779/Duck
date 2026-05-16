using System.Numerics;

using Duck.Graphics;

namespace Duck.Entities;

public class InsideCube : Entity
{
    private readonly Mesh _mesh;

    public readonly Transform Transform = new();
    public Vector4 Color
    {
        get;
    } = new(0.7f, 0.7f, 0.7f, 1.0f);
    public const float HalfSize = 5.0f;

    public InsideCube()
    {
        var vertices = new Vertex[]
        {
            new (new (-5, -5,  5), new (0, 0, -1)),
            new (new (-5,  5,  5), new (0, 0, -1)),
            new (new ( 5,  5,  5), new (0, 0, -1)),
            new (new ( 5, -5,  5), new (0, 0, -1)),

            new (new (-5, -5, -5), new (0, 0, 1)),
            new (new ( 5, -5, -5), new (0, 0, 1)),
            new (new ( 5,  5, -5), new (0, 0, 1)),
            new (new (-5,  5, -5), new (0, 0, 1)),

            new (new (-5, -5, -5), new (1, 0, 0)),
            new (new (-5,  5, -5), new (1, 0, 0)),
            new (new (-5,  5,  5), new (1, 0, 0)),
            new (new (-5, -5,  5), new (1, 0, 0)),

            new (new ( 5, -5, -5), new (-1, 0, 0)),
            new (new ( 5, -5,  5), new (-1, 0, 0)),
            new (new ( 5,  5,  5), new (-1, 0, 0)),
            new (new ( 5,  5, -5), new (-1, 0, 0)),

            new (new (-5,  5, -5), new (0, -1, 0)),
            new (new ( 5,  5, -5), new (0, -1, 0)),
            new (new ( 5,  5,  5), new (0, -1, 0)),
            new (new (-5,  5,  5), new (0, -1, 0)),

            new (new (-5, -5, -5), new (0, 1, 0)),
            new (new (-5, -5,  5), new (0, 1, 0)),
            new (new ( 5, -5,  5), new (0, 1, 0)),
            new (new ( 5, -5, -5), new (0, 1, 0))
        };

        var indices = new uint[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        };

        _mesh = new (vertices, indices);
    }
    
    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }
}