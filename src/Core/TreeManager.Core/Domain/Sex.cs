using System.Text.Json.Serialization;

namespace TreeManager.Core.Domain;

/// <summary>Biological sex of a person. Wire format uses Polish words (py-tree-manager parity).</summary>
[JsonConverter(typeof(SexJsonConverter))]
public enum Sex
{
    /// <summary>Unknown / not specified ("Nieznana" on wire).</summary>
    Unknown = 0,

    /// <summary>Male ("Mężczyzna" on wire; also tolerates unaccented "Mezczyzna" on read).</summary>
    Male = 1,

    /// <summary>Female ("Kobieta" on wire).</summary>
    Female = 2,
}
