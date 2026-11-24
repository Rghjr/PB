using Attractions.Helpers;

namespace Attractions.Models;

public class Addon
{
    public int Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public List<FeatureAssignment> FeatureModifiers { get; private set; }

    public Addon(
        int id,
        string name,
        string description,
        decimal price,
        IEnumerable<FeatureAssignment>? featureModifiers = null)
    {
        Id = id;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Price = decimal.Round(price, 2);
        FeatureModifiers = featureModifiers is not null
            ? featureModifiers.Select(m => m.Clone()).ToList()
            : new List<FeatureAssignment>();
    }

    public void UpdateDetails(
        string name,
        string description,
        decimal price,
        IEnumerable<FeatureAssignment>? featureModifiers = null)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        Price = decimal.Round(price, 2);

        if (featureModifiers is not null)
        {
            ReplaceFeatureModifiers(featureModifiers);
        }
    }

    public void ReplaceFeatureModifiers(IEnumerable<FeatureAssignment> modifiers)
    {
        FeatureModifiers = modifiers.Select(m => m.Clone()).ToList();
    }

    public void SetFeatureModifier(
        int featureId,
        string normalizedValue,
        AddonEffectMode effectMode = AddonEffectMode.TightenRequirement)
    {
        DomainValueHelper.UpsertFeatureAssignment(FeatureModifiers, featureId, normalizedValue, effectMode);
    }

    public bool RemoveFeatureModifier(int featureId)
    {
        return DomainValueHelper.RemoveFeatureAssignment(FeatureModifiers, featureId);
    }

    public void ApplyToEffectiveFeatures(
        IDictionary<int, EffectiveFeatureValue> offerFeatures,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById,
        AddonEffectMode modeToApply,
        string? offerContextLabel = null)
    {
        ArgumentNullException.ThrowIfNull(offerFeatures);
        ArgumentNullException.ThrowIfNull(definitionsById);

        string sourceLabel = string.IsNullOrWhiteSpace(offerContextLabel)
            ? $"Dodatek \"{Name}\""
            : $"Dodatek \"{Name}\" ({offerContextLabel})";

        foreach (var modifier in FeatureModifiers.Where(m => m.EffectMode == modeToApply))
        {
            if (!definitionsById.TryGetValue(modifier.FeatureId, out var definition))
            {
                continue;
            }

            if (modeToApply == AddonEffectMode.ProvideAccommodation)
            {
                offerFeatures[modifier.FeatureId] = new EffectiveFeatureValue(
                    modifier.FeatureId,
                    modifier.Value,
                    $"{sourceLabel} – udogodnienie",
                    modifiedByAddon: true);
                continue;
            }

            if (!offerFeatures.TryGetValue(modifier.FeatureId, out var existingEntry))
            {
                offerFeatures[modifier.FeatureId] = new EffectiveFeatureValue(
                    modifier.FeatureId,
                    modifier.Value,
                    sourceLabel,
                    modifiedByAddon: true);
                continue;
            }

            string stricter = definition.SelectStricterValue(
                existingEntry.Value,
                modifier.Value,
                out bool addonMadeStricter);

            if (addonMadeStricter)
            {
                offerFeatures[modifier.FeatureId] = new EffectiveFeatureValue(
                    modifier.FeatureId,
                    stricter,
                    sourceLabel,
                    overridesAttractionDefault: existingEntry.OverridesAttractionDefault,
                    modifiedByAddon: true);
            }
        }
    }
}
