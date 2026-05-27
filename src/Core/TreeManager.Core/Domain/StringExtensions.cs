namespace TreeManager.Core.Domain;

public static class StringExtensions
{
    /// <summary>Parses wire format "DD|MM|YYYY" (with "--" wildcards) into a <see cref="PartialDate"/>. Returns <see langword="default"/> on malformed input. Throws <see cref="ArgumentNullException"/> when called on null.</summary>
    public static PartialDate ParsePartialDate(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
        {
            return default;
        }

        var chunks = input.Split('|');
        if (chunks.Length != 3)
        {
            return default;
        }

        var day = int.TryParse(chunks[0], out var d) ? d : (int?)null;
        var month = int.TryParse(chunks[1], out var m) ? m : (int?)null;
        var yearChunk = chunks[2];
        var year = yearChunk.All(c => c == '-') ? null : yearChunk;

        return new PartialDate(day, month, year);
    }
}
