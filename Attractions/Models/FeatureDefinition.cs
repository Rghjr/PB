using System.Text.Json.Serialization;
using Attractions.Helpers;

namespace Attractions.Models;

public class FeatureDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; private set; }

    [JsonPropertyName("type")]
    public FeatureValueType Type { get; }

    [JsonPropertyName("description")]
    public string Description { get; private set; }

    [JsonPropertyName("comparisonRule")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FeatureComparisonRule? ComparisonRule { get; private set; }

    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; private set; }

    [JsonPropertyName("allowedValues")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedValues { get; private set; }

    [JsonPropertyName("profileBindingKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProfileBindingKey { get; }

    [JsonIgnore]
    public FeatureComparisonRule EffectiveRule => ComparisonRule ?? ResolveDefaultRule();

    [JsonConstructor]
    public FeatureDefinition(
        int id,
        string name,
        FeatureValueType type,
        string description = "",
        FeatureComparisonRule? comparisonRule = null,
        string? unit = null,
        List<string>? allowedValues = null,
        string? profileBindingKey = null)
    {
        Id = id;
        Name = name ?? string.Empty;
        Type = type;
        Description = description ?? string.Empty;
        ComparisonRule = comparisonRule;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit;
        AllowedValues = allowedValues is { Count: > 0 } ? new List<string>(allowedValues) : null;
        ProfileBindingKey = profileBindingKey;
    }

    public void UpdateDefinition(
        string name,
        string description,
        FeatureComparisonRule comparisonRule,
        string? unit = null,
        IEnumerable<string>? allowedValues = null)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        ComparisonRule = comparisonRule;

        if (Type == FeatureValueType.Number)
        {
            Unit = string.IsNullOrWhiteSpace(unit) ? null : unit;
        }

        if (Type == FeatureValueType.OptionList && allowedValues is not null)
        {
            AllowedValues = new List<string>(allowedValues);
        }
    }

    public FeatureComparisonRule ResolveDefaultRule()
    {
        string lowerName = Name.ToLowerInvariant();

        return Type switch
        {
            FeatureValueType.Boolean =>
                lowerName.Contains("wymagan") || lowerName.Contains("obowiązk")
                    ? FeatureComparisonRule.ConditionRequired
                    : FeatureComparisonRule.FacilityAvailable,

            FeatureValueType.Number =>
                lowerName.Contains("maksymaln") || lowerName.Contains("max") || lowerName.Contains("górny")
                    ? FeatureComparisonRule.MaxThreshold
                    : FeatureComparisonRule.MinThreshold,

            FeatureValueType.OptionList =>
                AllowedValues is { Count: >= 2 }
                    ? FeatureComparisonRule.MaxLevelIndex
                    : FeatureComparisonRule.ExactOrContainsMatch,

            _ => FeatureComparisonRule.ExactOrContainsMatch
        };
    }

    public bool TryNormalizeValue(string? rawInput, out string normalizedValue, out string errorMessage)
    {
        normalizedValue = string.Empty;
        errorMessage = string.Empty;

        string cleaned = DomainValueHelper.CleanText(rawInput);
        if (cleaned.Length == 0)
        {
            errorMessage = $"Wartość dla cechy \"{Name}\" nie może być pusta.";
            return false;
        }

        switch (Type)
        {
            case FeatureValueType.Boolean:
                if (DomainValueHelper.TryParseBooleanFlexible(cleaned, out bool boolVal))
                {
                    normalizedValue = boolVal ? "true" : "false";
                    return true;
                }

                errorMessage = $"Cecha \"{Name}\" wymaga wartości logicznej (Tak / Nie / true / false).";
                return false;

            case FeatureValueType.Number:
                if (DomainValueHelper.TryParseDecimalInvariant(cleaned, out decimal numberVal))
                {
                    normalizedValue = DomainValueHelper.FormatDecimal(numberVal);
                    return true;
                }

                errorMessage = $"Cecha \"{Name}\" wymaga poprawnej wartości liczbowej.";
                return false;

            case FeatureValueType.OptionList:
                if (AllowedValues is null || AllowedValues.Count == 0)
                {
                    normalizedValue = cleaned;
                    return true;
                }

                if (int.TryParse(cleaned, out int optionNumber) &&
                    optionNumber >= 1 &&
                    optionNumber <= AllowedValues.Count)
                {
                    normalizedValue = AllowedValues[optionNumber - 1];
                    return true;
                }

                int matchedIndex = GetOptionIndex(cleaned);
                if (matchedIndex >= 0)
                {
                    normalizedValue = AllowedValues[matchedIndex];
                    return true;
                }

                errorMessage =
                    $"Wartość \"{cleaned}\" nie należy do dozwolonych opcji cechy \"{Name}\": {string.Join(", ", AllowedValues)}.";
                return false;

            case FeatureValueType.Text:
            default:
                normalizedValue = cleaned;
                return true;
        }
    }

    public string FormatValue(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "(brak)";
        }

        return Type switch
        {
            FeatureValueType.Boolean when DomainValueHelper.TryParseBooleanFlexible(rawValue, out bool b) =>
                b ? "Tak" : "Nie",
            FeatureValueType.Number when !string.IsNullOrWhiteSpace(Unit) =>
                $"{rawValue} {Unit}",
            _ => rawValue
        };
    }

    public string SelectStricterValue(string firstRawValue, string secondRawValue, out bool secondIsStricter)
    {
        secondIsStricter = false;

        switch (EffectiveRule)
        {
            case FeatureComparisonRule.FacilityAvailable:
            {
                bool first = DomainValueHelper.TryParseBooleanFlexible(firstRawValue, out bool b1) && b1;
                bool second = DomainValueHelper.TryParseBooleanFlexible(secondRawValue, out bool b2) && b2;
                if (first && !second)
                {
                    secondIsStricter = true;
                    return "false";
                }

                return first && second ? "true" : "false";
            }

            case FeatureComparisonRule.ConditionRequired:
            {
                bool first = DomainValueHelper.TryParseBooleanFlexible(firstRawValue, out bool b1) && b1;
                bool second = DomainValueHelper.TryParseBooleanFlexible(secondRawValue, out bool b2) && b2;
                if (!first && second)
                {
                    secondIsStricter = true;
                    return "true";
                }

                return first || second ? "true" : "false";
            }

            case FeatureComparisonRule.MinThreshold:
            case FeatureComparisonRule.MaxThreshold:
                return SelectStricterNumeric(
                    firstRawValue,
                    secondRawValue,
                    isMinThreshold: EffectiveRule == FeatureComparisonRule.MinThreshold,
                    out secondIsStricter);

            case FeatureComparisonRule.MaxLevelIndex:
            {
                int idx1 = GetOptionIndex(firstRawValue);
                int idx2 = GetOptionIndex(secondRawValue);
                if (idx2 > idx1)
                {
                    secondIsStricter = true;
                    return secondRawValue;
                }

                return firstRawValue;
            }

            default:
                return firstRawValue;
        }
    }

    public RequirementCheckResult? EvaluateAgainstProfile(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return EffectiveRule switch
        {
            FeatureComparisonRule.FacilityAvailable => EvaluateFacility(effectiveOfferValue, profile, sourceElement),
            FeatureComparisonRule.ConditionRequired => EvaluateCondition(effectiveOfferValue, profile, sourceElement),
            FeatureComparisonRule.MinThreshold => EvaluateNumericThreshold(effectiveOfferValue, profile, sourceElement, isMinThreshold: true),
            FeatureComparisonRule.MaxThreshold => EvaluateNumericThreshold(effectiveOfferValue, profile, sourceElement, isMinThreshold: false),
            FeatureComparisonRule.MaxLevelIndex => EvaluateMaxLevel(effectiveOfferValue, profile, sourceElement),
            _ => EvaluateTextOrOptionMatch(effectiveOfferValue, profile, sourceElement)
        };
    }

    public int GetOptionIndex(string? optionValue)
    {
        string cleaned = DomainValueHelper.CleanText(optionValue);
        if (cleaned.Length == 0 || AllowedValues is null)
        {
            return -1;
        }

        for (int i = 0; i < AllowedValues.Count; i++)
        {
            if (string.Equals(AllowedValues[i], cleaned, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        string asciiTarget = DomainValueHelper.RemoveDiacritics(cleaned);
        for (int i = 0; i < AllowedValues.Count; i++)
        {
            if (string.Equals(DomainValueHelper.RemoveDiacritics(AllowedValues[i]), asciiTarget, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string SelectStricterNumeric(
        string firstRawValue,
        string secondRawValue,
        bool isMinThreshold,
        out bool secondIsStricter)
    {
        secondIsStricter = false;
        bool ok1 = DomainValueHelper.TryParseDecimalInvariant(firstRawValue, out decimal d1);
        bool ok2 = DomainValueHelper.TryParseDecimalInvariant(secondRawValue, out decimal d2);

        if (ok1 && ok2)
        {
            bool stricter = isMinThreshold ? d2 > d1 : d2 < d1;
            if (stricter)
            {
                secondIsStricter = true;
                return DomainValueHelper.FormatDecimal(d2);
            }

            return DomainValueHelper.FormatDecimal(d1);
        }

        return ok2 ? DomainValueHelper.FormatDecimal(d2) : firstRawValue;
    }

    private RequirementCheckResult? EvaluateFacility(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement)
    {
        if (!profile.RequiresFacility(this))
        {
            return null;
        }

        bool available = DomainValueHelper.TryParseBooleanFlexible(effectiveOfferValue, out bool b) && b;
        if (available)
        {
            return CreateCheckResult(
                RequirementEvaluationStatus.Met,
                "Tak",
                "Wymagane przez uczestnika",
                $"{Name}: zapewnione.",
                sourceElement);
        }

        string reason = ProfileBindingKey == "wheelchair"
            ? "Brak dostępności dla osób poruszających się na wózku inwalidzkim."
            : $"Brak wymaganego udogodnienia: {Name}.";

        return CreateCheckResult(
            RequirementEvaluationStatus.Unmet,
            "Nie",
            "Wymagane przez uczestnika",
            reason,
            sourceElement);
    }

    private RequirementCheckResult? EvaluateCondition(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement)
    {
        bool conditionRequired = DomainValueHelper.TryParseBooleanFlexible(effectiveOfferValue, out bool b) && b;
        if (!conditionRequired)
        {
            return null;
        }

        bool satisfied = profile.SatisfiesCondition(this, out string participantDetail);
        return satisfied
            ? CreateCheckResult(
                RequirementEvaluationStatus.Met,
                "Tak",
                participantDetail,
                $"{Name}: spełnione ({participantDetail}).",
                sourceElement)
            : CreateCheckResult(
                RequirementEvaluationStatus.Unmet,
                "Tak",
                participantDetail,
                $"Wymagany warunek niespełniony – {Name} ({participantDetail}).",
                sourceElement);
    }

    private RequirementCheckResult? EvaluateNumericThreshold(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement,
        bool isMinThreshold)
    {
        if (!DomainValueHelper.TryParseDecimalInvariant(effectiveOfferValue, out decimal limitValue))
        {
            return null;
        }

        if (isMinThreshold && limitValue <= 0)
        {
            return null;
        }

        string boundWord = isMinThreshold ? "min." : "maks.";
        string formattedReq = FormatValue(DomainValueHelper.FormatDecimal(limitValue));
        decimal? participantMetric = profile.GetNumericMetric(this);

        if (!participantMetric.HasValue)
        {
            return CreateCheckResult(
                RequirementEvaluationStatus.Warning,
                formattedReq,
                "nie podano",
                $"Wymóg cechy \"{Name}\": {boundWord} {formattedReq} (brak podanej wartości w profilu uczestnika).",
                sourceElement);
        }

        string formattedUser = FormatValue(DomainValueHelper.FormatDecimal(participantMetric.Value));
        string subjectLabel = ResolveParticipantSubjectLabel(profile, isMinThreshold);
        bool passes = isMinThreshold ? participantMetric.Value >= limitValue : participantMetric.Value <= limitValue;

        if (passes)
        {
            string verb = isMinThreshold ? "wymagane min." : "dozwolone maks.";
            return CreateCheckResult(
                RequirementEvaluationStatus.Met,
                formattedReq,
                formattedUser,
                $"{Name}: {verb} {formattedReq} ({subjectLabel}: {formattedUser}).",
                sourceElement);
        }

        string capitalizedSubject = DomainValueHelper.CapitalizeFirst(subjectLabel);
        string unmetMessage = (isMinThreshold, ProfileBindingKey) switch
        {
            (true, "min_age") => $"Wymagany minimalny wiek: {formattedReq}. {capitalizedSubject}: {formattedUser}.",
            (false, "max_age") => $"Maksymalny dopuszczalny wiek: {formattedReq}. {capitalizedSubject}: {formattedUser}.",
            (true, _) => $"Wymagany próg cechy \"{Name}\": min. {formattedReq}. {capitalizedSubject}: {formattedUser}.",
            (false, _) => $"Przekroczony limit cechy \"{Name}\": maks. {formattedReq}. {capitalizedSubject}: {formattedUser}."
        };

        return CreateCheckResult(
            RequirementEvaluationStatus.Unmet,
            formattedReq,
            formattedUser,
            unmetMessage,
            sourceElement);
    }

    private RequirementCheckResult? EvaluateMaxLevel(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement)
    {
        string? maxAcceptable = profile.GetOptionOrTextPreference(this);
        if (string.IsNullOrWhiteSpace(maxAcceptable))
        {
            return null;
        }

        int offerIndex = GetOptionIndex(effectiveOfferValue);
        int userIndex = GetOptionIndex(maxAcceptable);

        if (offerIndex < 0 || userIndex < 0)
        {
            return EvaluateTextOrOptionMatch(effectiveOfferValue, profile, sourceElement);
        }

        return offerIndex <= userIndex
            ? CreateCheckResult(
                RequirementEvaluationStatus.Met,
                effectiveOfferValue,
                maxAcceptable,
                $"{Name}: {effectiveOfferValue} (akceptowalny poziom: do \"{maxAcceptable}\").",
                sourceElement)
            : CreateCheckResult(
                RequirementEvaluationStatus.Unmet,
                effectiveOfferValue,
                maxAcceptable,
                $"{Name}: wymagany poziom \"{effectiveOfferValue}\", podczas gdy maksymalny akceptowalny poziom w profilu to \"{maxAcceptable}\".",
                sourceElement);
    }

    private RequirementCheckResult? EvaluateTextOrOptionMatch(
        string effectiveOfferValue,
        ParticipantProfile profile,
        string sourceElement)
    {
        string? desiredText = profile.GetOptionOrTextPreference(this);
        if (string.IsNullOrWhiteSpace(desiredText))
        {
            return null;
        }

        bool matches = effectiveOfferValue.Contains(desiredText, StringComparison.OrdinalIgnoreCase);
        return matches
            ? CreateCheckResult(
                RequirementEvaluationStatus.Met,
                effectiveOfferValue,
                desiredText,
                $"{Name}: dopasowano \"{desiredText}\" (dostępne: {effectiveOfferValue}).",
                sourceElement)
            : CreateCheckResult(
                RequirementEvaluationStatus.Unmet,
                effectiveOfferValue,
                desiredText,
                $"{Name}: wymagano \"{desiredText}\", a oferta zapewnia \"{effectiveOfferValue}\".",
                sourceElement);
    }

    private RequirementCheckResult CreateCheckResult(
        RequirementEvaluationStatus status,
        string requiredValueText,
        string participantValueText,
        string message,
        string sourceElement)
    {
        return new RequirementCheckResult(
            featureId: Id,
            featureName: Name,
            status: status,
            requiredValueText: requiredValueText,
            participantValueText: participantValueText,
            message: message,
            sourceElement: sourceElement);
    }

    private string ResolveParticipantSubjectLabel(ParticipantProfile profile, bool isMinRule)
    {
        return ProfileBindingKey switch
        {
            "min_age" => profile.IsGroup ? "wiek najmłodszego uczestnika" : "wiek uczestnika",
            "max_age" => profile.IsGroup ? "wiek najstarszego uczestnika" : "wiek uczestnika",
            "min_height" => profile.IsGroup ? "wzrost najniższego uczestnika" : "wzrost uczestnika",
            _ => profile.IsGroup
                ? (isMinRule ? "najniższa wartość w grupie" : "najwyższa wartość w grupie")
                : "wartość uczestnika"
        };
    }
}
