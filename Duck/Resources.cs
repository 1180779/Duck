using System.Reflection;

namespace Duck;

public static class Resources
{
    public static string ReadResource(string resourcePath)
    {
        using Stream stream = GetResourceStream(resourcePath);
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static Stream GetResourceStream(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourcePath);
    }
}