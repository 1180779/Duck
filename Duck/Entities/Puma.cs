// #define DANCE

using System.Numerics;

using Duck.Entities.PumaParser;
using Duck.Graphics;

using Silk.NET.Input;

namespace Duck.Entities;

public sealed class Puma : Entity, IDisposable
{
    public const int PartCount = 6;
    public const float RotationSpeed = 0.01f;

    private static readonly Vector3[] PivotPoints =
    [
        new(0.0f, 0.0f, 0.0f),
        new(0.0f, 0.27f, 0.0f),
        new(0.0f, 0.27f, 0.0f),
        new(-0.91f, 0.27f, -0.26f),
        new(-2.05f, 0.27f, -0.26f),
        new(-1.72f, 0.27f, -0.26f)
    ];

    private static bool s_wasCDown;

    public const float ThetaStep = MathF.PI / 2;
    public readonly Transform[] Transforms = new Transform[PartCount];
    public const float Radius = 0.2f;

    private readonly Mesh[] _meshes = new Mesh[PartCount];
    private bool _animating;
    private float _theta;

    public Puma(string meshFolder = "Meshes", Quad? sheet = null)
    {
        Sheet = sheet;
        for (int i = 0; i < PartCount; i++)
        {
            PumaPart part = new();
            part.Load(Path.Combine(meshFolder, $"mesh{i + 1}.txt"));
            _meshes[i] = part.BuildMesh(PivotPoints[i]);

            Transforms[i] = new Transform();
            if (i > 0)
            {
                Transforms[i].Position = PivotPoints[i] - PivotPoints[i - 1];
            }
        }
    }

    public Vector4 Color
    {
        get;
    } = new(0.7f, 0.5f, 0.3f, 1.0f);

    public Quad? Sheet
    {
        get;
        set;
    }

    public Transform Transform => Transforms[0];

    public void Dispose()
    {
        for (int i = 0; i < PartCount; i++)
        {
            _meshes[i].Dispose();
        }
    }

    public override void HandleInput(IKeyboard keyboard, IMouse mouse, float dt)
    {
        if (keyboard.IsKeyPressed(Key.R))
        {
            Transforms[1].Rotation += new Vector3(0.0f, RotationSpeed, 0.0f);
        }

        if (keyboard.IsKeyPressed(Key.T))
        {
            Transforms[2].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        if (keyboard.IsKeyPressed(Key.Y))
        {
            Transforms[3].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        if (keyboard.IsKeyPressed(Key.U))
        {
            Transforms[4].Rotation += new Vector3(RotationSpeed, 0.0f, 0.0f);
        }

        if (keyboard.IsKeyPressed(Key.I))
        {
            Transforms[5].Rotation += new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        if (keyboard.IsKeyPressed(Key.F))
        {
            Transforms[1].Rotation -= new Vector3(0.0f, RotationSpeed, 0.0f);
        }

        if (keyboard.IsKeyPressed(Key.G))
        {
            Transforms[2].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        if (keyboard.IsKeyPressed(Key.H))
        {
            Transforms[3].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        if (keyboard.IsKeyPressed(Key.J))
        {
            Transforms[4].Rotation -= new Vector3(RotationSpeed, 0.0f, 0.0f);
        }

        if (keyboard.IsKeyPressed(Key.K))
        {
            Transforms[5].Rotation -= new Vector3(0.0f, 0.0f, RotationSpeed);
        }

        // TODO: if we want to make this the better way then subscribe to KeyDown event from IKeyboard in the constructor
        if (keyboard.IsKeyPressed(Key.C))
        {
            if (!s_wasCDown)
            {
                _animating = !_animating;
            }

            s_wasCDown = true;
        }
        else
        {
            s_wasCDown = false;
        }
    }

    public override void Render(Camera camera)
    {
        Matrix4x4 chainedMatrix = Transforms[0].ModelMatrix;
        Matrix4x4 chainedInv = Transforms[0].InvModelMatrix;
        for (int i = 0; i < PartCount; i++)
        {
            if (i > 0)
            {
                chainedMatrix = Transforms[i].ModelMatrix * chainedMatrix;
                chainedInv = chainedInv * Transforms[i].InvModelMatrix;
            }

            GI.Instance.Pipeline.SubmitOpaque(_meshes[i], chainedMatrix, chainedInv, Color);
        }
    }

    public override void Update(float deltaTime)
    {
#if DANCE
        Transforms[1].Rotation += new Vector3(0.0f, 0.025f, 0.0f);
        Transforms[2].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
        Transforms[3].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
        Transforms[4].Rotation += new Vector3(0.025f, 0.0f, 0.0f);
        Transforms[5].Rotation += new Vector3(0.0f, 0.0f, 0.025f);
#endif
        if (_animating && Sheet is not null)
        {
            _theta += ThetaStep * deltaTime;
            if (_theta > 2 * MathF.PI)
            {
                while (_theta > 2 * MathF.PI)
                {
                    _theta -= 2 * MathF.PI;
                }
            }

            TrackSheetCircle(Radius, _theta, Sheet);
        }
    }

    private void ApplyIk(Vector3 tipPosition, Vector3 tipNormal)
    {
        Vector3 localPos = Vector3.Transform(tipPosition, Transforms[0].InvModelMatrix);
        Vector3 localNormal = Vector3.TransformNormal(tipNormal, Transforms[0].InvModelMatrix);
        Angles angles = SolveIk(localPos, localNormal);
        Transforms[1].Rotation = new Vector3(0.0f, angles.A1, 0.0f);
        Transforms[2].Rotation = new Vector3(0.0f, 0.0f, angles.A2);
        Transforms[3].Rotation = new Vector3(0.0f, 0.0f, angles.A3);
        Transforms[4].Rotation = new Vector3(angles.A4, 0.0f, 0.0f);
        Transforms[5].Rotation = new Vector3(0.0f, 0.0f, angles.A5);
    }

    private static Vertex SampleSheetCircle(float radius, float theta, Quad sheet)
    {
        Vector3 defaultUVector = new(1.0f, 0.0f, 0.0f);
        Vector3 defaultVVector = new(0.0f, 1.0f, 0.0f);
        Vector3 defaultPosition = new(0.0f, 0.0f, 0.0f);
        Vector3 defaultNormal = new(0.0f, 0.0f, -1.0f);

        Vector3 transformedUVector = Vector3.TransformNormal(defaultUVector, sheet.Transform.ModelMatrix);
        Vector3 transformedVVector = Vector3.TransformNormal(defaultVVector, sheet.Transform.ModelMatrix);
        Vector3 transformedPosition = Vector3.Transform(defaultPosition, sheet.Transform.ModelMatrix);
        Vector3 transformedNormal =
            Vector3.Transform(defaultNormal, Matrix4x4.Transpose(sheet.Transform.InvModelMatrix));

        Vector3 tipPosition = transformedPosition +
                              (radius * MathF.Cos(theta) * transformedUVector) +
                              (radius * MathF.Sin(theta) * transformedVVector);
        return new Vertex(tipPosition, transformedNormal);
    }

    private static Angles SolveIk(Vector3 tipPosition, Vector3 tipNormal)
    {
        Angles result = new();
        const float l1 = 0.91f;
        const float l2 = 0.81f;
        const float l3 = 0.33f;
        const float dy = 0.27f;
        const float dz = 0.26f;
        tipNormal = Vector3.Normalize(tipNormal);
        Vector3 pos1 = tipPosition + (tipNormal * l3);
        float e = MathF.Sqrt((pos1.Z * pos1.Z) + (pos1.X * pos1.X) - (dz * dz));

        result.A1 = MathF.Atan2(pos1.Z, -pos1.X) + MathF.Atan2(dz, e);
        Vector3 pos2 = new(e, pos1.Y - dy, 0.0f);
        result.A3 = -MathF.Acos(MathF.Min(1.0f,
                ((pos2.X * pos2.X) + (pos2.Y * pos2.Y) - (l1 * l1) - (l2 * l2)) / (2.0f * l1 * l2)
            )
        );
        float k = l1 + (l2 * MathF.Cos(result.A3));
        float l = l2 * MathF.Sin(result.A3);
        result.A2 = -MathF.Atan2(pos2.Y, MathF.Sqrt((pos2.X * pos2.X) + (pos2.Z * pos2.Z))) - MathF.Atan2(l, k);
        Vector3 normal1 = Vector3.Transform(tipNormal,
            Matrix4x4.CreateRotationY(-result.A1) * Matrix4x4.CreateRotationZ(-(result.A2 + result.A3))
        );
        result.A5 = MathF.Acos(normal1.X);
        result.A4 = MathF.Atan2(normal1.Z, normal1.Y);
        return result;
    }

    private void TrackSheetCircle(float radius, float theta, Quad sheet)
    {
        Vertex vertex = SampleSheetCircle(radius, theta, sheet);
        ApplyIk(vertex.Position, vertex.Normal);
    }

    private record Angles
    {
        public float A1;
        public float A2;
        public float A3;
        public float A4;
        public float A5;
    }
}