using System.Numerics;

using Duck.Graphics;

namespace Duck.Entities;

public sealed class PointLight : Entity
#if DEBUG
    , IDisposable
#endif
{
    public Vector3 Position
    {
        get;
    }

    public Vector4 Color
    {
        get;
    }

#if DEBUG
    public const int SphereMeshPrecision = 16;

    private readonly Mesh _mesh;
    private readonly ConstantBuffer<ConstantBufferModel> _constantBufferModel;
    private readonly ConstantBuffer<ConstantBufferSurfaceColor> _constantBufferSurfaceColor;
#endif

    public PointLight(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
#if DEBUG
        _mesh = MeshGenerator.CreateSphereMesh(SphereMeshPrecision);
        Transform transform = new() { Position = position, Scale = 0.05f };
        _constantBufferModel = new ConstantBuffer<ConstantBufferModel>();
        _constantBufferModel.Update(new ConstantBufferModel
        {
            Model = transform.ModelMatrix, 
            ModelInv = transform.InvModelMatrix,
        });

        _constantBufferSurfaceColor = new ConstantBuffer<ConstantBufferSurfaceColor>();
        _constantBufferSurfaceColor.Update(new ConstantBufferSurfaceColor { SurfaceColor = Color });
#endif
    }

#if DEBUG
    public override void Render(Camera camera)
    {
        Transform transform = new()
        {
            Position = Position,
            Scale = 0.05f
        };
        GI.Instance.Pipeline.SubmitOpaque(_mesh, transform.ModelMatrix, transform.InvModelMatrix, Color);
    }

    public void Dispose()
    {
        _mesh.Dispose();
        _constantBufferModel.Dispose();
        _constantBufferSurfaceColor.Dispose();
    }
#endif
}