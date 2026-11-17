namespace Attractions.Models;

public class OrderAddonItem
{
    public int AddonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
