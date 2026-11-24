namespace Attractions.Models;

public class OrderOfferItem
{
    public int OfferId { get; }
    public string OfferName { get; }
    public string AttractionName { get; }
    public DateTime Date { get; }
    public decimal BasePrice { get; }
    public bool IsMainOffer { get; }
    public List<OrderAddonItem> SelectedAddons { get; }
    public List<string> EffectiveFeaturesSnapshot { get; }

    public OrderOfferItem(
        int offerId,
        string offerName,
        string attractionName,
        DateTime date,
        decimal basePrice,
        bool isMainOffer,
        IEnumerable<OrderAddonItem>? selectedAddons = null,
        IEnumerable<string>? effectiveFeaturesSnapshot = null)
    {
        OfferId = offerId;
        OfferName = offerName ?? string.Empty;
        AttractionName = attractionName ?? string.Empty;
        Date = date.Date;
        BasePrice = basePrice;
        IsMainOffer = isMainOffer;
        SelectedAddons = selectedAddons is not null
            ? new List<OrderAddonItem>(selectedAddons)
            : new List<OrderAddonItem>();
        EffectiveFeaturesSnapshot = effectiveFeaturesSnapshot is not null
            ? new List<string>(effectiveFeaturesSnapshot)
            : new List<string>();
    }

    public decimal AddonsSubtotal => SelectedAddons.Sum(a => a.Price);
    public decimal ItemSubtotal => BasePrice + AddonsSubtotal;
}
