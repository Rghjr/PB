using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.Services;

public record FeatureUsageInfo(
    List<Attraction> Attractions,
    List<Offer> Offers,
    List<(Offer ParentOffer, Addon Addon)> Addons)
{
    public int TotalCount => Attractions.Count + Offers.Count + Addons.Count;
    public bool IsUsed => TotalCount > 0;
}

public class FeatureService
{
    public List<FeatureDefinition> GetAllFeatures()
    {
        return InMemoryData.Features
            .OrderBy(f => f.Id)
            .ToList();
    }

    public FeatureDefinition? GetFeatureById(int id)
    {
        return InMemoryData.Features.FirstOrDefault(f => f.Id == id);
    }

    public ServiceResult<FeatureDefinition> AddFeature(
        string name,
        FeatureValueType type,
        string description,
        FeatureComparisonRule comparisonRule,
        string? unit = null,
        IEnumerable<string>? allowedValues = null)
    {
        string cleanName = DomainValueHelper.CleanText(name);
        if (cleanName.Length == 0)
        {
            return ServiceResult<FeatureDefinition>.Fail("Nazwa cechy nie może być pusta.");
        }

        if (IsDuplicateFeatureName(cleanName))
        {
            return ServiceResult<FeatureDefinition>.Fail($"Cecha o nazwie \"{cleanName}\" już istnieje w słowniku.");
        }

        if (!TryPrepareAllowedOptions(type, allowedValues, out var normalizedOptions, out string optionsError))
        {
            return ServiceResult<FeatureDefinition>.Fail(optionsError);
        }

        var feature = new FeatureDefinition(
            id: InMemoryData.GetNextFeatureId(),
            name: cleanName,
            type: type,
            description: DomainValueHelper.CleanText(description),
            comparisonRule: comparisonRule,
            unit: type == FeatureValueType.Number ? DomainValueHelper.CleanNullableText(unit) : null,
            allowedValues: normalizedOptions);

        InMemoryData.Features.Add(feature);

        if (!JsonFeatureStore.TrySaveFeatures(out string saveError))
        {
            return ServiceResult<FeatureDefinition>.Ok(
                feature,
                $"Dodano cechę \"{feature.Name}\" w pamięci, lecz wystąpił błąd zapisu JSON: {saveError}");
        }

        return ServiceResult<FeatureDefinition>.Ok(
            feature,
            $"Dodano nową cechę \"{feature.Name}\" (ID: {feature.Id}, Typ: {feature.Type}) do pliku Features.json.");
    }

    public ServiceResult UpdateFeature(
        int id,
        string name,
        string description,
        FeatureComparisonRule comparisonRule,
        string? unit = null,
        IEnumerable<string>? allowedValues = null)
    {
        var existing = GetFeatureById(id);
        if (existing is null)
        {
            return ServiceResult.Fail($"Cecha o ID {id} nie istnieje w słowniku.");
        }

        string cleanName = DomainValueHelper.CleanText(name);
        if (cleanName.Length == 0)
        {
            return ServiceResult.Fail("Nazwa cechy nie może być pusta.");
        }

        if (IsDuplicateFeatureName(cleanName, excludedId: id))
        {
            return ServiceResult.Fail($"Inna cecha posiada już nazwę \"{cleanName}\".");
        }

        List<string>? normalizedOptions = null;
        if (existing.Type == FeatureValueType.OptionList && allowedValues is not null)
        {
            if (!TryPrepareAllowedOptions(existing.Type, allowedValues, out normalizedOptions, out string optionsError))
            {
                return ServiceResult.Fail(optionsError);
            }
        }

        existing.UpdateDefinition(
            cleanName,
            DomainValueHelper.CleanText(description),
            comparisonRule,
            DomainValueHelper.CleanNullableText(unit),
            normalizedOptions);

        return !JsonFeatureStore.TrySaveFeatures(out string saveError)
            ? ServiceResult.Fail($"Zaktualizowano cechę w pamięci, ale zapis do Features.json nie powiódł się: {saveError}")
            : ServiceResult.Ok($"Zaktualizowano definicję cechy \"{existing.Name}\" (ID: {existing.Id}) w Features.json.");
    }

    public FeatureUsageInfo GetFeatureUsage(int featureId)
    {
        var usedByAttractions = InMemoryData.Attractions
            .Where(a => a.Features.Any(f => f.FeatureId == featureId))
            .OrderBy(a => a.Id)
            .ToList();

        var usedByOffers = InMemoryData.Offers
            .Where(o => o.Features.Any(f => f.FeatureId == featureId))
            .OrderBy(o => o.Id)
            .ToList();

        var usedByAddons = InMemoryData.Offers
            .SelectMany(o => o.Addons.Select(a => (ParentOffer: o, Addon: a)))
            .Where(pair => pair.Addon.FeatureModifiers.Any(m => m.FeatureId == featureId))
            .OrderBy(pair => pair.Addon.Id)
            .ToList();

        return new FeatureUsageInfo(usedByAttractions, usedByOffers, usedByAddons);
    }

    public ServiceResult DeleteFeature(int featureId, bool removeFromAssignedObjects = false)
    {
        var feature = GetFeatureById(featureId);
        if (feature is null)
        {
            return ServiceResult.Fail($"Cecha o ID {featureId} nie istnieje w słowniku.");
        }

        var usage = GetFeatureUsage(featureId);
        if (usage.IsUsed && !removeFromAssignedObjects)
        {
            return ServiceResult.Fail(
                $"Cecha \"{feature.Name}\" jest aktualnie wykorzystywana (Atrakcje: {usage.Attractions.Count}, " +
                $"Oferty: {usage.Offers.Count}, Dodatki: {usage.Addons.Count}). Potwierdź kaskadowe usunięcie przypisań lub odłącz cechę.");
        }

        if (usage.IsUsed)
        {
            usage.Attractions.ForEach(a => a.RemoveFeatureValue(featureId));
            usage.Offers.ForEach(o => o.RemoveFeatureValue(featureId));
            foreach (var (_, addon) in usage.Addons)
            {
                addon.RemoveFeatureModifier(featureId);
            }

            JsonFeatureStore.TrySaveAssignments(out _);
        }

        InMemoryData.Features.Remove(feature);

        if (!JsonFeatureStore.TrySaveFeatures(out string saveError))
        {
            return ServiceResult.Fail($"Usunięto cechę z pamięci, ale zapis do Features.json nie powiódł się: {saveError}");
        }

        string usageSuffix = usage.IsUsed
            ? $" Usunięto także jej przypisania z obiektów ({usage.TotalCount})."
            : string.Empty;

        return ServiceResult.Ok($"Usunięto cechę \"{feature.Name}\" (ID: {featureId}) ze słownika Features.json.{usageSuffix}");
    }

    public ServiceResult ValidateAndNormalizeAssignments(
        IEnumerable<FeatureAssignment> rawAssignments,
        out List<FeatureAssignment> normalizedList)
    {
        normalizedList = new List<FeatureAssignment>();
        var seenIds = new HashSet<int>();

        foreach (var item in rawAssignments)
        {
            var definition = GetFeatureById(item.FeatureId);
            if (definition is null)
            {
                return ServiceResult.Fail($"Cecha o ID {item.FeatureId} nie istnieje w słowniku Features.json.");
            }

            if (!definition.TryNormalizeValue(item.Value, out string normalizedVal, out string error))
            {
                return ServiceResult.Fail(error);
            }

            if (seenIds.Contains(item.FeatureId))
            {
                normalizedList.RemoveAll(x => x.FeatureId == item.FeatureId);
            }

            seenIds.Add(item.FeatureId);
            normalizedList.Add(new FeatureAssignment(item.FeatureId, normalizedVal, item.EffectMode));
        }

        return ServiceResult.Ok("Przypisania cech są poprawne.");
    }

    private static bool IsDuplicateFeatureName(string cleanName, int? excludedId = null)
    {
        return InMemoryData.Features.Any(f =>
            (!excludedId.HasValue || f.Id != excludedId.Value) &&
            string.Equals(f.Name, cleanName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryPrepareAllowedOptions(
        FeatureValueType type,
        IEnumerable<string>? rawValues,
        out List<string>? normalizedOptions,
        out string errorMessage)
    {
        normalizedOptions = null;
        errorMessage = string.Empty;

        if (type != FeatureValueType.OptionList)
        {
            return true;
        }

        normalizedOptions = (rawValues ?? Enumerable.Empty<string>())
            .Select(DomainValueHelper.CleanText)
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedOptions.Count < 2)
        {
            errorMessage = "Cecha typu OptionList musi posiadać co najmniej 2 dozwolone wartości (np. Niski, Średni, Wysoki).";
            return false;
        }

        return true;
    }
}
