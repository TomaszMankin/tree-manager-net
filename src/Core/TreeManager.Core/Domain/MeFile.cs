using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreeManager.Core.Domain;

public sealed record MeFile
{
    /// <summary>
    /// Cached <see cref="JsonSerializerOptions"/> for use with <see cref="MeFile"/>.
    /// Uses <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> so Polish characters
    /// are written as raw UTF-8 bytes (not \uXXXX escapes), matching py-tree-manager output.
    /// PropertyNamingPolicy is intentionally NOT set — explicit [JsonPropertyName] per property.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [JsonPropertyName("unique_identifier")]
    public Guid UniqueIdentifier { get; init; } = Guid.Empty;

    [JsonPropertyName("person_name")]
    public string PersonName { get; init; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; init; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("other_first_names")]
    public string OtherFirstNames { get; init; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("other_last_names")]
    public string OtherLastNames { get; init; } = string.Empty;

    [JsonPropertyName("maiden_name")]
    public string MaidenName { get; init; } = string.Empty;

    [JsonPropertyName("other_maiden_names")]
    public string OtherMaidenNames { get; init; } = string.Empty;

    [JsonPropertyName("has_maiden_name")]
    public bool HasMaidenName { get; init; } = false;

    [JsonPropertyName("sex")]
    public Sex Sex { get; init; } = Sex.Unknown;

    [JsonPropertyName("spouse")]
    public List<string> Spouse { get; init; } = [];

    [JsonPropertyName("spouse_id")]
    public List<Guid> SpouseId { get; init; } = [];

    [JsonPropertyName("children")]
    public List<string> Children { get; init; } = [];

    [JsonPropertyName("children_id")]
    public List<Guid> ChildrenId { get; init; } = [];

    [JsonPropertyName("parents")]
    public List<string> Parents { get; init; } = [];

    [JsonPropertyName("parents_id")]
    public List<Guid> ParentsId { get; init; } = [];

    [JsonPropertyName("siblings")]
    public List<string> Siblings { get; init; } = [];

    [JsonPropertyName("siblings_id")]
    public List<Guid> SiblingsId { get; init; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;

    [JsonPropertyName("dates_of_birth")]
    public string DatesOfBirth { get; init; } = string.Empty;

    [JsonPropertyName("dates_of_death")]
    public string DatesOfDeath { get; init; } = string.Empty;
}
