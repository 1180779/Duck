using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

using SharpGen.Runtime;

using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Duck.Entities;

public sealed class WaterQuad : Quad
{
    public WaterQuad()
    {
        Color = new Vector4(1.0f);
        
        H = 2.0f / (N - 1.0f);
        Dt = 1 / (float)N;
        A = C * C / H / H * Dt * Dt;
        B = 2 - (4 * A);
        Z = new float[N, N];
        ZOld = new float[N, N];
        D = new float[N, N];
        Width = N - 2;
        Height = N - 2;
        NormalTexture = CreateNormalTexture();

        InitDij();
        TestNormalsPerturbations();
    }

    public float A
    {
        get;
    }

    public float B
    {
        get;
    }

    public float C
    {
        get;
    } = 1.0f;

    public float Dt
    {
        get;
    }

    public float H
    {
        get;
    }

    public uint Height
    {
        get;
    }

    public uint N
    {
        get;
    } = 256;

    public ID3D11ShaderResourceView NormalTexture
    {
        get;
    }

    public uint Width
    {
        get;
    }

    private float[,] D
    {
        get;
    }

    private float[,] Z
    {
        get;
        set;
    }

    private float[,] ZOld
    {
        get;
        set;
    }

    public override void Render(Camera camera)
    {
        GI.Instance.Pipeline.SubmitWater(_mesh, Transform.ModelMatrix, Transform.InvModelMatrix, Color,
            normTexture: NormalTexture
        );
    }

    public override void Update(float dt)
    {
        UpdateHeights();
        UpdateNormalTexture();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ID3D11ShaderResourceView CreateNormalTexture()
    {
        Texture2DDescription texDesc = new()
        {
            Width = Width,
            Height = Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        byte[] blue = [128, 128, 255, 255];
        uint size = N - 2;
        byte[] texture = Enumerable.Repeat(blue, (int)(size * size)).SelectMany(arr => arr).ToArray();
        uint rowPitch = 4 * size;
        ID3D11ShaderResourceView normTexture;
        unsafe
        {
            fixed (byte* p = texture)
            {
                SubresourceData initData = new((nint)p, rowPitch);
                using ID3D11Texture2D tex = GI.Instance.Device.CreateTexture2D(texDesc, [initData]);
                normTexture = GI.Instance.Device.CreateShaderResourceView(tex);
            }
        }

        return normTexture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Dij(int i, int j)
    {
        float di = Transform.AxisScale.X / N * i;
        float dij = Transform.AxisScale.Y / N * j;
        return Dij(di, dij);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Dij(float di, float dj)
    {
        di = MathF.Min(Transform.AxisScale.X - di, di);
        dj = MathF.Min(Transform.AxisScale.Y - dj, dj);
        float l = MathF.Min(di, dj);
        return 0.95f * MathF.Min(1, l / 0.2f);
    }

    private void InitDij()
    {
        Debug.Assert(D.GetLength(0) == N && D.GetLength(1) == N);
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                D[i, j] = Dij(i, j);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte[] NormalsFromHeightMap()
    {
        Debug.Assert(Z.GetLength(0) == N && Z.GetLength(1) == N);

        uint size = N - 2;
        byte[] result = new byte[size * size * 4];
        float di = Transform.AxisScale.X / N;
        float dj = Transform.AxisScale.Y / N;
        for (int i = 1; i < N - 1; ++i)
        {
            for (int j = 1; j < N - 1; ++j)
            {
                float height = Z[i, j];
                float heightDiffX = Z[i - 1, j] - height;
                float heightDiffY = Z[i, j - 1] - height;
                Vector3 vx = new(di, 0.0f, heightDiffX);
                Vector3 vy = new(0.0f, dj, heightDiffY);
                Vector3 n = Vector3.Normalize(Vector3.Cross(vx, vy));

                int idx = (((i - 1) * (int)size) + (j - 1)) * 4;
                result[idx + 0] = (byte)(((n.X * 0.5f) + 0.5f) * 255.0f);
                result[idx + 1] = (byte)(((n.Y * 0.5f) + 0.5f) * 255.0f);
                result[idx + 2] = (byte)(((n.Z * 0.5f) + 0.5f) * 255.0f);
                result[idx + 3] = 255;
            }
        }

        return result;
    }

    private void TestNormalsPerturbations()
    {
        int cx = (int)N / 2, cy = (int)N / 2;
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                float dx = i - cx, dy = j - cy;
                Z[i, j] = MathF.Exp(-((dx * dx) + (dy * dy)) / 10.0f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateHeights()
    {
        Debug.Assert(ZOld.GetLength(0) == N && ZOld.GetLength(1) == N);
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                ZOld[i, j] = Znp1(i, j);
            }
        }

        (Z, ZOld) = (ZOld, Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateNormalTexture()
    {
        using ID3D11Texture2D texture = NormalTexture.Resource.QueryInterface<ID3D11Texture2D>();
        Result result = GI.Instance.Context.Map(texture, 0, MapMode.WriteDiscard, 0, out MappedSubresource mapped);
        if (result.Failure || mapped.DataPointer == 0)
        {
            throw new UnreachableException();
        }

        byte[] normals = NormalsFromHeightMap();
        unsafe
        {
            fixed (byte* p = normals)
            {
                uint rowBytes = (N - 2) * 4;
                for (int row = 0; row < N - 2; row++)
                {
                    Buffer.MemoryCopy(p + (row * rowBytes), (byte*)mapped.DataPointer + (row * mapped.RowPitch),
                        rowBytes, rowBytes
                    );
                }
            }
        }

        GI.Instance.Context.Unmap(texture, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Znp1(int i, int j)
    {
        Debug.Assert(i >= 0 && j >= 0 && i < N && j < N);
        Debug.Assert(D.GetLength(0) == N && D.GetLength(1) == N);
        Debug.Assert(Z.GetLength(0) == N && Z.GetLength(1) == N);
        Debug.Assert(ZOld.GetLength(0) == N && ZOld.GetLength(1) == N);

        float zij = Z[i, j];
        float zim1J = i > 0 ? Z[i - 1, j] : 0.0f;
        float zijm1 = j > 0 ? Z[i, j - 1] : 0.0f;
        float zip1J = i < N - 1 ? Z[i + 1, j] : 0.0f;
        float zijp1 = j < N - 1 ? Z[i, j + 1] : 0.0f;
        return D[i, j] * ((A * (zip1J + zim1J + zijm1 + zijp1)) + (B * zij) - ZOld[i, j]);
    }
}