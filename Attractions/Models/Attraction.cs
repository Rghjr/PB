using Attractions.Data;
using Attractions.Helpers;

namespace Attractions.Models;

public class Attraction
{
    public int Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Location { get; private set; }
    public int MaxParticipants { get; private set; }
    public List<FeatureAssignment> Features { get; private set; }

    public Attraction(
        int id,
        string name,
        string description,
        string location,
        int maxParticipants = 30,
        IEnumerable<FeatureAssignment>? features = null)
    {
        Id = id;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Location = location ?? string.Empty;
        MaxParticipants = Math.Max(1, maxParticipants);
        Features = features is not null
            ? features.Select(f => f.Clone()).ToList()
            : new List<FeatureAssignment>();
    }

    public void UpdateDetails(
        string name,
        string description,
        string location,
        int? maxParticipants = null,
        IEnumerable<FeatureAssignment>? features = null)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Location = location ?? string.Empty;

        if (maxParticipants.HasValue)
        {
            MaxParticipants = Math.Max(1, maxParticipants.Value);
        }

        if (features is not null)
        {
            ReplaceFeatures(features);
        }
    }

    public void ReplaceFeatures(IEnumerable<FeatureAssignment> features)
    {
        Features = features.Select(f => f.Clone()).ToList();
    }

    public void SetMaxParticipants(int maxParticipants)
    {
        MaxParticipants = Math.Max(1, maxParticipants);
    }

    public void SetFeatureValue(int featureId, string normalizedValue)
    {
        DomainValueHelper.UpsertFeatureAssignment(Features, featureId, normalizedValue);
    }

    public bool RemoveFeatureValue(int featureId)
    {
        return DomainValueHelper.RemoveFeatureAssignment(Features, featureId);
    }

    public string? GetFeatureValue(int featureId)
    {
        return DomainValueHelper.GetFeatureValue(Features, featureId);
    }

    public Dictionary<int, EffectiveFeatureValue> GetEffectiveFeatures()
    {
        var result = new Dictionary<int, EffectiveFeatureValue>();

        foreach (var assignment in Features)
        {
            result[assignment.FeatureId] = new EffectiveFeatureValue(
                assignment.FeatureId,
                assignment.Value,
                $"Atrakcja \"{Name}\"");
        }

        return result;
    }

    public CompatibilityReport ValidateAgainst(ParticipantProfile profile)
    {
        return ValidateAgainst(profile, InMemoryData.Features);
    }

    public CompatibilityReport ValidateAgainst(
        ParticipantProfile profile,
        IReadOnlyCollection<FeatureDefinition> featureDefinitions)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(featureDefinitions);

        var effectiveFeatures = GetEffectiveFeatures();
        var report = FeatureSetEvaluator.Evaluate(
            targetName: Name,
            targetTypeLabel: "ATRAKCJI",
            fallbackMissingFacilitySource: $"Atrakcja \"{Name}\"",
            effectiveFeatures: effectiveFeatures,
            profile: profile,
            definitions: featureDefinitions,
            includeSourceInMessages: false);

        int count = profile.ParticipantsCount;
        if (count > MaxParticipants)
        {
            report.UnmetRequirements.Insert(0, new RequirementCheckResult(
                featureId: 0,
                featureName: "Maksymalna liczba osób na atrakcji",
                status: RequirementEvaluationStatus.Unmet,
                requiredValueText: $"maks. {MaxParticipants} os.",
                participantValueText: $"{count} os.",
                message: $"Przekroczono maksymalną liczbę osób na atrakcji: dozwolone maks. {MaxParticipants} os., liczebność grupy: {count} os."));
        }
        else
        {
            report.MetRequirements.Insert(0, new RequirementCheckResult(
                featureId: 0,
                featureName: "Maksymalna liczba osób na atrakcji",
                status: RequirementEvaluationStatus.Met,
                requiredValueText: $"maks. {MaxParticipants} os.",
                participantValueText: $"{count} os.",
                message: $"Liczebność uczestników ({count} os.) mieści się w limicie atrakcji (maks. {MaxParticipants} os.)."));
        }

        return report;
    }
}
