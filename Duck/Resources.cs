using System.Diagnostics;
using System.Reflection;

namespace Duck;

public static class Resources
{
    public static Stream GetFileStream(string resourcePath)
    {
        FileStream fileStream = File.OpenRead(resourcePath);
        return fileStream;
    }

    public static Stream GetResourceStream(string resourcePath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourcePath) ?? throw new UnreachableException();
    }

    public static string ReadResource(string resourcePath)
    {
        using Stream stream = GetResourceStream(resourcePath);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}