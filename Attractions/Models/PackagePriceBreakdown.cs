namespace Attractions.Models;

public class OrderParticipantGroupItem
{
    public TicketCategory Category { get; }
    public string CategoryName { get; }
    public int Count { get; }
    public decimal DiscountPercent { get; }
    public decimal BaseUnitPrice { get; }
    public decimal FinalUnitPrice { get; }
    public decimal LineSubtotal => FinalUnitPrice * Count;

    public OrderParticipantGroupItem(
        TicketCategory category,
        string categoryName,
        int count,
        decimal discountPercent,
        decimal baseUnitPrice,
        decimal finalUnitPrice)
    {
        Category = category;
        CategoryName = categoryName ?? string.Empty;
        Count = count;
        DiscountPercent = discountPercent;
        BaseUnitPrice = baseUnitPrice;
        FinalUnitPrice = finalUnitPrice;
    }
}

public class PackagePriceBreakdown
{
    public decimal BasePricePerPerson { get; }
    public int TotalParticipants { get; }
    public List<OrderParticipantGroupItem> CategoryLines { get; }
    public decimal ExtraGroupDiscountPercent { get; }

    public PackagePriceBreakdown(
        decimal basePricePerPerson,
        int totalParticipants,
        IEnumerable<OrderParticipantGroupItem> categoryLines,
        decimal extraGroupDiscountPercent)
    {
        BasePricePerPerson = basePricePerPerson;
        TotalParticipants = totalParticipants;
        CategoryLines = categoryLines is not null
            ? new List<OrderParticipantGroupItem>(categoryLines)
            : new List<OrderParticipantGroupItem>();
        ExtraGroupDiscountPercent = extraGroupDiscountPercent;
    }

    public decimal RegularGrossTotal => decimal.Round(BasePricePerPerson * TotalParticipants, 2);
    public decimal SubtotalAfterCategoryDiscounts => decimal.Round(CategoryLines.Sum(l => l.LineSubtotal), 2);
    public decimal CategoryDiscountsSavings => decimal.Round(RegularGrossTotal - SubtotalAfterCategoryDiscounts, 2);

    public decimal ExtraGroupDiscountAmount =>
        decimal.Round(SubtotalAfterCategoryDiscounts * (ExtraGroupDiscountPercent / 100m), 2);

    public decimal FinalTotalPrice =>
        decimal.Round(SubtotalAfterCategoryDiscounts - ExtraGroupDiscountAmount, 2);

    public decimal TotalSavings => decimal.Round(RegularGrossTotal - FinalTotalPrice, 2);

    public static PackagePriceBreakdown Calculate(decimal basePricePerPerson, ParticipantGroupBreakdown breakdown)
    {
        ArgumentNullException.ThrowIfNull(breakdown);

        var lines = new List<OrderParticipantGroupItem>();
        foreach (var (category, count) in breakdown.GetNonZeroCategories())
        {
            decimal discountPct = TicketDiscountPolicy.GetCategoryDiscountPercent(category);
            decimal unitAfterDiscount = decimal.Round(basePricePerPerson * (1m - discountPct / 100m), 2);

            lines.Add(new OrderParticipantGroupItem(
                category,
                TicketDiscountPolicy.GetCategoryDisplayName(category),
                count,
                discountPct,
                basePricePerPerson,
                unitAfterDiscount));
        }

        int totalCount = breakdown.TotalCount;
        decimal groupDiscountPct = TicketDiscountPolicy.GetExtraGroupDiscountPercent(totalCount);

        return new PackagePriceBreakdown(basePricePerPerson, totalCount, lines, groupDiscountPct);
    }
}
