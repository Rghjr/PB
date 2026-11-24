using Attractions.Data;
using Attractions.Helpers;

namespace Attractions.Models;

public class Offer
{
    public int Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public DateTime Date { get; private set; }
    public OfferStatus Status { get; private set; }
    public int AttractionId { get; private set; }
    public int MinParticipants { get; private set; }
    public int? MaxParticipants { get; private set; }
    public List<Addon> Addons { get; }
    public List<int> RelatedOfferIds { get; }
    public List<FeatureAssignment> Features { get; private set; }

    public Offer(
        int id,
        int attractionId,
        string name,
        string description,
        decimal price,
        DateTime date,
        OfferStatus status = OfferStatus.Planned,
        int minParticipants = 1,
        int? maxParticipants = null,
        IEnumerable<Addon>? addons = null,
        IEnumerable<FeatureAssignment>? features = null)
    {
        Id = id;
        AttractionId = attractionId;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Price = decimal.Round(price, 2);
        Date = date.Date;
        Status = status;
        MinParticipants = Math.Max(1, minParticipants);
        MaxParticipants = maxParticipants;
        Addons = addons is not null ? new List<Addon>(addons) : new List<Addon>();
        RelatedOfferIds = new List<int>();
        Features = features is not null
            ? features.Select(f => f.Clone()).ToList()
            : new List<FeatureAssignment>();
    }

    public bool IsToday => Status == OfferStatus.Planned && Date.Date == DateTime.Today;
    public bool IsFuturePlanned => Status == OfferStatus.Planned && Date.Date > DateTime.Today;
    public bool IsPast => Date.Date < DateTime.Today;
    public bool IsAvailableForUser => Status == OfferStatus.Planned && Date.Date >= DateTime.Today;
    public bool IsGroupOffer => MinParticipants > 1;

    public void UpdateDetails(
        int attractionId,
        string name,
        string description,
        decimal price,
        DateTime date,
        OfferStatus status,
        int minParticipants,
        int? maxParticipants,
        IEnumerable<FeatureAssignment>? features = null)
    {
        AttractionId = attractionId;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Price = decimal.Round(price, 2);
        Date = date.Date;
        Status = status;
        MinParticipants = Math.Max(1, minParticipants);
        MaxParticipants = maxParticipants;

        if (features is not null)
        {
            ReplaceFeatures(features);
        }
    }

    public void SetStatus(OfferStatus status)
    {
        Status = status;
    }

    public void SetGroupLimits(int minParticipants, int? maxParticipants)
    {
        MinParticipants = Math.Max(1, minParticipants);
        MaxParticipants = maxParticipants;
    }

    public void ReplaceFeatures(IEnumerable<FeatureAssignment> features)
    {
        Features = features.Select(f => f.Clone()).ToList();
    }

    public int GetEffectiveMaxParticipants(Attraction? parentAttraction = null)
    {
        var resolvedAttraction = parentAttraction ?? InMemoryData.Attractions.FirstOrDefault(a => a.Id == AttractionId);
        int attractionMax = resolvedAttraction?.MaxParticipants ?? 100;

        return MaxParticipants.HasValue
            ? Math.Min(MaxParticipants.Value, attractionMax)
            : attractionMax;
    }

    public bool IsExactGroupSizeOffer(Attraction? parentAttraction = null)
    {
        return MinParticipants > 1 && MinParticipants == GetEffectiveMaxParticipants(parentAttraction);
    }

    public string FormatGroupSizeLimit(Attraction? parentAttraction = null)
    {
        int max = GetEffectiveMaxParticipants(parentAttraction);
        if (MinParticipants > 1 && MinParticipants == max)
        {
            return $"TYLKO {MinParticipants} os.";
        }

        return MinParticipants > 1
            ? $"Grupa {MinParticipants}–{max} os."
            : $"1–{max} os.";
    }

    public void SetFeatureValue(int featureId, string normalizedValue)
    {
        DomainValueHelper.UpsertFeatureAssignment(Features, featureId, normalizedValue);
    }

    public bool RemoveFeatureValue(int featureId)
    {
        return DomainValueHelper.RemoveFeatureAssignment(Features, featureId);
    }

    public string? GetDirectFeatureValue(int featureId)
    {
        return DomainValueHelper.GetFeatureValue(Features, featureId);
    }

    public Dictionary<int, EffectiveFeatureValue> GetEffectiveFeatures(
        Attraction? parentAttraction = null,
        IEnumerable<Addon>? selectedAddons = null,
        IReadOnlyCollection<FeatureDefinition>? featureDefinitions = null,
        string? offerRolePrefix = null)
    {
        var resolvedAttraction = parentAttraction ?? InMemoryData.Attractions.FirstOrDefault(a => a.Id == AttractionId);
        var definitions = featureDefinitions ?? InMemoryData.Features;
        var definitionsById = definitions.ToDictionary(d => d.Id);

        string offerSourceLabel = string.IsNullOrWhiteSpace(offerRolePrefix)
            ? $"Oferta \"{Name}\""
            : $"{offerRolePrefix}: \"{Name}\"";

        var effective = new Dictionary<int, EffectiveFeatureValue>();

        if (resolvedAttraction is not null)
        {
            foreach (var attractionFeature in resolvedAttraction.Features)
            {
                effective[attractionFeature.FeatureId] = new EffectiveFeatureValue(
                    attractionFeature.FeatureId,
                    attractionFeature.Value,
                    $"{offerSourceLabel} (cecha domyślna z atrakcji \"{resolvedAttraction.Name}\")");
            }
        }

        foreach (var offerFeature in Features)
        {
            bool overridesAttraction = effective.ContainsKey(offerFeature.FeatureId);
            effective[offerFeature.FeatureId] = new EffectiveFeatureValue(
                offerFeature.FeatureId,
                offerFeature.Value,
                offerSourceLabel,
                overridesAttractionDefault: overridesAttraction);
        }

        if (selectedAddons is not null)
        {
            var addonList = selectedAddons.ToList();
            string addonContext = $"do oferty \"{Name}\"";

            foreach (var addon in addonList)
            {
                addon.ApplyToEffectiveFeatures(effective, definitionsById, AddonEffectMode.ProvideAccommodation, addonContext);
            }

            foreach (var addon in addonList)
            {
                addon.ApplyToEffectiveFeatures(effective, definitionsById, AddonEffectMode.TightenRequirement, addonContext);
            }
        }

        return effective;
    }

    public CompatibilityReport ValidateAgainst(ParticipantProfile profile)
    {
        var attraction = InMemoryData.Attractions.FirstOrDefault(a => a.Id == AttractionId);
        return ValidateAgainst(profile, attraction, selectedAddons: null, InMemoryData.Features);
    }

    public CompatibilityReport ValidateAgainst(
        ParticipantProfile profile,
        Attraction? parentAttraction,
        IEnumerable<Addon>? selectedAddons = null,
        IReadOnlyCollection<FeatureDefinition>? featureDefinitions = null,
        bool includeSourceInMessages = false,
        string? offerRolePrefix = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var resolvedAttraction = parentAttraction ?? InMemoryData.Attractions.FirstOrDefault(a => a.Id == AttractionId);
        var definitions = featureDefinitions ?? InMemoryData.Features;
        var effectiveFeatures = GetEffectiveFeatures(resolvedAttraction, selectedAddons, definitions, offerRolePrefix);
        string fallbackSource = string.IsNullOrWhiteSpace(offerRolePrefix)
            ? $"Oferta \"{Name}\""
            : $"{offerRolePrefix}: \"{Name}\"";

        var report = FeatureSetEvaluator.Evaluate(
            targetName: Name,
            targetTypeLabel: "OFERTY",
            fallbackMissingFacilitySource: fallbackSource,
            effectiveFeatures: effectiveFeatures,
            profile: profile,
            definitions: definitions,
            includeSourceInMessages: includeSourceInMessages);

        string groupSource = includeSourceInMessages ? fallbackSource : string.Empty;
        EvaluateGroupSizeAgainstCount(
            profile.ParticipantsCount,
            resolvedAttraction,
            groupSource,
            report.MetRequirements,
            report.UnmetRequirements);

        return report;
    }

    public void EvaluateGroupSizeAgainstCount(
        int participantsCount,
        Attraction? resolvedAttraction,
        string sourceLabel,
        List<RequirementCheckResult> metBucket,
        List<RequirementCheckResult> unmetBucket)
    {
        int effectiveMax = GetEffectiveMaxParticipants(resolvedAttraction);
        string limitDesc = FormatGroupSizeLimit(resolvedAttraction);

        if (participantsCount < MinParticipants)
        {
            string msg = MinParticipants == effectiveMax
                ? $"Oferta przeznaczona wyłącznie dla grupy dokładnie {MinParticipants} os. (liczebność uczestników: {participantsCount} os.)."
                : $"Oferta grupowa wymaga co najmniej {MinParticipants} os. (liczebność uczestników: {participantsCount} os.).";

            unmetBucket.Insert(0, new RequirementCheckResult(
                featureId: -1,
                featureName: "Wymagana liczebność grupy",
                status: RequirementEvaluationStatus.Unmet,
                requiredValueText: limitDesc,
                participantValueText: $"{participantsCount} os.",
                message: msg,
                sourceElement: sourceLabel));
            return;
        }

        if (participantsCount > effectiveMax)
        {
            string maxOrigin = MaxParticipants.HasValue && (resolvedAttraction is null || MaxParticipants.Value <= resolvedAttraction.MaxParticipants)
                ? $"limit oferty: maks. {effectiveMax} os."
                : $"pojemność atrakcji: maks. {effectiveMax} os.";

            unmetBucket.Insert(0, new RequirementCheckResult(
                featureId: -2,
                featureName: "Maksymalna liczba osób",
                status: RequirementEvaluationStatus.Unmet,
                requiredValueText: limitDesc,
                participantValueText: $"{participantsCount} os.",
                message: $"Przekroczono maksymalną liczbę osób ({maxOrigin}; liczebność uczestników: {participantsCount} os.).",
                sourceElement: sourceLabel));
            return;
        }

        metBucket.Insert(0, new RequirementCheckResult(
            featureId: -1,
            featureName: "Liczebność uczestników / grupy",
            status: RequirementEvaluationStatus.Met,
            requiredValueText: limitDesc,
            participantValueText: $"{participantsCount} os.",
            message: $"Liczebność uczestników ({participantsCount} os.) mieści się w wymaganym przedziale ({limitDesc}).",
            sourceElement: sourceLabel));
    }
}
