using Attractions.Helpers;

namespace Attractions.Models;

public class ParticipantProfile
{
    public List<int> ParticipantAges { get; }
    public decimal? MinParticipantHeightCm { get; }
    public bool RequiresWheelchairAccess { get; }
    public int DisabledParticipantsCount { get; }
    public string? MaxPhysicalDemand { get; }
    public bool? ExplicitAdultGuardianPresent { get; }
    public bool RequiresChildFriendly { get; }
    public bool RequiresStrollerAccess { get; }
    public bool RequiresElevator { get; }

    public HashSet<int> RequiredFacilityFeatureIds { get; }
    public HashSet<int> ConfirmedConditionFeatureIds { get; }
    public Dictionary<int, List<decimal>> CustomNumericValues { get; }
    public Dictionary<int, string> CustomOptionOrTextValues { get; }

    public ParticipantProfile(
        IEnumerable<int> participantAges,
        decimal? minParticipantHeightCm = null,
        bool requiresWheelchairAccess = false,
        int disabledParticipantsCount = 0,
        string? maxPhysicalDemand = null,
        bool? explicitAdultGuardianPresent = null,
        bool requiresChildFriendly = false,
        bool requiresStrollerAccess = false,
        bool requiresElevator = false,
        IEnumerable<int>? requiredFacilityFeatureIds = null,
        IEnumerable<int>? confirmedConditionFeatureIds = null,
        IDictionary<int, List<decimal>>? customNumericValues = null,
        IDictionary<int, string>? customOptionOrTextValues = null)
    {
        ArgumentNullException.ThrowIfNull(participantAges);

        ParticipantAges = new List<int>(participantAges);
        MinParticipantHeightCm = minParticipantHeightCm;
        RequiresWheelchairAccess = requiresWheelchairAccess;
        DisabledParticipantsCount = Math.Max(0, disabledParticipantsCount);
        MaxPhysicalDemand = string.IsNullOrWhiteSpace(maxPhysicalDemand) ? null : maxPhysicalDemand;
        ExplicitAdultGuardianPresent = explicitAdultGuardianPresent;
        RequiresChildFriendly = requiresChildFriendly;
        RequiresStrollerAccess = requiresStrollerAccess;
        RequiresElevator = requiresElevator;

        RequiredFacilityFeatureIds = requiredFacilityFeatureIds is not null
            ? new HashSet<int>(requiredFacilityFeatureIds)
            : new HashSet<int>();

        ConfirmedConditionFeatureIds = confirmedConditionFeatureIds is not null
            ? new HashSet<int>(confirmedConditionFeatureIds)
            : new HashSet<int>();

        CustomNumericValues = customNumericValues is not null
            ? customNumericValues.ToDictionary(kv => kv.Key, kv => new List<decimal>(kv.Value))
            : new Dictionary<int, List<decimal>>();

        CustomOptionOrTextValues = customOptionOrTextValues is not null
            ? new Dictionary<int, string>(customOptionOrTextValues)
            : new Dictionary<int, string>();
    }

    public int ParticipantsCount => ParticipantAges.Count > 0 ? ParticipantAges.Count : 1;
    public bool IsGroup => ParticipantAges.Count > 1;
    public int? YoungestParticipantAge => ParticipantAges.Count > 0 ? ParticipantAges.Min() : null;
    public int? OldestParticipantAge => ParticipantAges.Count > 0 ? ParticipantAges.Max() : null;
    public bool HasMinorParticipants => ParticipantAges.Any(age => age < 18);
    public bool HasAdultParticipants => ParticipantAges.Any(age => age >= 18);

    public bool EffectiveHasAdultGuardian
    {
        get
        {
            if (ExplicitAdultGuardianPresent.HasValue)
            {
                return ExplicitAdultGuardianPresent.Value;
            }

            if (ParticipantAges.Count > 0 && !HasMinorParticipants)
            {
                return true;
            }

            return HasAdultParticipants;
        }
    }

    public bool RequiresFacility(FeatureDefinition feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (RequiredFacilityFeatureIds.Contains(feature.Id))
        {
            return true;
        }

        return feature.ProfileBindingKey switch
        {
            "wheelchair" => RequiresWheelchairAccess,
            "child_friendly" => RequiresChildFriendly,
            "stroller_access" => RequiresStrollerAccess,
            "elevator" => RequiresElevator,
            _ => false
        };
    }

    public bool SatisfiesCondition(FeatureDefinition feature, out string participantDetail)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (feature.ProfileBindingKey == "guardian_required")
        {
            if (ParticipantAges.Count > 0 && !HasMinorParticipants)
            {
                participantDetail = $"wszyscy uczestnicy są pełnoletni (min. {YoungestParticipantAge} lat)";
                return true;
            }

            if (EffectiveHasAdultGuardian)
            {
                participantDetail = HasAdultParticipants
                    ? "w grupie znajduje się osoba pełnoletnia"
                    : "potwierdzono obecność opiekuna";
                return true;
            }

            participantDetail = YoungestParticipantAge.HasValue
                ? $"uczestnik niepełnoletni ({YoungestParticipantAge} lat) bez opiekuna"
                : "brak pełnoletniego opiekuna";
            return false;
        }

        bool confirmed = ConfirmedConditionFeatureIds.Contains(feature.Id);
        participantDetail = confirmed ? "potwierdzono w profilu" : "nie potwierdzono w profilu";
        return confirmed;
    }

    public decimal? GetNumericMetric(FeatureDefinition feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        switch (feature.ProfileBindingKey)
        {
            case "min_age" when YoungestParticipantAge.HasValue:
                return YoungestParticipantAge.Value;
            case "max_age" when OldestParticipantAge.HasValue:
                return OldestParticipantAge.Value;
            case "min_height" when MinParticipantHeightCm.HasValue:
                return MinParticipantHeightCm.Value;
        }

        if (CustomNumericValues.TryGetValue(feature.Id, out var customList) && customList.Count > 0)
        {
            return feature.EffectiveRule == FeatureComparisonRule.MaxThreshold
                ? customList.Max()
                : customList.Min();
        }

        return null;
    }

    public string? GetOptionOrTextPreference(FeatureDefinition feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (feature.ProfileBindingKey == "physical_demand" && !string.IsNullOrWhiteSpace(MaxPhysicalDemand))
        {
            return MaxPhysicalDemand;
        }

        if (CustomOptionOrTextValues.TryGetValue(feature.Id, out string? customValue) &&
            !string.IsNullOrWhiteSpace(customValue))
        {
            return customValue;
        }

        return null;
    }

    public ParticipantGroupBreakdown ToGroupBreakdown()
    {
        return ParticipantGroupBreakdown.FromParticipantProfile(this);
    }

    public string GetShortSummary(IReadOnlyCollection<FeatureDefinition> definitions)
    {
        var parts = new List<string>();

        if (ParticipantAges.Count == 1)
        {
            parts.Add($"1 os. ({ParticipantAges[0]} lat)");
        }
        else if (ParticipantAges.Count > 1)
        {
            parts.Add($"Grupa {ParticipantAges.Count} os. (wiek: {string.Join(", ", ParticipantAges)} lat)");
        }
        else
        {
            parts.Add("Wiek: nie podano");
        }

        if (MinParticipantHeightCm.HasValue)
        {
            parts.Add($"Min. wzrost: {DomainValueHelper.FormatDecimal(MinParticipantHeightCm.Value)} cm");
        }

        if (RequiresWheelchairAccess) parts.Add("Wózek inwalidzki: TAK");
        if (!string.IsNullOrWhiteSpace(MaxPhysicalDemand)) parts.Add($"Maks. wysiłek: {MaxPhysicalDemand}");
        if (RequiresChildFriendly) parts.Add("Dla dzieci");
        if (RequiresStrollerAccess) parts.Add("Wózek dziecięcy");
        if (RequiresElevator) parts.Add("Winda");

        var extraFacilities = definitions
            .Where(d => RequiredFacilityFeatureIds.Contains(d.Id) &&
                        d.ProfileBindingKey is not ("wheelchair" or "child_friendly" or "stroller_access" or "elevator"))
            .Select(d => d.Name)
            .ToList();

        if (extraFacilities.Count > 0)
        {
            parts.Add($"Udogodnienia: {string.Join(", ", extraFacilities)}");
        }

        int customCount = CustomNumericValues.Count +
                          CustomOptionOrTextValues.Count +
                          ConfirmedConditionFeatureIds.Count;
        if (customCount > 0)
        {
            parts.Add($"Dodatkowe kryteria: {customCount}");
        }

        return string.Join(" | ", parts);
    }
}
