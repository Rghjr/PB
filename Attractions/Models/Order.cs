namespace Attractions.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int TicketsCount { get; set; } = 1;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Blik;
    public List<OrderOfferItem> Items { get; set; } = new();

    public decimal PricePerPerson => Items.Sum(i => i.ItemSubtotal);
    public decimal TotalPrice => PricePerPerson * TicketsCount;
}
