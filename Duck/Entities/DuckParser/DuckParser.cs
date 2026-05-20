using System.Globalization;
using System.Numerics;

using Duck.Graphics;

namespace Duck.Entities.DuckParser;

public sealed class DuckParser
{
    public readonly List<Triangle> Triangles = [];
    public readonly List<Vector3> VertexNormals = [];
    public readonly List<Vector3> VertexPositions = [];
    public readonly List<Vector2> VertexUVs = [];

    public Mesh BuildMesh(Vector3 pivotPoint = default)
    {
        Vertex[] vertices = new Vertex[VertexPositions.Count];
        for (int i = 0; i < VertexPositions.Count; i++)
        {
            vertices[i] = new Vertex(VertexPositions[i] - pivotPoint, VertexNormals[i], VertexUVs[i]);
        }

        uint[] indices = new uint[Triangles.Count * 3];
        for (int t = 0; t < Triangles.Count; t++)
        {
            indices[(t * 3) + 0] = (uint)Triangles[t].VertexIdxIdx1;
            indices[(t * 3) + 1] = (uint)Triangles[t].VertexIdxIdx2;
            indices[(t * 3) + 2] = (uint)Triangles[t].VertexIdxIdx3;
        }

        return new Mesh(vertices, indices);
    }

    public void Load(string path)
    {
        using StreamReader reader = new(Resources.GetResourceStream($"{GI.MeshesEmbResPath}{path}"));

        int vertCount = int.Parse(reader.ReadLine()!.Trim());
        VertexPositions.Capacity = vertCount;
        VertexNormals.Capacity = vertCount;
        VertexUVs.Capacity = vertCount;
        for (int i = 0; i < vertCount; i++)
        {
            string[] parts = reader.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 8)
            {
                throw new FormatException($"Vertex {i}: expected 8 values, got {parts.Length}.");
            }

            VertexPositions.Add(new Vector3(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture)
                )
            );
            VertexNormals.Add(new Vector3(
                    float.Parse(parts[3], CultureInfo.InvariantCulture),
                    float.Parse(parts[4], CultureInfo.InvariantCulture),
                    float.Parse(parts[5], CultureInfo.InvariantCulture)
                )
            );
            VertexUVs.Add(new Vector2(
                    float.Parse(parts[6], CultureInfo.InvariantCulture),
                    float.Parse(parts[7], CultureInfo.InvariantCulture)
                )
            );
        }

        int triCount = int.Parse(reader.ReadLine()!.Trim());
        for (int i = 0; i < triCount; i++)
        {
            Triangles.Add(Triangle.Parse(reader.ReadLine()!));
        }
    }
}