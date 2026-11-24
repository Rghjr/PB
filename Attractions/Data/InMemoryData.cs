using Attractions.Models;

namespace Attractions.Data;

public static class InMemoryData
{
    public static readonly object SyncRoot = new();

    private static int _lastAttractionId;
    private static int _lastOfferId;
    private static int _lastAddonId;
    private static int _lastOrderId;
    private static int _lastFeatureId;

    public static List<FeatureDefinition> Features { get; set; } = new();
    public static List<Attraction> Attractions { get; set; } = new();
    public static List<Offer> Offers { get; set; } = new();
    public static List<Order> Orders { get; set; } = new();

    public static int GetNextFeatureId() =>
        ComputeNextId(Features, f => f.Id, ref _lastFeatureId);

    public static int GetNextAttractionId() =>
        ComputeNextId(Attractions, a => a.Id, ref _lastAttractionId);

    public static int GetNextOfferId() =>
        ComputeNextId(Offers, o => o.Id, ref _lastOfferId);

    public static int GetNextOrderId() =>
        ComputeNextId(Orders, o => o.Id, ref _lastOrderId);

    public static int GetNextAddonId()
    {
        int currentMax = Offers
            .SelectMany(o => o.Addons)
            .Select(a => a.Id)
            .DefaultIfEmpty(0)
            .Max();

        _lastAddonId = Math.Max(_lastAddonId, currentMax) + 1;
        return _lastAddonId;
    }

    private static int ComputeNextId<T>(
        IReadOnlyCollection<T> items,
        Func<T, int> idSelector,
        ref int lastTracker)
    {
        int currentMax = items.Count > 0 ? items.Max(idSelector) : 0;
        lastTracker = Math.Max(lastTracker, currentMax) + 1;
        return lastTracker;
    }
}
