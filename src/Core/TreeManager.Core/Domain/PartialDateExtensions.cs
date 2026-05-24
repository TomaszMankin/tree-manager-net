namespace TreeManager.Core.Domain;

/// <summary>
/// Parse and format helpers for <see cref="PartialDate"/>.
/// Wire format "DD|MM|YYYY" with "XX" wildcards for unknown components.
/// Ported from familytree's ExtendedDateTimeExtension — see ADR-002.
/// </summary>
public static class PartialDateExtensions
{
    private const char Wildcard = 'X';
    private const char SerializableSeparator = '|';
    private const char DateSeparator = '/';

    /// <summary>Formats to wire format "DD|MM|YYYY" with "XX" wildcards.</summary>
    public static string ToSerializedString(this PartialDate date)
    {
        var day = ToFormattedValue(date.Day, 2);
        var month = ToFormattedValue(date.Month, 2);
        var year = ToFormattedValue(date.Year, 4);
        return $"{day}{SerializableSeparator}{month}{SerializableSeparator}{year}";
    }

    /// <summary>Formats to display format "DD/MM/YYYY" with "XX" wildcards.</summary>
    public static string ToDateString(this PartialDate date)
    {
        var day = ToFormattedValue(date.Day, 2);
        var month = ToFormattedValue(date.Month, 2);
        var year = ToFormattedValue(date.Year, 4);
        return $"{day}{DateSeparator}{month}{DateSeparator}{year}";
    }

    /// <summary>
    /// Parses wire format "DD|MM|YYYY". Returns <see langword="default"/> on malformed input.
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="input"/> is null.
    /// </summary>
    public static PartialDate Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        if (input.Length == 0) return default;

        var chunks = input.Split(SerializableSeparator);
        if (chunks.Length != 3) return default;

        int?[] parts = new int?[3];
        for (int i = 0; i < 3; i++)
        {
            if (int.TryParse(chunks[i], out var result))
            {
                parts[i] = result;
            }
            else
            {
                // Allow "XX" or "XXXX" wildcards; anything non-numeric maps to null
                parts[i] = null;
            }
        }

        // chunks order is [day, month, year]
        return new PartialDate(parts[0], parts[1], parts[2]);
    }

    private static string ToFormattedValue(int? value, int pad)
    {
        return value.HasValue
            ? value.Value.ToString().PadLeft(pad, '0')
            : new string(Wildcard, pad);
    }
}
