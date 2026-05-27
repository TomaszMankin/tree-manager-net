using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreeManager.Core.Domain;

/// <summary>Custom JSON converter for <see cref="Sex"/>. Writes Polish words on serialize. On deserialize accepts both accented "Mężczyzna" and unaccented "Mezczyzna".</summary>
public sealed class SexJsonConverter : JsonConverter<Sex>
{
    public override Sex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Sex.Unknown;
        }

        var raw = reader.GetString();
        return (raw ?? string.Empty).Trim() switch
        {
            "Mężczyzna" or "Mezczyzna" => Sex.Male,
            "Kobieta" => Sex.Female,
            "" or "Nieznana" => Sex.Unknown,
            _ => Sex.Unknown, // tolerant of unknown legacy values
        };
    }

    public override void Write(Utf8JsonWriter writer, Sex value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            Sex.Male => "Mężczyzna",
            Sex.Female => "Kobieta",
            _ => "Nieznana", // Sex.Unknown writes the Polish word, not empty string
        });
    }
}
