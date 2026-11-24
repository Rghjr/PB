using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.Data;

public class FeaturesFileDocument
{
    [JsonPropertyName("features")]
    public List<FeatureDefinition> Features { get; }

    [JsonConstructor]
    public FeaturesFileDocument(List<FeatureDefinition>? features = null)
    {
        Features = features ?? new List<FeatureDefinition>();
    }
}

public class OfferGroupLimitSnapshot
{
    [JsonPropertyName("minParticipants")]
    public int MinParticipants { get; }

    [JsonPropertyName("maxParticipants")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxParticipants { get; }

    [JsonConstructor]
    public OfferGroupLimitSnapshot(int minParticipants = 1, int? maxParticipants = null)
    {
        MinParticipants = minParticipants;
        MaxParticipants = maxParticipants;
    }
}

public class FeatureAssignmentsFileDocument
{
    [JsonPropertyName("attractionFeatures")]
    public Dictionary<int, List<FeatureAssignment>> AttractionFeatures { get; }

    [JsonPropertyName("attractionMaxParticipants")]
    public Dictionary<int, int> AttractionMaxParticipants { get; }

    [JsonPropertyName("offerFeatures")]
    public Dictionary<int, List<FeatureAssignment>> OfferFeatures { get; }

    [JsonPropertyName("offerGroupLimits")]
    public Dictionary<int, OfferGroupLimitSnapshot> OfferGroupLimits { get; }

    [JsonPropertyName("addonFeatureModifiers")]
    public Dictionary<int, List<FeatureAssignment>> AddonFeatureModifiers { get; }

    [JsonConstructor]
    public FeatureAssignmentsFileDocument(
        Dictionary<int, List<FeatureAssignment>>? attractionFeatures = null,
        Dictionary<int, int>? attractionMaxParticipants = null,
        Dictionary<int, List<FeatureAssignment>>? offerFeatures = null,
        Dictionary<int, OfferGroupLimitSnapshot>? offerGroupLimits = null,
        Dictionary<int, List<FeatureAssignment>>? addonFeatureModifiers = null)
    {
        AttractionFeatures = attractionFeatures ?? new Dictionary<int, List<FeatureAssignment>>();
        AttractionMaxParticipants = attractionMaxParticipants ?? new Dictionary<int, int>();
        OfferFeatures = offerFeatures ?? new Dictionary<int, List<FeatureAssignment>>();
        OfferGroupLimits = offerGroupLimits ?? new Dictionary<int, OfferGroupLimitSnapshot>();
        AddonFeatureModifiers = addonFeatureModifiers ?? new Dictionary<int, List<FeatureAssignment>>();
    }
}

public static class JsonFeatureStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static string? _resolvedDataDirectory;

    public static string LastWarningMessage { get; private set; } = string.Empty;

    public static string GetDataDirectory()
    {
        if (!string.IsNullOrEmpty(_resolvedDataDirectory))
        {
            return _resolvedDataDirectory;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (int depth = 0; depth < 5 && current is not null; depth++)
        {
            if (File.Exists(Path.Combine(current.FullName, "Attractions.csproj")))
            {
                return SetAndEnsureDirectory(Path.Combine(current.FullName, "Data"));
            }

            if (File.Exists(Path.Combine(current.FullName, "Attractions", "Attractions.csproj")))
            {
                return SetAndEnsureDirectory(Path.Combine(current.FullName, "Attractions", "Data"));
            }

            current = current.Parent;
        }

        return SetAndEnsureDirectory(Path.Combine(AppContext.BaseDirectory, "Data"));
    }

    public static string GetFeaturesFilePath() => Path.Combine(GetDataDirectory(), "Features.json");

    public static string GetAssignmentsFilePath() => Path.Combine(GetDataDirectory(), "FeatureAssignments.json");

    public static void LoadFeaturesOrInitializeDefaults()
    {
        LastWarningMessage = string.Empty;
        string filePath = GetFeaturesFilePath();

        if (!File.Exists(filePath))
        {
            InMemoryData.Features = CreateDefaultFeatures();
            TrySaveFeatures(out _);
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var document = JsonSerializer.Deserialize<FeaturesFileDocument>(json, SerializerOptions);

            if (document?.Features is null || document.Features.Count == 0)
            {
                InMemoryData.Features = CreateDefaultFeatures();
                TrySaveFeatures(out _);
                return;
            }

            InMemoryData.Features = SanitizeLoadedFeatures(document.Features);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LastWarningMessage =
                $"Wykryto problem z odczytem pliku Features.json ({ex.Message}). Załadowano domyślny słownik cech w pamięci.";
            InMemoryData.Features = CreateDefaultFeatures();
        }
    }

    public static bool TrySaveFeatures(out string errorMessage)
    {
        return TryWriteJsonFile(
            GetFeaturesFilePath(),
            () => new FeaturesFileDocument(InMemoryData.Features.OrderBy(f => f.Id).ToList()),
            "Features.json",
            out errorMessage);
    }

    public static void LoadOrSaveInitialAssignments()
    {
        string filePath = GetAssignmentsFilePath();
        if (!File.Exists(filePath))
        {
            TrySaveAssignments(out _);
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var doc = JsonSerializer.Deserialize<FeatureAssignmentsFileDocument>(json, SerializerOptions);
            if (doc is null)
            {
                TrySaveAssignments(out _);
                return;
            }

            var definitionsById = InMemoryData.Features.ToDictionary(f => f.Id);

            foreach (var attraction in InMemoryData.Attractions)
            {
                if (doc.AttractionFeatures.TryGetValue(attraction.Id, out var savedList))
                {
                    attraction.ReplaceFeatures(SanitizeAssignments(savedList, definitionsById));
                }

                if (doc.AttractionMaxParticipants.TryGetValue(attraction.Id, out int savedMax) && savedMax >= 1)
                {
                    attraction.SetMaxParticipants(savedMax);
                }
            }

            foreach (var offer in InMemoryData.Offers)
            {
                if (doc.OfferFeatures.TryGetValue(offer.Id, out var savedOfferList))
                {
                    offer.ReplaceFeatures(SanitizeAssignments(savedOfferList, definitionsById));
                }

                if (doc.OfferGroupLimits.TryGetValue(offer.Id, out var savedLimits) && savedLimits.MinParticipants >= 1)
                {
                    offer.SetGroupLimits(
                        savedLimits.MinParticipants,
                        savedLimits.MaxParticipants is >= 1 ? savedLimits.MaxParticipants : null);
                }

                foreach (var addon in offer.Addons)
                {
                    if (doc.AddonFeatureModifiers.TryGetValue(addon.Id, out var savedAddonList))
                    {
                        addon.ReplaceFeatureModifiers(SanitizeAssignments(savedAddonList, definitionsById));
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LastWarningMessage =
                $"Wykryto problem z odczytem przypisań cech FeatureAssignments.json ({ex.Message}). Użyto domyślnych danych startowych.";
        }
    }

    public static bool TrySaveAssignments(out string errorMessage)
    {
        return TryWriteJsonFile(
            GetAssignmentsFilePath(),
            BuildAssignmentsDocumentSnapshot,
            "FeatureAssignments.json",
            out errorMessage);
    }

    private static FeatureAssignmentsFileDocument BuildAssignmentsDocumentSnapshot()
    {
        var doc = new FeatureAssignmentsFileDocument();

        foreach (var attraction in InMemoryData.Attractions.OrderBy(a => a.Id))
        {
            doc.AttractionFeatures[attraction.Id] = attraction.Features
                .OrderBy(f => f.FeatureId)
                .Select(f => f.Clone())
                .ToList();
            doc.AttractionMaxParticipants[attraction.Id] = attraction.MaxParticipants;
        }

        foreach (var offer in InMemoryData.Offers.OrderBy(o => o.Id))
        {
            doc.OfferFeatures[offer.Id] = offer.Features
                .OrderBy(f => f.FeatureId)
                .Select(f => f.Clone())
                .ToList();

            doc.OfferGroupLimits[offer.Id] = new OfferGroupLimitSnapshot(offer.MinParticipants, offer.MaxParticipants);

            foreach (var addon in offer.Addons.OrderBy(a => a.Id))
            {
                if (addon.FeatureModifiers.Count > 0)
                {
                    doc.AddonFeatureModifiers[addon.Id] = addon.FeatureModifiers
                        .OrderBy(m => m.FeatureId)
                        .Select(m => m.Clone())
                        .ToList();
                }
            }
        }

        return doc;
    }

    private static bool TryWriteJsonFile<T>(
        string filePath,
        Func<T> documentBuilder,
        string fileLabel,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            lock (InMemoryData.SyncRoot)
            {
                var doc = documentBuilder();
                string json = JsonSerializer.Serialize(doc, SerializerOptions);
                File.WriteAllText(filePath, json);
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errorMessage = $"Nie udało się zapisać pliku {fileLabel}: {ex.Message}";
            return false;
        }
    }

    private static string SetAndEnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        _resolvedDataDirectory = path;
        return path;
    }

    private static List<FeatureDefinition> SanitizeLoadedFeatures(IEnumerable<FeatureDefinition> rawFeatures)
    {
        var result = new List<FeatureDefinition>();
        var usedIds = new HashSet<int>();

        foreach (var f in rawFeatures)
        {
            string cleanName = DomainValueHelper.CleanText(f.Name);
            if (f.Id <= 0 || usedIds.Contains(f.Id) || cleanName.Length == 0)
            {
                continue;
            }

            List<string>? cleanOptions = null;
            if (f.Type == FeatureValueType.OptionList)
            {
                cleanOptions = (f.AllowedValues ?? Enumerable.Empty<string>())
                    .Select(DomainValueHelper.CleanText)
                    .Where(v => v.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            usedIds.Add(f.Id);
            result.Add(new FeatureDefinition(
                f.Id,
                cleanName,
                f.Type,
                DomainValueHelper.CleanText(f.Description),
                f.ComparisonRule,
                DomainValueHelper.CleanNullableText(f.Unit),
                cleanOptions,
                DomainValueHelper.CleanNullableText(f.ProfileBindingKey)));
        }

        return result.OrderBy(f => f.Id).ToList();
    }

    private static List<FeatureAssignment> SanitizeAssignments(
        IEnumerable<FeatureAssignment>? assignments,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById)
    {
        var valid = new List<FeatureAssignment>();
        if (assignments is null)
        {
            return valid;
        }

        var seenIds = new HashSet<int>();
        foreach (var item in assignments)
        {
            if (!definitionsById.TryGetValue(item.FeatureId, out var def) || seenIds.Contains(item.FeatureId))
            {
                continue;
            }

            if (!def.TryNormalizeValue(item.Value, out string normalized, out _))
            {
                continue;
            }

            seenIds.Add(item.FeatureId);
            valid.Add(new FeatureAssignment(item.FeatureId, normalized, item.EffectMode));
        }

        return valid;
    }

    public static List<FeatureDefinition> CreateDefaultFeatures()
    {
        return new List<FeatureDefinition>
        {
            new(1, "Dostępność dla wózków inwalidzkich", FeatureValueType.Boolean,
                "Czy atrakcja lub trasa jest dostępna dla osób poruszających się na wózku inwalidzkim",
                FeatureComparisonRule.FacilityAvailable, profileBindingKey: "wheelchair"),
            new(2, "Minimalny wiek", FeatureValueType.Number,
                "Minimalny wymagany wiek uczestnika w latach",
                FeatureComparisonRule.MinThreshold, unit: "lat", profileBindingKey: "min_age"),
            new(3, "Maksymalny wiek", FeatureValueType.Number,
                "Maksymalny dopuszczalny wiek uczestnika w latach",
                FeatureComparisonRule.MaxThreshold, unit: "lat", profileBindingKey: "max_age"),
            new(4, "Wymagana obecność opiekuna", FeatureValueType.Boolean,
                "Czy osoby niepełnoletnie wymagają obecności pełnoletniego opiekuna",
                FeatureComparisonRule.ConditionRequired, profileBindingKey: "guardian_required"),
            new(5, "Wymagana sprawność fizyczna", FeatureValueType.OptionList,
                "Wymagany poziom kondycji i sprawności fizycznej na trasie",
                FeatureComparisonRule.MaxLevelIndex,
                allowedValues: new List<string> { "Niski", "Średni", "Wysoki" },
                profileBindingKey: "physical_demand"),
            new(6, "Wymagany minimalny wzrost", FeatureValueType.Number,
                "Minimalny wzrost uczestnika w centymetrach",
                FeatureComparisonRule.MinThreshold, unit: "cm", profileBindingKey: "min_height"),
            new(7, "Dostępność dla dzieci", FeatureValueType.Boolean,
                "Czy atrakcja lub oferta jest dostosowana i przyjazna dla dzieci",
                FeatureComparisonRule.FacilityAvailable, profileBindingKey: "child_friendly"),
            new(8, "Możliwość wejścia z wózkiem dziecięcym", FeatureValueType.Boolean,
                "Czy trasę można pokonać z wózkiem dziecięcym bez barier architektonicznych",
                FeatureComparisonRule.FacilityAvailable, profileBindingKey: "stroller_access"),
            new(9, "Dostępność windy", FeatureValueType.Boolean,
                "Czy w obiekcie lub na trasie zwiedzania dostępna jest winda",
                FeatureComparisonRule.FacilityAvailable, profileBindingKey: "elevator"),
            new(10, "Język oprowadzania / materiałów", FeatureValueType.Text,
                "Dostępne języki przewodnika, audioprzewodnika lub materiałów edukacyjnych",
                FeatureComparisonRule.ExactOrContainsMatch)
        };
    }
}
