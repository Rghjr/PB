using Attractions.Data;
using Attractions.DTOs;
using Attractions.Models;

namespace Attractions.Services;

public class OrderService
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;
    private readonly OfferExtensionService _offerExtensionService;

    public OrderService(
        OfferService offerService,
        AttractionService attractionService,
        OfferExtensionService offerExtensionService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
        _offerExtensionService = offerExtensionService ?? throw new ArgumentNullException(nameof(offerExtensionService));
    }

    public List<Attraction> GetAttractionsWithAvailableOffers()
    {
        var availableAttractionIds = _offerService
            .GetAvailableOffersForUser()
            .Select(o => o.AttractionId)
            .ToHashSet();

        return _attractionService
            .GetAllAttractions()
            .Where(a => availableAttractionIds.Contains(a.Id))
            .OrderBy(a => a.Id)
            .ToList();
    }

    public List<Offer> GetAvailableOffersForAttraction(int attractionId)
    {
        return _offerService
            .GetAvailableOffersForUser()
            .Where(o => o.AttractionId == attractionId)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public List<Order> GetAllOrders()
    {
        return InMemoryData.Orders
            .OrderByDescending(o => o.Id)
            .ToList();
    }

    public Order? GetOrderById(int orderId)
    {
        return InMemoryData.Orders.FirstOrDefault(o => o.Id == orderId);
    }

    public ServiceResult<Order> PlaceOrder(
        OfferConfiguration configuration,
        string customerName,
        string customerEmail,
        int ticketsCount,
        PaymentMethod paymentMethod = PaymentMethod.Blik)
    {
        if (configuration is null)
        {
            return ServiceResult<Order>.Fail("Brak skonfigurowanej oferty do złożenia zamówienia.");
        }

        var syncResult = _offerExtensionService.SynchronizeConfiguration(configuration);
        if (!syncResult.IsSuccess)
        {
            return ServiceResult<Order>.Fail(syncResult.Message);
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return ServiceResult<Order>.Fail("Imię i nazwisko zamawiającego nie może być puste.");
        }

        if (ticketsCount < 1)
        {
            return ServiceResult<Order>.Fail("Liczba biletów / uczestników musi wynosić co najmniej 1.");
        }

        if (ticketsCount > 100)
        {
            return ServiceResult<Order>.Fail("Maksymalna liczba biletów w pojedynczym zamówieniu wynosi 100.");
        }

        string normalizedEmail = customerEmail?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(normalizedEmail) && !IsValidSimpleEmail(normalizedEmail))
        {
            return ServiceResult<Order>.Fail("Podano nieprawidłowy format adresu e-mail.");
        }

        var items = new List<OrderOfferItem>
        {
            CreateOfferItemSnapshot(
                configuration.BaseOffer,
                configuration.SelectedAddons,
                isMainOffer: true)
        };

        foreach (var selectedRelated in configuration.SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
        {
            items.Add(CreateOfferItemSnapshot(
                selectedRelated.Offer,
                selectedRelated.SelectedAddons,
                isMainOffer: false));
        }

        int nextId = InMemoryData.GetNextOrderId();
        var order = new Order
        {
            Id = nextId,
            OrderNumber = $"WAW-{DateTime.Today:yyyy}-{nextId:D4}",
            CreatedAt = DateTime.Now,
            CustomerName = customerName.Trim(),
            CustomerEmail = normalizedEmail,
            TicketsCount = ticketsCount,
            PaymentMethod = paymentMethod,
            Items = items
        };

        InMemoryData.Orders.Add(order);

        return ServiceResult<Order>.Ok(
            order,
            $"Zamówienie {order.OrderNumber} zostało pomyślnie złożone! Łączna kwota: {order.TotalPrice:F2} zł.");
    }

    private OrderOfferItem CreateOfferItemSnapshot(Offer offer, IEnumerable<Addon> selectedAddons, bool isMainOffer)
    {
        string attractionName = _attractionService.GetAttractionById(offer.AttractionId)?.Name ?? "(nieznana atrakcja)";

        return new OrderOfferItem
        {
            OfferId = offer.Id,
            OfferName = offer.Name,
            AttractionName = attractionName,
            Date = offer.Date.Date,
            BasePrice = offer.Price,
            IsMainOffer = isMainOffer,
            SelectedAddons = selectedAddons
                .OrderBy(a => a.Id)
                .Select(a => new OrderAddonItem
                {
                    AddonId = a.Id,
                    Name = a.Name,
                    Price = a.Price
                })
                .ToList()
        };
    }

    private static bool IsValidSimpleEmail(string email)
    {
        if (email.Contains(' '))
        {
            return false;
        }

        int atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@'))
        {
            return false;
        }

        int dotIndex = email.IndexOf('.', atIndex + 2);
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }
}
