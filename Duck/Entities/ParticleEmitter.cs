using System.Numerics;
using System.Runtime.InteropServices;
using Duck.Graphics;
using Vortice.Direct3D11;

namespace Duck.Entities;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleInstance
{
    public Vector3 CurrentPos;
    public float Age;
    public Vector3 PreviousPos;
    public float MaxAge;
    public float Texture;
}

public class Particle
{
    public Vector3 Position;
    public Vector3 PreviousPosition;
    public Vector3 Velocity;
    public float Age;
    public float MaxAge;
    public float Texture;
    public bool IsAlive => Age < MaxAge;
}

public class ParticleEmitter : Entity, IDisposable
{
    private readonly List<Particle> _particles = new();
    private readonly Random _rand = new();

    private Mesh _quadMesh;
    private ID3D11Buffer _instanceBuffer;
    private ID3D11ShaderResourceView[] _particleTextures;

    private const int MaxParticles = 500;
    private const float ParticlesPerSecond = 100;
    private const float ParticleMaxAgeInSeconds = 0.4f;
    private float _particlesToCreate;

    public bool Animating;
    public Vertex PositonNormal;

    private bool _disposing = false;
    public ParticleEmitter()
    {
        var device = GI.Instance.Device;

        var quadVertices = new VertexPosition[]
        {
            new VertexPosition(new Vector3(0f, 0f, 0f)),
            new VertexPosition(new Vector3(0f, 1f, 0f)),
            new VertexPosition(new Vector3(1f, 1f, 0f)),
            new VertexPosition(new Vector3(1f, 0f, 0f)),
        };
        var quadIndices = new uint[] { 0, 1, 2, 0, 2, 3 };
        _quadMesh = new Mesh(quadVertices, quadIndices);

        int instanceSize = Marshal.SizeOf<ParticleInstance>();
        var bufferDesc = new BufferDescription
        {
            Usage = ResourceUsage.Dynamic, 
            ByteWidth = (uint)instanceSize * MaxParticles,
            BindFlags = BindFlags.VertexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.None
        };
        _instanceBuffer = device.CreateBuffer(bufferDesc);

        _particleTextures = new ID3D11ShaderResourceView[2];

        using Stream textureStream = Resources.GetResourceStream($"{GI.TextureBasePath}rain3.png");
        _particleTextures[0] = GI.Instance.LoadTextureFromStream(textureStream);
        using Stream textureStream2 = Resources.GetResourceStream($"{GI.TextureBasePath}rain.png");
        _particleTextures[1] = GI.Instance.LoadTextureFromStream(textureStream2);
    }

    public void SetEmitterPositionAndNormal(Vertex positionNormal)
    {
        PositonNormal = positionNormal;
    }

    public override void Update(float dt)
    {
        if (!Animating)
        {
            return;
        }

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.PreviousPosition = particle.Position;
            particle.Velocity += new Vector3(0, -9.81f*0.4f, 0) * dt;
            particle.Position += particle.Velocity * dt;
            particle.Age += dt;

            if (!particle.IsAlive)
            {
                _particles.RemoveAt(i);
            }
        }

        _particlesToCreate += dt * ParticlesPerSecond;

        while (_particlesToCreate > 1.0f)
        {
            _particlesToCreate -= 1.0f;
            if (_particles.Count + 1 >= MaxParticles)
            {
                continue;
            }

            Vector3 randomOffset = new Vector3(
                (float)_rand.NextDouble() - 0.5f,
                (float)_rand.NextDouble() - 0.5f,
                (float)_rand.NextDouble() - 0.5f
            );

            Vector3 startVelocity = Vector3.Normalize(PositonNormal.Normal + randomOffset*1.5f) * (2.5f + (float)_rand.NextDouble() * 1f);

            _particles.Add(new Particle
            {
                Position = PositonNormal.Position,
                PreviousPosition = PositonNormal.Position,
                Velocity = startVelocity,
                Age = 0.0f,
                MaxAge = ParticleMaxAgeInSeconds + (float)_rand.NextDouble(),
                Texture = (float)_rand.Next(2)
            });
        }
    }

    public ParticleInstance[] GetInstanceData()
    {
        var data = new ParticleInstance[_particles.Count];
        for (int i = 0; i < _particles.Count; i++)
        {
            data[i] = new ParticleInstance
            {
                CurrentPos = _particles[i].Position,
                PreviousPos = _particles[i].PreviousPosition,
                Age = _particles[i].Age,
                MaxAge = _particles[i].MaxAge,
                Texture = _particles[i].Texture,
            };
        }

        return data;
    }

    public override void Render(Camera camera)
    {
        if (_particles.Count == 0)
        {
            return;
        }

        var context = GI.Instance.Context;
        var instanceData = GetInstanceData();

        unsafe
        {
            var mappedResource = context.Map(_instanceBuffer, 0, MapMode.WriteDiscard);
            fixed (void* ptr = instanceData)
            {
                int bytesToCopy = _particles.Count * Marshal.SizeOf<ParticleInstance>();
                System.Buffer.MemoryCopy(ptr, mappedResource.DataPointer.ToPointer(), mappedResource.RowPitch, bytesToCopy);
            }

            context.Unmap(_instanceBuffer, 0);
        }

        GI.Instance.Pipeline.SubmitParticle(_quadMesh, _instanceBuffer, _particles.Count, _particleTextures);
    }

    public void Dispose()
    {
        if (!_disposing)
        {
            _quadMesh.Dispose();
            _instanceBuffer.Dispose();
            foreach (var texture in _particleTextures)
            {
                texture.Dispose();
            }

            _disposing = true;
        }
    }
}