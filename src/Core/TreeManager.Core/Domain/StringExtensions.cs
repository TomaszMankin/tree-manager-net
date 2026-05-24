namespace TreeManager.Core.Domain;

public static class StringExtensions
{
    /// <summary>Parses wire format "DD|MM|YYYY" into a <see cref="PartialDate"/>. Returns <see langword="default"/> on malformed input. Throws <see cref="ArgumentNullException"/> when called on null.</summary>
    public static PartialDate ParsePartialDate(this string input)
    {
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        if (input.Length == 0) return default;

        var chunks = input.Split('|');
        if (chunks.Length != 3) return default;

        int?[] parts = new int?[3];
        for (int i = 0; i < 3; i++)
        {
            parts[i] = int.TryParse(chunks[i], out var result) ? result : (int?)null;
        }

        return new PartialDate(parts[0], parts[1], parts[2]);
    }
}
