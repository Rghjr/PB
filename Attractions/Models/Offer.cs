namespace Attractions.Models;

public class Offer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Planned;
    public int AttractionId { get; set; }
    public List<Addon> Addons { get; set; } = new();
    public List<int> RelatedOfferIds { get; set; } = new();

    public bool IsToday => Status == OfferStatus.Planned && Date.Date == DateTime.Today;
    public bool IsFuturePlanned => Status == OfferStatus.Planned && Date.Date > DateTime.Today;
    public bool IsPast => Date.Date < DateTime.Today;
    public bool IsAvailableForUser => Status == OfferStatus.Planned && Date.Date >= DateTime.Today;
}
