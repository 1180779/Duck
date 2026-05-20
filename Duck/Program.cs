using System.Diagnostics;
using System.Numerics;

using Duck.Entities;
using Duck.Graphics;

using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Duck;

internal static class Program
{
    private static readonly List<Entity> GameObjects = [];
    private static Camera s_camera = null!;
    private static IKeyboard s_keyboard = null!;
    private static IMouse s_mouse = null!;
    private static IWindow s_window = null!;

    private static void Main(string[] _)
    {
        WindowOptions options = WindowOptions.Default;
        GlfwWindowing.Use();
        options.API = GraphicsAPI.None;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Duck";

        s_window = Window.Create(options);

        s_window.Load += OnLoad;
        s_window.Update += OnUpdate;
        s_window.Render += OnRender;
        s_window.Closing += OnClosing;

        s_window.Run();
    }

    private static void OnClosing()
    {
        foreach (Entity obj in GameObjects)
        {
            (obj as IDisposable)?.Dispose();
        }

        GI.Instance.Dispose();
    }

    private static void OnLoad()
    {
        INativeWindow? native = s_window.Native;
        if (native is null)
        {
            throw new PlatformNotSupportedException();
        }

        (IntPtr Hwnd, IntPtr HDC, IntPtr HInstance)? win32 = native.Win32;
        if (win32 is null)
        {
            throw new PlatformNotSupportedException();
        }

        IInputContext input = s_window.CreateInput();
        s_keyboard = input.Keyboards[0];
        s_mouse = input.Mice[0];

        IntPtr hwnd = win32.Value.Hwnd;
        uint width = (uint)s_window.Size.X;
        uint height = (uint)s_window.Size.Y;

        GI.CreateInstance(hwnd, width, height);
        s_camera = new Camera((float)width / height);
        s_camera.Position = new Vector3(15.0f);
        s_camera.LookAt(new Vector3());

        GI.Instance.Resize(width, height);
        s_window.Resize += size =>
        {
            GI.Instance.Resize((uint)size.X, (uint)size.Y);
        };

        InputElementDescription[] positionInputElements =
        [
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0)
        ];
        InputElementDescription[] positionNormalUvInputElements =
        [
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
        ];

        Shader unlitShader = new($"{GI.ShadersEmbResPath}unlitVS.hlsl", $"{GI.ShadersEmbResPath}unlitPS.hlsl",
            positionNormalUvInputElements
        );
        Shader gpassShader = new($"{GI.ShadersEmbResPath}gPassVS.hlsl",
            $"{GI.ShadersEmbResPath}gPassPS.hlsl", positionNormalUvInputElements
        );
        Shader lightPassShader = new($"{GI.ShadersEmbResPath}lightPassVS.hlsl",
            $"{GI.ShadersEmbResPath}lightPassPS.hlsl", []
        );
        Shader ambientPassShader = new($"{GI.ShadersEmbResPath}lightPassVS.hlsl",
            $"{GI.ShadersEmbResPath}ambientPassPS.hlsl", []
        );
        Shader envShader = new($"{GI.ShadersEmbResPath}envVS.hlsl", $"{GI.ShadersEmbResPath}envPS.hlsl",
            positionInputElements
        );
        Shader waterShader = new($"{GI.ShadersEmbResPath}waterVS.hlsl", $"{GI.ShadersEmbResPath}waterPS.hlsl",
            positionNormalUvInputElements
        );

        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Unlit, unlitShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.GPass, gpassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.LightPass, lightPassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.AmbientPass, ambientPassShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Env, envShader);
        GI.Instance.ShaderManager.AddShader(ShaderManager.ShaderType.Water, waterShader);

        using Stream waterStream = Resources.GetResourceStream($"{GI.TextureEmbResPath}scuba_suede_diff_4k.jpg");
        ID3D11ShaderResourceView waterTexture =
            GI.Instance.LoadTextureFromStream(
                waterStream
            );
        WaterQuad myQuad = new()
        {
            Transform =
            {
                Position = new Vector3(0, 0, 0), Rotation = new Vector3(MathF.PI / 2, 0, 0), Scale = 10.0f
            },
            Texture = waterTexture,
            Color = new Vector4(1.0f)
        };
        GameObjects.Add(myQuad);

        using Stream duckStream = Resources.GetResourceStream($"{GI.TextureEmbResPath}ducktex.jpg");
        ID3D11ShaderResourceView duckTexture =
            GI.Instance.LoadTextureFromStream(
                duckStream
            );
        Entities.Duck duck = new(new Vector3()) { Texture = duckTexture };
        GameObjects.Add(duck);

        PointLight pointLight = new(
            new Vector3(0.0f, 2.5f, 0.0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        );
        if (!GI.Instance.LightManager.Add(pointLight.Position, pointLight.Color))
        {
            throw new UnreachableException("Point light was not added");
        }

        GI.Instance.LightManager.Update();
        GameObjects.Add(pointLight);

        GameObjects.Add(s_camera);

        using Stream stream = Resources.GetResourceStream($"{GI.TextureEmbResPath}charolettenbrunn_park_4k.dds");
        ID3D11ShaderResourceView cubeTexture =
            GI.Instance.LoadCubeTextureFromStream(
                stream
            );
        GI.Instance.Pipeline.EnvCubeTexture = cubeTexture;
        GameObjects.Add(new InsideCube { CubeTexture = cubeTexture }
        );
    }

    private static void OnRender(double deltaTime)
    {
        s_camera.UpdateAndBindViewProjBuffer();

        foreach (Entity obj in GameObjects)
        {
            obj.Render(s_camera);
        }

        GI.Instance.Pipeline.Execute(s_camera);

        GI.Instance.SwapChain.Present(1, PresentFlags.None);
    }

    private static void OnUpdate(double deltaTime)
    {
        float dt = (float)deltaTime;

        foreach (Entity obj in GameObjects)
        {
            obj.HandleInput(s_keyboard, s_mouse, dt);
        }

        foreach (Entity obj in GameObjects)
        {
            obj.Update(dt);
        }
    }
}