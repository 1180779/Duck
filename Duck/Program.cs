using System.Numerics;
using Duck.Entities;
using Duck.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Duck;

internal class Program
{
    private static IWindow s_window = null!;
    private static readonly List<Entity> GameObjects = [];
    private static IKeyboard s_keyboard;
    private static IMouse s_mouse;
    private static Camera s_camera;

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        GlfwWindowing.Use();
        options.API = GraphicsAPI.None;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "GK2PUMA";

        s_window = Window.Create(options);

        s_window.Load += OnLoad;
        s_window.Update += OnUpdate;
        s_window.Render += OnRender;
        s_window.Closing += OnClosing;

        s_window.Run();
    }

    private static void OnLoad()
    {
        var hwnd = s_window.Native.Win32.Value.Hwnd;
        uint width = (uint)s_window.Size.X;
        uint height = (uint)s_window.Size.Y;

        var swapChainDesc = new SwapChainDescription()
        {
            BufferCount = 2,
            BufferDescription = new ModeDescription(width, height, Format.R8G8B8A8_UNorm),
            Windowed = true,
            OutputWindow = hwnd,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            BufferUsage = Usage.RenderTargetOutput
        };

        D3D11.D3D11CreateDeviceAndSwapChain(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            new[] { FeatureLevel.Level_11_0 },
            swapChainDesc,
            out var swapChain,
            out var device,
            out _,
            out var context);

        if (device == null || context == null || swapChain == null)
        {
            throw new Exception("Failed to create device.");
        }
        
        GI.Instance.Device = device;
        GI.Instance.Context = context;
        GI.Instance.SwapChain = swapChain;

        GI.Instance.Resize(width, height);
        s_window.Resize += (size) =>
        {
            GI.Instance.Resize((uint)size.X, (uint)size.Y);
        };

        GI.Instance.Pipeline.Init();

        var positionNormalInputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        var unlitShader = new Shader($"{ShaderManager.BasePath}unlitVS.hlsl", $"{ShaderManager.BasePath}unlitPS.hlsl",
            positionNormalInputElements);

        var phongShader = new Shader($"{ShaderManager.BasePath}blinnPhongVS.hlsl",
            $"{ShaderManager.BasePath}blinnPhongPS.hlsl", positionNormalInputElements);

        var gpassShader = new Shader($"{ShaderManager.BasePath}gPassVS.hlsl",
            $"{ShaderManager.BasePath}gPassPS.hlsl", positionNormalInputElements);

        var lightPassShader = new Shader($"{ShaderManager.BasePath}lightPassVS.hlsl",
            $"{ShaderManager.BasePath}lightPassPS.hlsl", []);

        var ambientPassShader = new Shader($"{ShaderManager.BasePath}lightPassVS.hlsl",
           $"{ShaderManager.BasePath}ambientPassPS.hlsl", []);

        var shadowVolumeShader = new Shader(
            $"{ShaderManager.BasePath}shadowVolumeVS.hlsl",
            $"{ShaderManager.BasePath}unlitPS.hlsl",
            positionNormalInputElements,
            $"{ShaderManager.BasePath}shadowVolumeGS.hlsl"
        );

        var particleInputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),

            new InputElementDescription("INSTANCE_CURRPOS", 0, Format.R32G32B32_Float, 0,  1, InputClassification.PerInstanceData, 1),
            new InputElementDescription("INSTANCE_AGE",     0, Format.R32_Float,       12, 1, InputClassification.PerInstanceData, 1),
            new InputElementDescription("INSTANCE_PREVPOS", 0, Format.R32G32B32_Float, 16, 1, InputClassification.PerInstanceData, 1),
            new InputElementDescription("INSTANCE_MAXAGE",  0, Format.R32_Float,       28, 1, InputClassification.PerInstanceData, 1),
            new InputElementDescription("INSTANCE_TEXTURE",  0, Format.R32_Float,       32, 1, InputClassification.PerInstanceData, 1)
        };

        var particleShader = new Shader(
           $"{ShaderManager.BasePath}particleVS.hlsl",
           $"{ShaderManager.BasePath}particlePS.hlsl",
           particleInputElements
       );

        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Unlit, unlitShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.BlinnPhong, phongShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.GPass, gpassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.LightPass, lightPassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.ShadowVolume, shadowVolumeShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.AmbientPass, ambientPassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Particle, particleShader);

        var input = s_window.CreateInput();
        s_keyboard = input.Keyboards[0];
        s_mouse = input.Mice[0];

        s_camera = new Camera((float)width / height);

        var myQuad = new ReflectiveQuad {
            Transform =
            {
                Position = new Vector3(0, -InsideCube.HalfSize + 1f, 2.5f),
                Rotation = new Vector3(30.0f * MathF.PI / 180, 0.0f, 0),
                Scale = 1.0f
            },
        };
        GameObjects.Add(myQuad);

        var pointLight = new PointLight(
            position: new Vector3(-3.0f, -2.5f, 1.5f),
            color: new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        );
        GI.Instance.LightManager.Add(pointLight.Position, pointLight.Color);
        GI.Instance.LightManager.Update();
        GameObjects.Add(pointLight);

        GameObjects.Add(s_camera);
        GameObjects.Add(new InsideCube());
    }

    private static void OnUpdate(double deltaTime)
    {
        float dt = (float)deltaTime;

        foreach (var obj in GameObjects)
        {
            obj.HandleInput(s_keyboard, s_mouse, dt);
        }

        foreach (var obj in GameObjects)
        {
            obj.Update(dt);
        }
    }

    private static void OnRender(double deltaTime)
    {
        s_camera.UpdateAndBindViewProjBuffer();

        foreach (var obj in GameObjects)
        {
            obj.Render(s_camera);
        }

        GI.Instance.Pipeline.Execute(s_camera);

        GI.Instance.SwapChain.Present(1, PresentFlags.None);
    }

    private static void OnClosing()
    {
        foreach (var obj in GameObjects)
        {
            (obj as IDisposable)?.Dispose();
        }

        GI.Instance.LightManager.Dispose();
        GI.Instance.ShaderManager.DisposeAll();
        GI.Instance.RenderTargetView?.Dispose();
        GI.Instance.SwapChain?.Dispose();
        GI.Instance.Context?.Dispose();
        GI.Instance.Device?.Dispose();
    }
}