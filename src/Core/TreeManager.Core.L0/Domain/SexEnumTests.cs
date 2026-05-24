using System.Text.Json;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class SexEnumTests
{
    private const string WireUnknown = "Nieznana";
    private const string WireMale = "Mężczyzna";
    private const string WireMaleUnaccented = "Mezczyzna";
    private const string WireFemale = "Kobieta";

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Serialize_Unknown_EmitsNieznana()
    {
        Assert.Equal(WireUnknown, Serialize(Sex.Unknown));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Serialize_Male_EmitsPolishWordMezczyzna()
    {
        Assert.Equal(WireMale, Serialize(Sex.Male));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Serialize_Female_EmitsKobieta()
    {
        Assert.Equal(WireFemale, Serialize(Sex.Female));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_AccentedMezczyzna_ReturnsMale()
    {
        Assert.Equal(Sex.Male, Deserialize(WireMale));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_UnaccentedMezczyzna_ReturnsMale()
    {
        //Arrange
        // Unaccented variant accepted for backward compatibility with legacy saved files
        var legacyWireValue = WireMaleUnaccented;

        //Act
        var result = Deserialize(legacyWireValue);

        //Assert
        Assert.Equal(Sex.Male, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_Kobieta_ReturnsFemale()
    {
        Assert.Equal(Sex.Female, Deserialize(WireFemale));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_Nieznana_ReturnsUnknown()
    {
        Assert.Equal(Sex.Unknown, Deserialize(WireUnknown));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_EmptyString_ReturnsUnknown()
    {
        Assert.Equal(Sex.Unknown, Deserialize(string.Empty));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deserialize_UnknownLegacyValue_ReturnsUnknown()
    {
        // Tolerant of unknown values — no throw
        Assert.Equal(Sex.Unknown, Deserialize("SomethingUnrecognised"));
    }

    private static string Serialize(Sex sex)
    {
        // Serialize as a top-level JSON object with a "sex" property to exercise the converter
        var wrapper = new MeFile { Sex = sex };
        var json = JsonSerializer.Serialize(wrapper, MeFile.DefaultOptions);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("sex").GetString()!;
    }

    private static Sex Deserialize(string raw)
    {
        var json = $$"""{"unique_identifier":"00000000-0000-0000-0000-000000000000","person_name":"","location":"","first_name":"","other_first_names":"","last_name":"","other_last_names":"","maiden_name":"","other_maiden_names":"","has_maiden_name":false,"sex":"{{raw}}","spouse":[],"spouse_id":[],"children":[],"children_id":[],"parents":[],"parents_id":[],"siblings":[],"siblings_id":[],"notes":"","dates_of_birth":"","dates_of_death":""}""";
        return JsonSerializer.Deserialize<MeFile>(json, MeFile.DefaultOptions)!.Sex;
    }
}
