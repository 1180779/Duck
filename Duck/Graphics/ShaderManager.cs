namespace Duck.Graphics;

public sealed class ShaderManager : IDisposable
{
    private readonly Dictionary<ShaderType, Shader> _shaders = new();

    public void Dispose()
    {
        foreach (Shader shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _shaders.Clear();
    }

    public void AddShader(ShaderType shaderType, Shader shader)
    {
        _shaders.Add(shaderType, shader);
    }

    public Shader GetShader(ShaderType shaderType)
    {
        return _shaders.TryGetValue(shaderType, out Shader? shader) ? shader : throw new Exception("Shader not found");
    }

    public enum ShaderType
    {
        Unlit,
        GPass,
        LightPass,
        AmbientPass,
    }
}