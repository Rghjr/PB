using System.Globalization;
using Attractions.Models;

namespace Attractions.Helpers;

public static class DomainValueHelper
{
    public static string CleanText(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
    }

    public static string? CleanNullableText(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || email.Contains(' '))
        {
            return false;
        }

        int atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@'))
        {
            return false;
        }

        int dotIndex = email.IndexOf('.', atIndex + 2);
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    public static bool TryParseBooleanFlexible(string? raw, out bool result)
    {
        result = false;
        string cleaned = CleanText(raw).ToLowerInvariant();
        if (cleaned.Length == 0)
        {
            return false;
        }

        switch (cleaned)
        {
            case "true":
            case "tak":
            case "t":
            case "yes":
            case "y":
            case "1":
                result = true;
                return true;

            case "false":
            case "nie":
            case "n":
            case "no":
            case "0":
                result = false;
                return true;

            default:
                return false;
        }
    }

    public static bool TryParseDecimalInvariant(string? raw, out decimal value)
    {
        value = 0m;
        string cleaned = CleanText(raw);
        if (cleaned.Length == 0)
        {
            return false;
        }

        return decimal.TryParse(
            cleaned.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static string FormatDecimal(decimal value)
    {
        return value == decimal.Truncate(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string RemoveDiacritics(string text)
    {
        return text
            .Replace('ą', 'a').Replace('Ą', 'A')
            .Replace('ć', 'c').Replace('Ć', 'C')
            .Replace('ę', 'e').Replace('Ę', 'E')
            .Replace('ł', 'l').Replace('Ł', 'L')
            .Replace('ń', 'n').Replace('Ń', 'N')
            .Replace('ó', 'o').Replace('Ó', 'O')
            .Replace('ś', 's').Replace('Ś', 'S')
            .Replace('ź', 'z').Replace('Ź', 'Z')
            .Replace('ż', 'z').Replace('Ż', 'Z');
    }

    public static string CapitalizeFirst(string text)
    {
        return string.IsNullOrEmpty(text)
            ? text
            : char.ToUpperInvariant(text[0]) + text[1..];
    }

    public static void UpsertFeatureAssignment(
        List<FeatureAssignment> targetList,
        int featureId,
        string normalizedValue,
        AddonEffectMode effectMode = AddonEffectMode.TightenRequirement)
    {
        int index = targetList.FindIndex(f => f.FeatureId == featureId);
        var updated = new FeatureAssignment(featureId, normalizedValue, effectMode);

        if (index >= 0)
        {
            targetList[index] = updated;
        }
        else
        {
            targetList.Add(updated);
        }
    }

    public static bool RemoveFeatureAssignment(List<FeatureAssignment> targetList, int featureId)
    {
        return targetList.RemoveAll(f => f.FeatureId == featureId) > 0;
    }

    public static string? GetFeatureValue(IReadOnlyCollection<FeatureAssignment> sourceList, int featureId)
    {
        return sourceList.FirstOrDefault(f => f.FeatureId == featureId)?.Value;
    }
}
