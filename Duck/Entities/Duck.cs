using System.Numerics;

using Duck.Graphics;

using Vortice.Direct3D11;

namespace Duck.Entities;

public sealed class Duck : Entity, IDisposable
{
    private const float WaterHalfExtent = 4.5f;
    private const float SplineSpeed = 0.08f;
    private const int ControlPointCount = 8;

    public readonly Transform Transform = new();
    private readonly Mesh _mesh;
    private readonly BSpline _path;
    private float _t;

    public Duck(Vector3 position, Vector4? color = null, Vector3 meshPivot = default)
    {
        DuckParser.DuckParser parser = new();
        parser.Load("duck.txt");
        _mesh = parser.BuildMesh(meshPivot);

        Transform.Scale = (float)(1.0 / 200.0);
        if (color.HasValue)
        {
            Color = color.Value;
        }

        _path = new BSpline(GenerateControlPoints(position), closed: true);
        Transform.Position = _path.Evaluate(0f);
    }

    private static Vector3[] GenerateControlPoints(Vector3 origin)
    {
        Random rng = Random.Shared;
        Vector3[] points = new Vector3[ControlPointCount];
        for (int i = 0; i < points.Length; i++)
        {
            float x = origin.X + (((rng.NextSingle() * 2f) - 1f) * WaterHalfExtent);
            float z = origin.Z + (((rng.NextSingle() * 2f) - 1f) * WaterHalfExtent);
            points[i] = new Vector3(x, origin.Y, z);
        }

        return points;
    }

    public WaterQuad? Water
    {
        get;
        set;
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

    public override void Update(float dt)
    {
        _t = (_t + (SplineSpeed * dt)) % 1f;
        Transform.Position = _path.Evaluate(_t);

        Vector3 tangent = _path.Tangent(_t);
        // Duck's local forward is -X
        Transform.Rotation = new Vector3(0f, MathF.Atan2(tangent.Z, -tangent.X), 0f);

        Water?.PerturbAt(Transform.Position);
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitOpaque(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color, Texture);
    }
}