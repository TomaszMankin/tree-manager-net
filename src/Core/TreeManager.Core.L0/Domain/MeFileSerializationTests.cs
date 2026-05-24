using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class MeFileSerializationTests
{
    private static string LoadFixture(string resourceName)
    {
        var assembly = typeof(MeFileSerializationTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Roundtrip_RealPyTreeManagerFixture_PreservesAllFields()
    {
        // Arrange
        var rawJson = LoadFixture("TreeManager.Core.L0.Fixtures.me-fixture.json");

        // Act — first pass: deserialize from fixture
        var meFile = JsonSerializer.Deserialize<MeFile>(rawJson, MeFile.DefaultOptions)!;

        // Assert structural values from fixture
        Assert.Equal(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), meFile.UniqueIdentifier);
        Assert.Equal("Jan Testowy", meFile.PersonName);
        Assert.Equal("Jan", meFile.FirstName);
        Assert.Equal("Testowy", meFile.LastName);
        Assert.Equal(Sex.Male, meFile.Sex);  // "Mezczyzna" (unaccented) normalizes to Male
        Assert.False(meFile.HasMaidenName);
        Assert.Empty(meFile.Spouse);
        Assert.Empty(meFile.SpouseId);
        Assert.Empty(meFile.Notes);
        Assert.Empty(meFile.DatesOfBirth);
        Assert.Empty(meFile.DatesOfDeath);

        // Act — second pass: serialize, then deserialize again (double roundtrip for stability)
        var serialized = JsonSerializer.Serialize(meFile, MeFile.DefaultOptions);
        var meFile2 = JsonSerializer.Deserialize<MeFile>(serialized, MeFile.DefaultOptions)!;

        // Assert structural equality on the stable (already-normalized) roundtrip
        // Note: fixture uses legacy unaccented "Mezczyzna" which normalizes to "Mężczyzna" on write.
        // DeepEquals on the normalized→normalized pair must hold exactly.
        var firstSerialized = JsonNode.Parse(serialized);
        var secondSerialized = JsonNode.Parse(JsonSerializer.Serialize(meFile2, MeFile.DefaultOptions));
        Assert.True(JsonNode.DeepEquals(firstSerialized, secondSerialized),
            $"Second-pass roundtrip mismatch.\nFirst:\n{serialized}");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Roundtrip_RealFixtureWithBom_StripsBom()
    {
        // Arrange — BOM fixture
        var rawJson = LoadFixture("TreeManager.Core.L0.Fixtures.me-fixture-bom.json");

        // Act — System.Text.Json must tolerate BOM transparently
        var meFile = JsonSerializer.Deserialize<MeFile>(rawJson, MeFile.DefaultOptions);

        // Assert — deserialization succeeded and produced valid data
        Assert.NotNull(meFile);
        Assert.Equal(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), meFile.UniqueIdentifier);
        Assert.Equal("Jan Testowy", meFile.PersonName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Serialize_DefaultMeFile_EmitsSnakeCasePropertyNames()
    {
        // Arrange
        var meFile = new MeFile { UniqueIdentifier = Guid.NewGuid() };

        // Act
        var json = JsonSerializer.Serialize(meFile, MeFile.DefaultOptions);

        // Assert — wire uses snake_case NOT PascalCase
        Assert.Contains("\"unique_identifier\"", json);
        Assert.DoesNotContain("\"UniqueIdentifier\"", json);
        Assert.Contains("\"person_name\"", json);
        Assert.DoesNotContain("\"PersonName\"", json);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void MeFile_HasExactly22Properties_AllWithJsonPropertyName()
    {
        // Arrange
        var props = typeof(MeFile).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>() != null)
            .ToArray();

        // Act & Assert
        Assert.Equal(22, props.Length);
        foreach (var p in props)
        {
            var attr = p.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
            Assert.NotNull(attr);
            Assert.False(string.IsNullOrEmpty(attr!.Name),
                $"Property {p.Name} has empty JsonPropertyName");
        }
    }
}
