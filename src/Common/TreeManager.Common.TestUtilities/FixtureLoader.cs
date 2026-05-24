using System.Reflection;

namespace TreeManager.Common.TestUtilities;

public static class FixtureLoader
{
    public static string Load(string fixtureName)
    {
        var name = $"TreeManager.Common.TestUtilities.Fixtures.{fixtureName}";
        var assembly = typeof(FixtureLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Fixture '{name}' not found. Available: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
