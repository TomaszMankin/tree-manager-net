namespace TreeManager.Core.Domain;

/// <summary>Format helpers for <see cref="PartialDate"/>. Wire format "DD|MM|YYYY" with "XX" wildcards for unknown components.</summary>
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

    private static string ToFormattedValue(int? value, int pad)
    {
        return value.HasValue
            ? value.Value.ToString().PadLeft(pad, '0')
            : new string(Wildcard, pad);
    }
}
