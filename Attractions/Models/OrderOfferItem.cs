namespace Attractions.Models;

public class OrderOfferItem
{
    public int OfferId { get; set; }
    public string OfferName { get; set; } = string.Empty;
    public string AttractionName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsMainOffer { get; set; }
    public List<OrderAddonItem> SelectedAddons { get; set; } = new();

    public decimal AddonsSubtotal => SelectedAddons.Sum(a => a.Price);
    public decimal ItemSubtotal => BasePrice + AddonsSubtotal;
}
