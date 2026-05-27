namespace TreeManager.Core.Domain;

/// <summary>Format helpers for <see cref="PartialDate"/>. Wire format "DD|MM|YYYY" with "--" wildcards for unknown components.</summary>
public static class PartialDateExtensions
{
    private const char Wildcard = '-';
    private const char SerializableSeparator = '|';
    private const char DateSeparator = '/';

    /// <summary>Parses wire format "DD|MM|YYYY" (with "--" wildcards) back to a <see cref="PartialDate"/>.</summary>
    public static PartialDate ToPartialDate(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var chunks = input.Split(SerializableSeparator);
        if (chunks.Length != 3)
        {
            return default;
        }

        var day = int.TryParse(chunks[0], out var d) ? d : (int?)null;
        var month = int.TryParse(chunks[1], out var m) ? m : (int?)null;
        var yearChunk = chunks[2];
        var year = yearChunk.All(c => c == Wildcard) ? null : yearChunk;

        return new PartialDate(day, month, year);
    }

    /// <summary>Formats to wire format "DD|MM|YYYY" with "--" wildcards.</summary>
    public static string ToSerializedString(this PartialDate date)
    {
        var day = FormatIntField(date.Day, 2);
        var month = FormatIntField(date.Month, 2);
        var year = date.Year ?? new string(Wildcard, 4);
        return $"{day}{SerializableSeparator}{month}{SerializableSeparator}{year}";
    }

    /// <summary>Formats to display format "DD/MM/YYYY" with "--" wildcards.</summary>
    public static string ToDateString(this PartialDate date)
    {
        var day = FormatIntField(date.Day, 2);
        var month = FormatIntField(date.Month, 2);
        var year = date.Year ?? new string(Wildcard, 4);
        return $"{day}{DateSeparator}{month}{DateSeparator}{year}";
    }

    private static string FormatIntField(int? value, int pad)
    {
        return value.HasValue
            ? value.Value.ToString().PadLeft(pad, '0')
            : new string(Wildcard, pad);
    }
}
