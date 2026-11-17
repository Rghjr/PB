using Attractions.Models;

namespace Attractions.DTOs;

public class SelectedRelatedOffer
{
    public Offer Offer { get; }
    public List<Addon> SelectedAddons { get; } = new();

    public SelectedRelatedOffer(Offer offer)
    {
        Offer = offer ?? throw new ArgumentNullException(nameof(offer));
    }

    public decimal BasePrice => Offer.Price;
    public decimal AddonsPrice => SelectedAddons.Sum(a => a.Price);
    public decimal Subtotal => BasePrice + AddonsPrice;
}
