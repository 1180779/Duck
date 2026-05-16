using System.Numerics;

namespace Duck.Graphics;

public static class MeshGenerator
{
    public static Mesh CreateSphereMesh(uint precision)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        for (int i = 0; i <= precision; i++)
        {
            double lat = Math.PI * (-0.5 + (double)i / precision);
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);

            for (int j = 0; j <= precision; j++)
            {
                double lon = 2 * Math.PI * (j == precision ? 0 : j) / precision;
                float x = (float)(Math.Cos(lon) * cosLat);
                float y = (float)sinLat;
                float z = (float)(Math.Sin(lon) * cosLat);

                vertices.Add(new Vertex(new Vector3(x, y, z), new Vector3(x, y, z)));
            }
        }

        for (uint i = 0; i < precision; i++)
        {
            for (uint j = 0; j < precision; j++)
            {
                uint first = i * (precision + 1) + j;
                uint second = first + (precision + 1);

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public static Mesh CreateCylinderMesh(uint lidPrecision, uint widthPrecision)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        Vector2[] unitCircle = GenerateUnitCircleVertices();
        GenerateCylinderSideVertices();
        GenerateCylinderSideIndices();
        GenerateCylinderCapVerticesAndIndices();
        var verts = vertices.ToArray();
        var idx = indices.ToArray();
        return new Mesh(verts, idx);

        Vector2[] GenerateUnitCircleVertices()
        {
            var vector2S = new Vector2[lidPrecision + 1];
            float angleStep = 2 * MathF.PI / lidPrecision;
            for (int i = 0; i <= lidPrecision; i++)
            {
                float theta = i * angleStep;
                vector2S[i] = new Vector2(MathF.Cos(theta), MathF.Sin(theta));
            }

            return vector2S;
        }

        void GenerateCylinderSideVertices()
        {
            for (int i = 0; i <= widthPrecision; i++)
            {
                float h = -0.5f + (float)i / widthPrecision;
                for (int j = 0; j <= lidPrecision; j++)
                {
                    var position = new Vector3(unitCircle[j].X, unitCircle[j].Y, h);
                    var normal = Vector3.Normalize(new Vector3(unitCircle[j].X, unitCircle[j].Y, 0));
                    vertices.Add(new Vertex(position, normal));
                }
            }
        }

        void GenerateCylinderSideIndices()
        {
            for (uint i = 0; i < widthPrecision; i++)
            {
                uint k1 = i * (lidPrecision + 1);
                uint k2 = k1 + lidPrecision + 1;
                for (uint j = 0; j < lidPrecision; j++)
                {
                    indices.Add(k1 + j);
                    indices.Add(k1 + j + 1);
                    indices.Add(k2 + j);

                    indices.Add(k2 + j);
                    indices.Add(k1 + j + 1);
                    indices.Add(k2 + j + 1);
                }
            }
        }

        void GenerateCylinderCapVerticesAndIndices()
        {
            for (int cap = 0; cap < 2; cap++)
            {
                float h = -0.5f + cap;
                float nz = -1.0f + 2.0f * cap;

                uint centerIndex = (uint)vertices.Count;
                vertices.Add(new Vertex(new Vector3(0, 0, h), new Vector3(0, 0, nz)));
                for (int j = 0; j < lidPrecision; j++)
                {
                    vertices.Add(new Vertex(new Vector3(unitCircle[j].X, unitCircle[j].Y, h), new Vector3(0, 0, nz)));
                }

                uint ringStart = centerIndex + 1;
                for (uint i = 0; i < lidPrecision; i++)
                {
                    uint next = (i + 1) % lidPrecision;
                    indices.Add(centerIndex);
                    if (cap == 0) // bottom: normal faces -Z
                    {
                        indices.Add(ringStart + next);
                        indices.Add(ringStart + i);
                    }
                    else // top: normal faces +Z
                    {
                        indices.Add(ringStart + i);
                        indices.Add(ringStart + next);
                    }
                }
            }
        }
    }
}