using System.Globalization;
using System.Numerics;

using Duck.Graphics;

namespace Duck.Entities.PumaParser;

public sealed class PumaPart
{
    public readonly List<Vector3> VertexPositions = [];
    public readonly List<int> VertexPosIndices = [];
    public readonly List<Vector3> VertexNormals = [];
    public readonly List<Triangle> Triangles = [];
    public readonly List<Edge> Edges = [];

    public void Load(string path)
    {
        using var reader = new StreamReader(path);

        int posCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < posCount; i++)
        {
            VertexPositions.Add(ParseVector3(reader.ReadLine()!));
        }

        int vertCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < vertCount; i++)
        {
            var parts = reader.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                throw new FormatException($"Vertex {i}: expected 4 values, got {parts.Length}.");
            }

            VertexPosIndices.Add(int.Parse(parts[0]));
            VertexNormals.Add(new Vector3(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture)
            ));
        }

        int triCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < triCount; i++)
        {
            Triangles.Add(Triangle.Parse(reader.ReadLine()!));
        }

        int edgeCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < edgeCount; i++)
        {
            Edges.Add(Edge.Parse(reader.ReadLine()!));
        }
    }

    private static Vector3 ParseVector3(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Position: expected 3 values, got {parts.Length}.");
        }

        return new Vector3(
            float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture)
        );
    }

    public Mesh BuildMesh(Vector3 pivotPoint = default)
    {
        var vertices = new Vertex[VertexPosIndices.Count];
        for (int i = 0; i < VertexPosIndices.Count; i++)
        {
            vertices[i] = new Vertex(VertexPositions[VertexPosIndices[i]] - pivotPoint, VertexNormals[i]);
        }

        var indices = new uint[Triangles.Count * 3];
        for (int t = 0; t < Triangles.Count; t++)
        {
            indices[t * 3 + 0] = (uint)Triangles[t].VertexIdxIdx1;
            indices[t * 3 + 1] = (uint)Triangles[t].VertexIdxIdx2;
            indices[t * 3 + 2] = (uint)Triangles[t].VertexIdxIdx3;
        }

        return new Mesh(vertices, indices);
    }
}