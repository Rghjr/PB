using Attractions.Models;

namespace Attractions.DTOs;

public class SelectedRelatedOffer
{
    public Offer Offer { get; }
    public List<Addon> SelectedAddons { get; }

    public SelectedRelatedOffer(Offer offer, IEnumerable<Addon>? selectedAddons = null)
    {
        Offer = offer ?? throw new ArgumentNullException(nameof(offer));
        SelectedAddons = selectedAddons is not null ? new List<Addon>(selectedAddons) : new List<Addon>();
    }

    public decimal BasePrice => Offer.Price;
    public decimal AddonsPrice => SelectedAddons.Sum(a => a.Price);
    public decimal Subtotal => BasePrice + AddonsPrice;
}
