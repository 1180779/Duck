using System.Numerics;

namespace Duck.Graphics;

public static class AdjacencyHelper
{
    public static uint[] Build(Vertex[] vertices, uint[] indices)
    {
        uint[] posMap = new uint[vertices.Length];
        var posDict = new Dictionary<Vector3, uint>();

        for (uint i = 0; i < vertices.Length; i++)
        {
            if (!posDict.TryGetValue(vertices[i].Position, out uint sharedIndex))
            {
                sharedIndex = i;
                posDict[vertices[i].Position] = sharedIndex;
            }

            posMap[i] = sharedIndex;
        }

        var edgeMap = new Dictionary<(uint, uint), uint>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            uint i0 = indices[i];
            uint i1 = indices[i + 1];
            uint i2 = indices[i + 2];

            edgeMap[(posMap[i0], posMap[i1])] = i2;
            edgeMap[(posMap[i1], posMap[i2])] = i0;
            edgeMap[(posMap[i2], posMap[i0])] = i1;
        }

        uint[] adjIndices = new uint[indices.Length * 2];
        for (int i = 0; i < indices.Length; i += 3)
        {
            uint i0 = indices[i];
            uint i1 = indices[i + 1];
            uint i2 = indices[i + 2];

            uint p0 = posMap[i0];
            uint p1 = posMap[i1];
            uint p2 = posMap[i2];

            uint adj01 = edgeMap.TryGetValue((p1, p0), out uint a01) ? a01 : i2;
            uint adj12 = edgeMap.TryGetValue((p2, p1), out uint a12) ? a12 : i0;
            uint adj20 = edgeMap.TryGetValue((p0, p2), out uint a20) ? a20 : i1;

            int adjIdx = i * 2;
            adjIndices[adjIdx] = i0;
            adjIndices[adjIdx + 1] = adj01;
            adjIndices[adjIdx + 2] = i1;
            adjIndices[adjIdx + 3] = adj12;
            adjIndices[adjIdx + 4] = i2;
            adjIndices[adjIdx + 5] = adj20;
        }

        return adjIndices;
    }
}