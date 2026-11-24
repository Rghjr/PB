namespace Attractions.Models;

public class OrderAddonItem
{
    public int AddonId { get; }
    public string Name { get; }
    public decimal Price { get; }

    public OrderAddonItem(int addonId, string name, decimal price)
    {
        AddonId = addonId;
        Name = name ?? string.Empty;
        Price = price;
    }
}
