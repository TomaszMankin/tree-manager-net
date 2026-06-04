using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Services;

/// <summary>Pure helpers for Drzewo filename encoding — couple code, full name, filename render.</summary>
public static class FolderTreeNaming
{
    private const string UnknownSentinel = "(nieznane)";

    /// <summary>Returns the gender token (M/F) for a <see cref="Sex"/> value.</summary>
    public static string GenderToken(Sex sex)
    {
        if (sex == Sex.Male) { return "M"; }
        if (sex == Sex.Female) { return "F"; }
        return string.Empty;
    }

    /// <summary>Returns the per-generation base-26 couple code for a given index and total.
    /// Width is pre-detected per generation from the total couple count.</summary>
    public static string CoupleCode(int coupleIndex, int totalCouples)
    {
        int n = Math.Max(1, totalCouples);

        int width;
        if (n <= 26) { width = 1; }
        else if (n <= 676) { width = 2; }
        else if (n <= 17576) { width = 3; }
        else if (n <= 456976) { width = 4; }
        else
        {
            width = (int)Math.Ceiling(Math.Log(n) / Math.Log(26));
        }

        var digits = new List<char>(width);
        int idx = coupleIndex;
        for (int i = 0; i < width; i++)
        {
            digits.Add((char)('A' + (idx % 26)));
            idx /= 26;
        }
        digits.Reverse();
        return new string([.. digits]);
    }

    /// <summary>Computes the display full name of a person from a <see cref="MeFile"/>.
    /// Returns empty string for unknown name segments, sentinel for missing first/last name.</summary>
    public static string FullName(MeFile meFile)
    {
        var sb = new StringBuilder();

        string firstName = string.IsNullOrEmpty(meFile.FirstName) ? UnknownSentinel : meFile.FirstName;
        sb.Append(firstName);

        if (!string.IsNullOrEmpty(meFile.OtherFirstNames))
        {
            sb.Append(' ');
            sb.Append(meFile.OtherFirstNames);
        }

        sb.Append(' ');
        string lastName = string.IsNullOrEmpty(meFile.LastName) ? UnknownSentinel : meFile.LastName;
        sb.Append(lastName);

        if (!string.IsNullOrEmpty(meFile.OtherLastNames))
        {
            sb.Append(';');
            sb.Append(meFile.OtherLastNames);
        }

        if (meFile.HasMaidenName)
        {
            if (!string.IsNullOrEmpty(meFile.MaidenName))
            {
                sb.Append(" zd. ");
                sb.Append(meFile.MaidenName);

                if (!string.IsNullOrEmpty(meFile.OtherMaidenNames))
                {
                    sb.Append(';');
                    sb.Append(meFile.OtherMaidenNames);
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>Replaces filesystem-forbidden characters with underscore. Polish diacritics untouched.</summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        var sb = new StringBuilder(name.Length);
        foreach (char ch in name)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Deduplicates <paramref name="filename"/> against <paramref name="seen"/> by appending a (N) suffix.
    /// Does NOT add the result to <paramref name="seen"/>; the caller is responsible for that.
    /// </summary>
    public static string Deduplicate(string filename, HashSet<string> seen)
    {
        if (!seen.Contains(filename))
        {
            return filename;
        }

        string withoutExt = filename.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            ? filename[..^4]
            : filename;

        int n = 2;
        while (true)
        {
            string candidate = $"{withoutExt} ({n}).lnk";
            if (!seen.Contains(candidate))
            {
                return candidate;
            }

            n++;
        }
    }

    /// <summary>Encodes a <see cref="FolderTreeMember"/> into the Drzewo shortcut filename.
    /// Format gen==0: [NN][0][gender] FullName.lnk
    /// Format gen!=0: [NN][display][couple-code][gender] FullName.lnk
    /// where NN=gen+50, display=-gen, couple-code=base-26 letter(s), gender defaults M when empty.</summary>
    public static string RenderFilename(FolderTreeMember member)
    {
        int gen = member.Generation;
        string nn = (gen + 50).ToString("D2");
        string genderToken = string.IsNullOrEmpty(member.Gender) ? "M" : member.Gender;
        int display = -gen;
        string sanitized = Sanitize(member.FullName);

        if (gen == 0)
        {
            return $"[{nn}][{display}][{genderToken}] {sanitized}.lnk";
        }
        else
        {
            string code = CoupleCode(member.CoupleIndex, member.TotalCouplesInGeneration);
            return $"[{nn}][{display}][{code}][{genderToken}] {sanitized}.lnk";
        }
    }
}
