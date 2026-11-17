using Attractions.Models;

namespace Attractions.DTOs;

public class OfferConfiguration
{
    public Offer BaseOffer { get; }
    public List<Addon> SelectedAddons { get; } = new();
    public List<SelectedRelatedOffer> SelectedRelatedOffers { get; } = new();

    public OfferConfiguration(Offer baseOffer)
    {
        BaseOffer = baseOffer ?? throw new ArgumentNullException(nameof(baseOffer));
    }

    public decimal BasePrice => BaseOffer.Price;
    public decimal BaseAddonsPrice => SelectedAddons.Sum(a => a.Price);
    public decimal RelatedOffersBasePrice => SelectedRelatedOffers.Sum(r => r.BasePrice);
    public decimal RelatedOffersAddonsPrice => SelectedRelatedOffers.Sum(r => r.AddonsPrice);
    public decimal TotalPrice => BasePrice + BaseAddonsPrice + SelectedRelatedOffers.Sum(r => r.Subtotal);
}
