using System.Numerics;

using Duck.Graphics;

using Silk.NET.Input;

namespace Duck.Entities;

public class Camera : Entity, IDisposable
{
    private readonly ConstantBuffer<ConstantBufferViewProj> _viewProjBuffer = new();

    public Vector3 Position;
    public float Pitch;
    public float Yaw;

    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }

    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }

    private Vector2 _lastMousePosition;
    private bool _isFirstMove = true;
    private readonly float _speed = 10.0f;
    private readonly float _sensitivity = 0.002f;

    private float _aspectRatio;
    private readonly float _fovY;
    private readonly float _nearPlane;
    private readonly float _farPlane;
    private bool _projectionDirty = true;

    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (Math.Abs(value - AspectRatio) > 0.001f)
            {
                _aspectRatio = value;
                _projectionDirty = true;
            }
        }
    }

    public Camera(float aspectRatio, float fovY = MathF.PI / 4f, float nearPlane = 0.1f, float farPlane = 1000f)
    {
        Position = new Vector3(0, 0, -5);
        Yaw = MathF.PI / 2f;
        Up = Vector3.UnitY;

        _aspectRatio = aspectRatio;
        _fovY = fovY;
        _nearPlane = nearPlane;
        _farPlane = farPlane;

        UpdateVectors();
    }

    public override void HandleInput(IKeyboard keyboard, IMouse mouse, float dt)
    {
        if (keyboard.IsKeyPressed(Key.W))
        {
            Position += Forward * _speed * dt;
        }

        if (keyboard.IsKeyPressed(Key.S))
        {
            Position -= Forward * _speed * dt;
        }

        if (keyboard.IsKeyPressed(Key.D))
        {
            Position += Right * _speed * dt;
        }

        if (keyboard.IsKeyPressed(Key.A))
        {
            Position -= Right * _speed * dt;
        }

        if (keyboard.IsKeyPressed(Key.E))
        {
            Position += Vector3.UnitY * _speed * dt;
        }

        if (keyboard.IsKeyPressed(Key.Q))
        {
            Position -= Vector3.UnitY * _speed * dt;
        }

        if (mouse.IsButtonPressed(MouseButton.Right))
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;

            if (_isFirstMove)
            {
                _lastMousePosition = mouse.Position;
                _isFirstMove = false;
            }

            Vector2 delta = mouse.Position - _lastMousePosition;
            _lastMousePosition = mouse.Position;

            Yaw -= delta.X * _sensitivity;
            Pitch -= delta.Y * _sensitivity;

            Pitch = Math.Clamp(Pitch, -MathF.PI / 2.0f + 0.01f, MathF.PI / 2.0f - 0.01f);
        }
        else
        {
            mouse.Cursor.CursorMode = CursorMode.Normal;
            _isFirstMove = true;
        }
    }

    public override void Update(float dt)
    {
        float currentAspect = (float)GI.Instance.Width / GI.Instance.Height;
        if (Math.Abs(currentAspect - AspectRatio) > 0.001f)
        {
            AspectRatio = currentAspect;
        }

        UpdateVectors();
        ViewMatrix = Matrix4x4.CreateLookAtLeftHanded(Position, Position + Forward, Up);

        if (_projectionDirty)
        {
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(_fovY, _aspectRatio, _nearPlane, _farPlane);
            _projectionDirty = false;
        }
    }

    public void UpdateAndBindViewProjBuffer(Vector3 pos)
    {
        _viewProjBuffer.Update(new ConstantBufferViewProj
        {
            View = ViewMatrix,
            Projection = ProjectionMatrix,
            CamPos = pos
        });
        _viewProjBuffer.Bind(1);
    }
    
    public void UpdateAndBindViewProjBuffer()
    {
        _viewProjBuffer.Update(new ConstantBufferViewProj
        {
            View = ViewMatrix,
            Projection = ProjectionMatrix,
            CamPos = Position
        });
        _viewProjBuffer.Bind(1);
    }

    public void UpdateAsMirror(Camera mainCamera, Matrix4x4 mirrorTransform)
    {
        Vector3 mirrorPos = new(mirrorTransform.M41, mirrorTransform.M42, mirrorTransform.M43);
        Vector3 mirrorNormal = Vector3.Normalize(new Vector3(-mirrorTransform.M31, -mirrorTransform.M32, -mirrorTransform.M33));

        float d = -Vector3.Dot(mirrorNormal, mirrorPos);
        Plane mirrorPlane = new(mirrorNormal, d);

        Matrix4x4 reflectionMatrix = Matrix4x4.CreateReflection(mirrorPlane);
        ViewMatrix = Matrix4x4.Multiply(reflectionMatrix, mainCamera.ViewMatrix);
        Position = Vector3.Transform(mainCamera.Position, reflectionMatrix);
        Forward = Vector3.TransformNormal(mainCamera.Forward, reflectionMatrix);
        Up = Vector3.TransformNormal(mainCamera.Up, reflectionMatrix);
        Right = Vector3.TransformNormal(mainCamera.Right, reflectionMatrix);
        AspectRatio = mainCamera.AspectRatio;
        
        if (_projectionDirty)
        {
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(_fovY, _aspectRatio, _nearPlane, _farPlane);
            _projectionDirty = false;
        }
    }

    public void LookAt(Vector3 target)
    {
        Vector3 dir = Vector3.Normalize(target - Position);
        Pitch = MathF.Asin(dir.Y);
        Yaw = MathF.Atan2(dir.Z, dir.X);
    }

    public void Dispose() => _viewProjBuffer.Dispose();

    private void UpdateVectors()
    {
        float cosPitch = MathF.Cos(Pitch);
        float sinPitch = MathF.Sin(Pitch);
        float cosYaw = MathF.Cos(Yaw);
        float sinYaw = MathF.Sin(Yaw);

        Forward = Vector3.Normalize(new Vector3(cosYaw * cosPitch, sinPitch, sinYaw * cosPitch));
        Right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Forward));
        Up = Vector3.Normalize(Vector3.Cross(Forward, Right));
    }
}