using System.Numerics;
using Duck.Graphics;

namespace Duck.Entities;

public sealed class Cylinder : Entity, IDisposable
{
    public const int CylinderLidMeshPrecision = 32;
    public const int CylinderWidthMeshPrecision = 1;

    private readonly Mesh _mesh;
    public readonly Transform Transform = new();

    public Vector4 Color
    {
        get;
    }

    public Cylinder(Vector3 position, Vector4 color)
    {
        Color = color;
        _mesh = MeshGenerator.CreateCylinderMesh(CylinderLidMeshPrecision, CylinderWidthMeshPrecision);
        Transform.Position = position;
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color);
    }

    public void Dispose()
    {
        _mesh.Dispose();
    }
}