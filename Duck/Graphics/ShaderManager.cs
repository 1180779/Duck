namespace Duck.Graphics;

public class ShaderManager
{
    public const string BasePath = "Duck.Shaders.";
    public enum ShaderType
    {
        Unlit,
        BlinnPhong,
        GPass,
        LightPass,
        AmbientPass,
        ShadowVolume,
        Particle
    }
    
    private readonly Dictionary<ShaderType, Shader> _shaders = new();

    public Shader GetShader(ShaderType shaderType)
    {
        if (_shaders.TryGetValue(shaderType, out var shader))
        {
            return shader;
        }

        throw new Exception($"Shader not found");
    }

    public void AddShader(ShaderType shaderType, Shader shader)
    {
        _shaders.Add(shaderType, shader);
    }

    public void DisposeAll()
    {
        foreach (var shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _shaders.Clear();
    }
}