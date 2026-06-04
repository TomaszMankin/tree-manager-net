using System;
using System.Collections.Generic;
using System.Linq;
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

    // Location is a derived machine-local path applied at save time only;
    // excluding it prevents false-positive dirty detection when comparing
    // a loaded snapshot (which carries Location) against a freshly assembled state.
    public bool Equals(MeFile other)
    {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return UniqueIdentifier == other.UniqueIdentifier
            && PersonName == other.PersonName
            && FirstName == other.FirstName
            && OtherFirstNames == other.OtherFirstNames
            && LastName == other.LastName
            && OtherLastNames == other.OtherLastNames
            && MaidenName == other.MaidenName
            && OtherMaidenNames == other.OtherMaidenNames
            && HasMaidenName == other.HasMaidenName
            && Sex == other.Sex
            && Notes == other.Notes
            && DatesOfBirth == other.DatesOfBirth
            && DatesOfDeath == other.DatesOfDeath
            && Parents.SequenceEqual(other.Parents)
            && ParentsId.SequenceEqual(other.ParentsId)
            && Children.SequenceEqual(other.Children)
            && ChildrenId.SequenceEqual(other.ChildrenId)
            && Spouse.SequenceEqual(other.Spouse)
            && SpouseId.SequenceEqual(other.SpouseId)
            && Siblings.SequenceEqual(other.Siblings)
            && SiblingsId.SequenceEqual(other.SiblingsId);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(UniqueIdentifier);
        hash.Add(PersonName);
        hash.Add(FirstName);
        hash.Add(OtherFirstNames);
        hash.Add(LastName);
        hash.Add(OtherLastNames);
        hash.Add(MaidenName);
        hash.Add(OtherMaidenNames);
        hash.Add(HasMaidenName);
        hash.Add(Sex);
        hash.Add(Notes);
        hash.Add(DatesOfBirth);
        hash.Add(DatesOfDeath);
        foreach (var p in Parents) { hash.Add(p); }
        foreach (var p in ParentsId) { hash.Add(p); }
        foreach (var c in Children) { hash.Add(c); }
        foreach (var c in ChildrenId) { hash.Add(c); }
        foreach (var s in Spouse) { hash.Add(s); }
        foreach (var s in SpouseId) { hash.Add(s); }
        foreach (var s in Siblings) { hash.Add(s); }
        foreach (var s in SiblingsId) { hash.Add(s); }
        return hash.ToHashCode();
    }
}
