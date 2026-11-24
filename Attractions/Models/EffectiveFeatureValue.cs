namespace Attractions.Models;

public class EffectiveFeatureValue
{
    public int FeatureId { get; }
    public string Value { get; }
    public string SourceElement { get; }
    public bool OverridesAttractionDefault { get; }
    public bool ModifiedByAddon { get; }

    public EffectiveFeatureValue(
        int featureId,
        string value,
        string sourceElement,
        bool overridesAttractionDefault = false,
        bool modifiedByAddon = false)
    {
        FeatureId = featureId;
        Value = value ?? string.Empty;
        SourceElement = sourceElement ?? string.Empty;
        OverridesAttractionDefault = overridesAttractionDefault;
        ModifiedByAddon = modifiedByAddon;
    }

    public EffectiveFeatureValue Clone()
    {
        return new EffectiveFeatureValue(
            FeatureId,
            Value,
            SourceElement,
            OverridesAttractionDefault,
            ModifiedByAddon);
    }
}

/// <summary>
/// Wspólny mechanizm weryfikacji zestawu efektywnych cech względem profilu uczestnika,
/// wykorzystywany przez klasy domenowe Attraction, Offer oraz OfferConfiguration (zasada DRY).
/// </summary>
public static class FeatureSetEvaluator
{
    public static CompatibilityReport Evaluate(
        string targetName,
        string targetTypeLabel,
        string fallbackMissingFacilitySource,
        IReadOnlyDictionary<int, EffectiveFeatureValue> effectiveFeatures,
        ParticipantProfile profile,
        IReadOnlyCollection<FeatureDefinition> definitions,
        bool includeSourceInMessages = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(definitions);

        var met = new List<RequirementCheckResult>();
        var unmet = new List<RequirementCheckResult>();
        var warnings = new List<RequirementCheckResult>();

        var defById = definitions.ToDictionary(d => d.Id);

        foreach (var entry in effectiveFeatures.Values.OrderBy(e => e.FeatureId))
        {
            if (!defById.TryGetValue(entry.FeatureId, out var definition))
            {
                continue;
            }

            string source = includeSourceInMessages ? entry.SourceElement : string.Empty;
            var check = definition.EvaluateAgainstProfile(entry.Value, profile, source);
            if (check is null)
            {
                continue;
            }

            AddCheckToBuckets(check, met, unmet, warnings);
        }

        foreach (var facilityDef in definitions
                     .Where(d => d.EffectiveRule == FeatureComparisonRule.FacilityAvailable &&
                                 profile.RequiresFacility(d))
                     .OrderBy(d => d.Id))
        {
            if (effectiveFeatures.ContainsKey(facilityDef.Id))
            {
                continue;
            }

            string source = includeSourceInMessages ? fallbackMissingFacilitySource : string.Empty;
            string message = facilityDef.ProfileBindingKey == "wheelchair"
                ? "Brak potwierdzonej dostępności dla osób poruszających się na wózku inwalidzkim."
                : $"Brak wymaganego udogodnienia: {facilityDef.Name}.";

            unmet.Add(new RequirementCheckResult(
                featureId: facilityDef.Id,
                featureName: facilityDef.Name,
                status: RequirementEvaluationStatus.Unmet,
                requiredValueText: "Brak informacji / Nie",
                participantValueText: "Wymagane przez uczestnika",
                message: message,
                sourceElement: source));
        }

        return new CompatibilityReport(targetName, targetTypeLabel, met, unmet, warnings);
    }

    public static void AddCheckToBuckets(
        RequirementCheckResult check,
        List<RequirementCheckResult> met,
        List<RequirementCheckResult> unmet,
        List<RequirementCheckResult> warnings)
    {
        switch (check.Status)
        {
            case RequirementEvaluationStatus.Met:
                met.Add(check);
                break;
            case RequirementEvaluationStatus.Unmet:
                unmet.Add(check);
                break;
            case RequirementEvaluationStatus.Warning:
                warnings.Add(check);
                break;
        }
    }
}
