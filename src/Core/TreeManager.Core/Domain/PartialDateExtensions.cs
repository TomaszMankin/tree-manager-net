namespace TreeManager.Core.Domain;

/// <summary>Format helpers for <see cref="PartialDate"/>. Wire format "DD|MM|YYYY" with "--" wildcards for unknown components, optionally prefixed with qualifier chars.</summary>
public static class PartialDateExtensions
{
    private const char Wildcard = '-';
    private const char SerializableSeparator = '|';
    private const char DateSeparator = '/';
    private const char BeforePrefix = '<';
    private const char ApproxPrefix = '~';

    /// <summary>Parses wire format "DD|MM|YYYY" (with "--" wildcards and optional qualifier prefix) back to a <see cref="PartialDate"/>.</summary>
    public static PartialDate ToPartialDate(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var isApprox = false;
        var isBefore = false;
        var i = 0;
        while (i < input.Length && (input[i] == ApproxPrefix || input[i] == BeforePrefix))
        {
            if (input[i] == ApproxPrefix) { isApprox = true; }
            else { isBefore = true; }
            i++;
        }

        var body = input.Substring(i);
        var chunks = body.Split(SerializableSeparator);
        if (chunks.Length != 3)
        {
            return default;
        }

        var day = int.TryParse(chunks[0], out var d) ? d : (int?)null;
        var month = int.TryParse(chunks[1], out var m) ? m : (int?)null;
        var yearChunk = chunks[2];
        var year = yearChunk.All(c => c == Wildcard) ? null : yearChunk;

        return new PartialDate(day, month, year) { IsBefore = isBefore, IsApprox = isApprox };
    }

    /// <summary>Formats to wire format with optional qualifier prefix, then "DD|MM|YYYY" with "--" wildcards.</summary>
    public static string ToSerializedString(this PartialDate date)
    {
        var day = FormatIntField(date.Day, 2);
        var month = FormatIntField(date.Month, 2);
        var year = date.Year ?? new string(Wildcard, 4);
        var body = $"{day}{SerializableSeparator}{month}{SerializableSeparator}{year}";

        var prefix = (date.IsApprox ? ApproxPrefix.ToString() : string.Empty)
            + (date.IsBefore ? BeforePrefix.ToString() : string.Empty);

        return prefix + body;
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
