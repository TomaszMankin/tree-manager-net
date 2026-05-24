using System.Reflection;

namespace TreeManager.Common.TestUtilities;

public static class FixtureLoader
{
    public static string LoadMeFixture(bool withBom = false)
    {
        var name = withBom
            ? "TreeManager.Common.TestUtilities.Fixtures.me-fixture-bom.json"
            : "TreeManager.Common.TestUtilities.Fixtures.me-fixture.json";
        var assembly = typeof(FixtureLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Fixture '{name}' not found. Available: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
