using Attractions.Models;

namespace Attractions.Data;

public static class InMemoryData
{
    private static int _lastAttractionId;
    private static int _lastOfferId;
    private static int _lastAddonId;
    private static int _lastOrderId;

    public static List<Attraction> Attractions { get; set; } = new();
    public static List<Offer> Offers { get; set; } = new();
    public static List<Order> Orders { get; set; } = new();

    public static int GetNextAttractionId()
    {
        int currentMax = Attractions.Count > 0 ? Attractions.Max(a => a.Id) : 0;
        _lastAttractionId = Math.Max(_lastAttractionId, currentMax) + 1;
        return _lastAttractionId;
    }

    public static int GetNextOfferId()
    {
        int currentMax = Offers.Count > 0 ? Offers.Max(o => o.Id) : 0;
        _lastOfferId = Math.Max(_lastOfferId, currentMax) + 1;
        return _lastOfferId;
    }

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

    public static int GetNextOrderId()
    {
        int currentMax = Orders.Count > 0 ? Orders.Max(o => o.Id) : 0;
        _lastOrderId = Math.Max(_lastOrderId, currentMax) + 1;
        return _lastOrderId;
    }
}
