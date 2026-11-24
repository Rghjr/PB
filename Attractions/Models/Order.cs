namespace Attractions.Models;

public class Order
{
    public int Id { get; }
    public string OrderNumber { get; }
    public DateTime CreatedAt { get; }
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public int TicketsCount { get; }
    public PaymentMethod PaymentMethod { get; }
    public List<OrderOfferItem> Items { get; }
    public List<OrderParticipantGroupItem> ParticipantGroups { get; }
    public decimal ExtraGroupDiscountPercent { get; }
    public decimal ExtraGroupDiscountAmount { get; }

    public Order(
        int id,
        string orderNumber,
        DateTime createdAt,
        string customerName,
        string customerEmail,
        int ticketsCount,
        PaymentMethod paymentMethod,
        IEnumerable<OrderOfferItem> items,
        IEnumerable<OrderParticipantGroupItem>? participantGroups = null,
        decimal extraGroupDiscountPercent = 0m,
        decimal extraGroupDiscountAmount = 0m)
    {
        ArgumentNullException.ThrowIfNull(items);

        Id = id;
        OrderNumber = orderNumber ?? string.Empty;
        CreatedAt = createdAt;
        CustomerName = customerName ?? string.Empty;
        CustomerEmail = customerEmail ?? string.Empty;
        TicketsCount = Math.Max(1, ticketsCount);
        PaymentMethod = paymentMethod;
        Items = new List<OrderOfferItem>(items);
        ParticipantGroups = participantGroups is not null
            ? new List<OrderParticipantGroupItem>(participantGroups)
            : new List<OrderParticipantGroupItem>();
        ExtraGroupDiscountPercent = extraGroupDiscountPercent;
        ExtraGroupDiscountAmount = extraGroupDiscountAmount;
    }

    public decimal PricePerPerson => Items.Sum(i => i.ItemSubtotal);
    public decimal RegularGrossPrice => decimal.Round(PricePerPerson * TicketsCount, 2);

    public decimal SubtotalAfterCategoryDiscounts =>
        ParticipantGroups.Count > 0
            ? decimal.Round(ParticipantGroups.Sum(g => g.LineSubtotal), 2)
            : RegularGrossPrice;

    public decimal CategoryDiscountsSavings =>
        decimal.Round(RegularGrossPrice - SubtotalAfterCategoryDiscounts, 2);

    public decimal TotalPrice =>
        ParticipantGroups.Count > 0
            ? decimal.Round(SubtotalAfterCategoryDiscounts - ExtraGroupDiscountAmount, 2)
            : RegularGrossPrice;

    public decimal TotalSavings => decimal.Round(RegularGrossPrice - TotalPrice, 2);
}
