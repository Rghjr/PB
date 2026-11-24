using Attractions.Data;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.DTOs;

public class OfferConfiguration
{
    public Offer BaseOffer { get; }
    public List<Addon> SelectedAddons { get; }
    public List<SelectedRelatedOffer> SelectedRelatedOffers { get; }

    public OfferConfiguration(
        Offer baseOffer,
        IEnumerable<Addon>? selectedAddons = null,
        IEnumerable<SelectedRelatedOffer>? selectedRelatedOffers = null)
    {
        BaseOffer = baseOffer ?? throw new ArgumentNullException(nameof(baseOffer));
        SelectedAddons = selectedAddons is not null ? new List<Addon>(selectedAddons) : new List<Addon>();
        SelectedRelatedOffers = selectedRelatedOffers is not null
            ? new List<SelectedRelatedOffer>(selectedRelatedOffers)
            : new List<SelectedRelatedOffer>();
    }

    public decimal BasePrice => BaseOffer.Price;
    public decimal BaseAddonsPrice => SelectedAddons.Sum(a => a.Price);
    public decimal RelatedOffersBasePrice => SelectedRelatedOffers.Sum(r => r.BasePrice);
    public decimal RelatedOffersAddonsPrice => SelectedRelatedOffers.Sum(r => r.AddonsPrice);
    public decimal TotalPrice => BasePrice + BaseAddonsPrice + SelectedRelatedOffers.Sum(r => r.Subtotal);

    public PackagePriceBreakdown CalculatePriceBreakdown(ParticipantGroupBreakdown groupBreakdown)
    {
        return PackagePriceBreakdown.Calculate(TotalPrice, groupBreakdown);
    }

    public (int MinParticipants, int MaxParticipants, string MinSource, string MaxSource) GetPackageGroupSizeBounds(
        Func<int, Attraction?>? attractionResolver = null)
    {
        var resolveAttraction = attractionResolver ?? (id => InMemoryData.Attractions.FirstOrDefault(a => a.Id == id));

        var baseAttr = resolveAttraction(BaseOffer.AttractionId);
        int maxMin = BaseOffer.MinParticipants;
        string minSource = $"Oferta główna: \"{BaseOffer.Name}\"";

        int minMax = BaseOffer.GetEffectiveMaxParticipants(baseAttr);
        string maxSource = $"Oferta główna: \"{BaseOffer.Name}\"";

        foreach (var rel in SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
        {
            var relAttr = resolveAttraction(rel.Offer.AttractionId);
            if (rel.Offer.MinParticipants > maxMin)
            {
                maxMin = rel.Offer.MinParticipants;
                minSource = $"Oferta powiązana: \"{rel.Offer.Name}\"";
            }

            int relEffectiveMax = rel.Offer.GetEffectiveMaxParticipants(relAttr);
            if (relEffectiveMax < minMax)
            {
                minMax = relEffectiveMax;
                maxSource = $"Oferta powiązana: \"{rel.Offer.Name}\"";
            }
        }

        return (maxMin, minMax, minSource, maxSource);
    }

    public Dictionary<int, EffectiveFeatureValue> GetPackageEffectiveFeatures(
        IReadOnlyCollection<FeatureDefinition>? featureDefinitions = null,
        Func<int, Attraction?>? attractionResolver = null)
    {
        var definitions = featureDefinitions ?? InMemoryData.Features;
        var definitionsById = definitions.ToDictionary(d => d.Id);
        var resolveAttraction = attractionResolver ?? (id => InMemoryData.Attractions.FirstOrDefault(a => a.Id == id));

        var itemFeatureSets = BuildPerOfferFeatureSets(definitions, resolveAttraction);
        if (itemFeatureSets.Count == 0)
        {
            return new Dictionary<int, EffectiveFeatureValue>();
        }

        var aggregated = new Dictionary<int, EffectiveFeatureValue>();
        var allPresentFeatureIds = itemFeatureSets
            .SelectMany(s => s.Features.Keys)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        foreach (int featureId in allPresentFeatureIds)
        {
            if (!definitionsById.TryGetValue(featureId, out var definition))
            {
                continue;
            }

            if (definition.EffectiveRule == FeatureComparisonRule.FacilityAvailable)
            {
                var firstMissingOrFalse = itemFeatureSets.FirstOrDefault(item =>
                    !item.Features.TryGetValue(featureId, out var val) ||
                    !(DomainValueHelper.TryParseBooleanFlexible(val.Value, out bool b) && b));

                if (firstMissingOrFalse.Features is not null)
                {
                    string reasonSource = firstMissingOrFalse.Features.TryGetValue(featureId, out var explicitFalse)
                        ? explicitFalse.SourceElement
                        : firstMissingOrFalse.OfferLabel;

                    aggregated[featureId] = new EffectiveFeatureValue(featureId, "false", reasonSource);
                }
                else
                {
                    string source = itemFeatureSets.Count > 1
                        ? "Wszystkie pozycje pakietu"
                        : itemFeatureSets[0].Features[featureId].SourceElement;
                    aggregated[featureId] = new EffectiveFeatureValue(featureId, "true", source);
                }

                continue;
            }

            EffectiveFeatureValue? strictestEntry = null;
            foreach (var item in itemFeatureSets)
            {
                if (!item.Features.TryGetValue(featureId, out var candidate))
                {
                    continue;
                }

                if (strictestEntry is null)
                {
                    strictestEntry = candidate.Clone();
                    continue;
                }

                string combined = definition.SelectStricterValue(
                    strictestEntry.Value,
                    candidate.Value,
                    out bool candidateIsStricter);

                if (candidateIsStricter)
                {
                    strictestEntry = new EffectiveFeatureValue(
                        featureId,
                        combined,
                        candidate.SourceElement,
                        candidate.OverridesAttractionDefault,
                        candidate.ModifiedByAddon);
                }
            }

            if (strictestEntry is not null)
            {
                aggregated[featureId] = strictestEntry;
            }
        }

        return aggregated;
    }

    public CompatibilityReport ValidateAgainst(ParticipantProfile profile)
    {
        return ValidateAgainst(
            profile,
            InMemoryData.Features,
            id => InMemoryData.Attractions.FirstOrDefault(a => a.Id == id));
    }

    public CompatibilityReport ValidateAgainst(
        ParticipantProfile profile,
        IReadOnlyCollection<FeatureDefinition> featureDefinitions,
        Func<int, Attraction?> attractionResolver)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(featureDefinitions);
        ArgumentNullException.ThrowIfNull(attractionResolver);

        var allUnmet = new List<RequirementCheckResult>();
        var allWarnings = new List<RequirementCheckResult>();

        var baseAttraction = attractionResolver(BaseOffer.AttractionId);
        var baseReport = BaseOffer.ValidateAgainst(
            profile,
            baseAttraction,
            SelectedAddons,
            featureDefinitions,
            includeSourceInMessages: true,
            offerRolePrefix: "Oferta główna");

        allUnmet.AddRange(baseReport.UnmetRequirements);
        allWarnings.AddRange(baseReport.Warnings);

        foreach (var relatedSelection in SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
        {
            var relatedAttraction = attractionResolver(relatedSelection.Offer.AttractionId);
            var relatedReport = relatedSelection.Offer.ValidateAgainst(
                profile,
                relatedAttraction,
                relatedSelection.SelectedAddons,
                featureDefinitions,
                includeSourceInMessages: true,
                offerRolePrefix: "Oferta powiązana");

            allUnmet.AddRange(relatedReport.UnmetRequirements);
            allWarnings.AddRange(relatedReport.Warnings);
        }

        var aggregatedPackageFeatures = GetPackageEffectiveFeatures(featureDefinitions, attractionResolver);
        var packageAggregateReport = FeatureSetEvaluator.Evaluate(
            targetName: BuildPackageLabel(),
            targetTypeLabel: "PAKIETU OFERT",
            fallbackMissingFacilitySource: "Pakiet ofert",
            effectiveFeatures: aggregatedPackageFeatures,
            profile: profile,
            definitions: featureDefinitions,
            includeSourceInMessages: true);

        var (pkgMin, pkgMax, minSrc, maxSrc) = GetPackageGroupSizeBounds(attractionResolver);
        if (pkgMin > pkgMax)
        {
            allUnmet.Insert(0, new RequirementCheckResult(
                featureId: -3,
                featureName: "Sprzeczność limitów grupowych w pakiecie",
                status: RequirementEvaluationStatus.Unmet,
                requiredValueText: $"min. {pkgMin} vs maks. {pkgMax}",
                participantValueText: $"{profile.ParticipantsCount} os.",
                message: $"Wybrane oferty mają wykluczające się limity wielkości grupy: {minSrc} wymaga min. {pkgMin} os., natomiast {maxSrc} dopuszcza maks. {pkgMax} os.",
                sourceElement: "Pakiet ofert"));
        }
        else if (profile.ParticipantsCount >= pkgMin && profile.ParticipantsCount <= pkgMax)
        {
            string rangeText = pkgMin > 1 && pkgMin == pkgMax
                ? $"TYLKO {pkgMin} os."
                : $"{pkgMin}–{pkgMax} os.";

            packageAggregateReport.MetRequirements.Insert(0, new RequirementCheckResult(
                featureId: -1,
                featureName: "Liczebność uczestników / grupy w pakiecie",
                status: RequirementEvaluationStatus.Met,
                requiredValueText: rangeText,
                participantValueText: $"{profile.ParticipantsCount} os.",
                message: $"Liczebność grupy ({profile.ParticipantsCount} os.) mieści się w dopuszczalnym przedziale pakietu ({rangeText}).",
                sourceElement: pkgMin > 1 ? minSrc : maxSrc));
        }

        var deduplicatedWarnings = allWarnings
            .GroupBy(w => (w.FeatureId, w.SourceElement))
            .Select(g => g.First())
            .ToList();

        return new CompatibilityReport(
            BuildPackageLabel(),
            "PAKIETU OFERT",
            packageAggregateReport.MetRequirements,
            allUnmet,
            deduplicatedWarnings);
    }

    private List<(string OfferLabel, Dictionary<int, EffectiveFeatureValue> Features)> BuildPerOfferFeatureSets(
        IReadOnlyCollection<FeatureDefinition> definitions,
        Func<int, Attraction?> attractionResolver)
    {
        var sets = new List<(string OfferLabel, Dictionary<int, EffectiveFeatureValue> Features)>();

        var baseAttraction = attractionResolver(BaseOffer.AttractionId);
        var baseFeatures = BaseOffer.GetEffectiveFeatures(
            baseAttraction,
            SelectedAddons,
            definitions,
            offerRolePrefix: "Oferta główna");

        sets.Add(($"Oferta główna: \"{BaseOffer.Name}\"", baseFeatures));

        foreach (var related in SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
        {
            var relAttraction = attractionResolver(related.Offer.AttractionId);
            var relFeatures = related.Offer.GetEffectiveFeatures(
                relAttraction,
                related.SelectedAddons,
                definitions,
                offerRolePrefix: "Oferta powiązana");

            sets.Add(($"Oferta powiązana: \"{related.Offer.Name}\"", relFeatures));
        }

        return sets;
    }

    private string BuildPackageLabel()
    {
        int extraCount = SelectedRelatedOffers.Count;
        return extraCount > 0
            ? $"{BaseOffer.Name} (+ {extraCount} powiązane)"
            : BaseOffer.Name;
    }
}
