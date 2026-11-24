using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
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

    public int GetBookedParticipantsForOffer(int offerId)
    {
        return InMemoryData.Orders
            .Where(order => order.Items.Any(item => item.OfferId == offerId))
            .Sum(order => order.TicketsCount);
    }

    public int GetRemainingSpotsForOffer(Offer offer)
    {
        var attraction = _attractionService.GetAttractionById(offer.AttractionId);
        int effectiveMax = offer.GetEffectiveMaxParticipants(attraction);
        int booked = GetBookedParticipantsForOffer(offer.Id);
        return Math.Max(0, effectiveMax - booked);
    }

    public int GetBookedParticipantsForAttractionOnDate(int attractionId, DateTime date)
    {
        var offerIdsForAttraction = InMemoryData.Offers
            .Where(o => o.AttractionId == attractionId)
            .Select(o => o.Id)
            .ToHashSet();

        return InMemoryData.Orders
            .Where(order => order.Items.Any(i => offerIdsForAttraction.Contains(i.OfferId) && i.Date.Date == date.Date))
            .Sum(order => order.TicketsCount);
    }

    public int GetRemainingAttractionSpotsOnDate(Attraction attraction, DateTime date)
    {
        int booked = GetBookedParticipantsForAttractionOnDate(attraction.Id, date);
        return Math.Max(0, attraction.MaxParticipants - booked);
    }

    public (int MinRemainingSpots, string LimitingSource) GetPackageRemainingSpots(OfferConfiguration config)
    {
        var (pkgMin, pkgMax, _, _) = config.GetPackageGroupSizeBounds(_attractionService.GetAttractionById);
        if (pkgMin > pkgMax)
        {
            return (0, config.BaseOffer.Name);
        }

        int minRemaining = pkgMax;
        string limitingSource = config.BaseOffer.Name;

        var allPackageOffers = new List<Offer> { config.BaseOffer };
        allPackageOffers.AddRange(config.SelectedRelatedOffers.Select(r => r.Offer));

        foreach (var packageOffer in allPackageOffers)
        {
            int remOffer = GetRemainingSpotsForOffer(packageOffer);
            if (remOffer < minRemaining)
            {
                minRemaining = remOffer;
                limitingSource = packageOffer.Name;
            }

            var attr = _attractionService.GetAttractionById(packageOffer.AttractionId);
            if (attr is not null)
            {
                int remAttr = GetRemainingAttractionSpotsOnDate(attr, packageOffer.Date);
                if (remAttr < minRemaining)
                {
                    minRemaining = remAttr;
                    limitingSource = $"{attr.Name} ({packageOffer.Date:yyyy-MM-dd})";
                }
            }
        }

        return (minRemaining, limitingSource);
    }

    public ServiceResult<Order> PlaceOrder(
        OfferConfiguration configuration,
        string customerName,
        string customerEmail,
        int ticketsCount,
        PaymentMethod paymentMethod = PaymentMethod.Blik,
        ParticipantProfile? participantProfile = null)
    {
        return PlaceOrder(
            configuration,
            customerName,
            customerEmail,
            ParticipantGroupBreakdown.FromAdultsCount(ticketsCount),
            paymentMethod,
            participantProfile);
    }

    public ServiceResult<Order> PlaceOrder(
        OfferConfiguration configuration,
        string customerName,
        string customerEmail,
        ParticipantGroupBreakdown groupBreakdown,
        PaymentMethod paymentMethod = PaymentMethod.Blik,
        ParticipantProfile? participantProfile = null)
    {
        if (configuration is null)
        {
            return ServiceResult<Order>.Fail("Brak skonfigurowanej oferty do złożenia zamówienia.");
        }

        if (groupBreakdown is null || groupBreakdown.TotalCount < 1)
        {
            return ServiceResult<Order>.Fail("Liczba uczestników / biletów w grupie musi wynosić co najmniej 1.");
        }

        int ticketsCount = groupBreakdown.TotalCount;

        var syncResult = _offerExtensionService.SynchronizeConfiguration(configuration);
        if (!syncResult.IsSuccess)
        {
            return ServiceResult<Order>.Fail(syncResult.Message);
        }

        var capacityCheck = ValidatePackageCapacityAndBounds(configuration, ticketsCount);
        if (!capacityCheck.IsSuccess)
        {
            return ServiceResult<Order>.Fail(capacityCheck.Message);
        }

        if (participantProfile is not null)
        {
            var profileForOrder = CloneProfileWithAdjustedCountIfNeeded(participantProfile, groupBreakdown);
            var compatibilityReport = configuration.ValidateAgainst(
                profileForOrder,
                InMemoryData.Features,
                _attractionService.GetAttractionById);

            if (!compatibilityReport.IsCompatible)
            {
                string reasons = string.Join(
                    Environment.NewLine + "  - ",
                    compatibilityReport.IncompatibilityReasons);

                return ServiceResult<Order>.Fail(
                    $"Finalizacja zamówienia została zablokowana – skonfigurowany pakiet nie spełnia wymagań profilu/grupy:{Environment.NewLine}  - {reasons}");
            }
        }
        else
        {
            var inferredCheck = ValidateCategoryBreakdownAgainstPackage(configuration, groupBreakdown);
            if (!inferredCheck.IsSuccess)
            {
                return ServiceResult<Order>.Fail(inferredCheck.Message);
            }
        }

        string cleanCustomerName = DomainValueHelper.CleanText(customerName);
        if (cleanCustomerName.Length == 0)
        {
            return ServiceResult<Order>.Fail("Imię i nazwisko zamawiającego nie może być puste.");
        }

        if (ticketsCount > 100)
        {
            return ServiceResult<Order>.Fail("Maksymalna liczba biletów w pojedynczym zamówieniu wynosi 100.");
        }

        string normalizedEmail = DomainValueHelper.CleanText(customerEmail);
        if (normalizedEmail.Length > 0 && !DomainValueHelper.IsValidEmail(normalizedEmail))
        {
            return ServiceResult<Order>.Fail("Podano nieprawidłowy format adresu e-mail.");
        }

        var items = new List<OrderOfferItem>
        {
            CreateOfferItemSnapshot(configuration.BaseOffer, configuration.SelectedAddons, isMainOffer: true)
        };

        foreach (var selectedRelated in configuration.SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
        {
            items.Add(CreateOfferItemSnapshot(
                selectedRelated.Offer,
                selectedRelated.SelectedAddons,
                isMainOffer: false));
        }

        var priceCalc = configuration.CalculatePriceBreakdown(groupBreakdown);
        int nextId = InMemoryData.GetNextOrderId();

        var order = new Order(
            id: nextId,
            orderNumber: $"WAW-{DateTime.Today:yyyy}-{nextId:D4}",
            createdAt: DateTime.Now,
            customerName: cleanCustomerName,
            customerEmail: normalizedEmail,
            ticketsCount: ticketsCount,
            paymentMethod: paymentMethod,
            items: items,
            participantGroups: priceCalc.CategoryLines,
            extraGroupDiscountPercent: priceCalc.ExtraGroupDiscountPercent,
            extraGroupDiscountAmount: priceCalc.ExtraGroupDiscountAmount);

        InMemoryData.Orders.Add(order);

        string savingsNote = order.TotalSavings > 0
            ? $" (naliczone zniżki łącznie: -{order.TotalSavings:F2} zł)"
            : string.Empty;

        return ServiceResult<Order>.Ok(
            order,
            $"Zamówienie {order.OrderNumber} zostało pomyślnie złożone! Łączna kwota: {order.TotalPrice:F2} zł{savingsNote}.");
    }

    private ServiceResult ValidatePackageCapacityAndBounds(OfferConfiguration configuration, int ticketsCount)
    {
        var allOffersInPackage = new List<Offer> { configuration.BaseOffer };
        allOffersInPackage.AddRange(configuration.SelectedRelatedOffers.Select(r => r.Offer));

        foreach (var offer in allOffersInPackage)
        {
            var attraction = _attractionService.GetAttractionById(offer.AttractionId);
            int effectiveMax = offer.GetEffectiveMaxParticipants(attraction);

            if (ticketsCount < offer.MinParticipants)
            {
                string exactMsg = offer.MinParticipants == effectiveMax
                    ? $"Oferta \"{offer.Name}\" jest dostępna wyłącznie dla grup dokładnie {offer.MinParticipants}-osobowych (wybrano: {ticketsCount} os.)."
                    : $"Oferta grupowa \"{offer.Name}\" wymaga co najmniej {offer.MinParticipants} osób (wybrano: {ticketsCount} os.).";
                return ServiceResult.Fail(exactMsg);
            }

            if (ticketsCount > effectiveMax)
            {
                return ServiceResult.Fail(
                    $"Liczba osób ({ticketsCount}) przekracza maksymalny limit miejsc ({effectiveMax} os.) dla oferty \"{offer.Name}\".");
            }

            int remainingOfferSpots = GetRemainingSpotsForOffer(offer);
            if (ticketsCount > remainingOfferSpots)
            {
                return ServiceResult.Fail(
                    $"Brak wystarczającej liczby wolnych miejsc dla oferty \"{offer.Name}\" (pozostało wolnych: {remainingOfferSpots} z {effectiveMax} os.).");
            }

            if (attraction is not null)
            {
                int remainingAttractionSpots = GetRemainingAttractionSpotsOnDate(attraction, offer.Date);
                if (ticketsCount > remainingAttractionSpots)
                {
                    return ServiceResult.Fail(
                        $"Przekroczono dzienną maksymalną pojemność atrakcji \"{attraction.Name}\" w dniu {offer.Date:yyyy-MM-dd} (pozostało miejsc: {remainingAttractionSpots} z {attraction.MaxParticipants} os.).");
                }
            }
        }

        return ServiceResult.Ok("Pojemność pakietu zweryfikowana.");
    }

    private static ParticipantProfile CloneProfileWithAdjustedCountIfNeeded(
        ParticipantProfile source,
        ParticipantGroupBreakdown breakdown)
    {
        if (source.ParticipantsCount == breakdown.TotalCount)
        {
            return source;
        }

        var adjustedAges = new List<int>(source.ParticipantAges);
        while (adjustedAges.Count < breakdown.TotalCount)
        {
            adjustedAges.Add(source.OldestParticipantAge ?? 30);
        }

        return new ParticipantProfile(
            participantAges: adjustedAges,
            minParticipantHeightCm: source.MinParticipantHeightCm,
            requiresWheelchairAccess: source.RequiresWheelchairAccess || breakdown.DisabledCount > 0,
            disabledParticipantsCount: breakdown.DisabledCount,
            maxPhysicalDemand: source.MaxPhysicalDemand,
            explicitAdultGuardianPresent: source.ExplicitAdultGuardianPresent,
            requiresChildFriendly: source.RequiresChildFriendly || breakdown.ChildrenCount > 0,
            requiresStrollerAccess: source.RequiresStrollerAccess,
            requiresElevator: source.RequiresElevator,
            requiredFacilityFeatureIds: source.RequiredFacilityFeatureIds,
            confirmedConditionFeatureIds: source.ConfirmedConditionFeatureIds,
            customNumericValues: source.CustomNumericValues,
            customOptionOrTextValues: source.CustomOptionOrTextValues);
    }

    private ServiceResult ValidateCategoryBreakdownAgainstPackage(
        OfferConfiguration configuration,
        ParticipantGroupBreakdown breakdown)
    {
        var packageFeatures = configuration.GetPackageEffectiveFeatures(
            InMemoryData.Features,
            _attractionService.GetAttractionById);

        var minAgeDef = InMemoryData.Features.FirstOrDefault(f => f.ProfileBindingKey == "min_age");
        if (minAgeDef is not null &&
            packageFeatures.TryGetValue(minAgeDef.Id, out var minAgeEntry) &&
            DomainValueHelper.TryParseDecimalInvariant(minAgeEntry.Value, out decimal minRequiredAge) &&
            breakdown.ChildrenCount > 0 &&
            minRequiredAge > 12)
        {
            return ServiceResult.Fail(
                $"Nie można przydzielić biletów kategorii \"Dziecko (do 12 lat)\" – pakiet wymaga ukończenia co najmniej {minRequiredAge:0} lat ({minAgeEntry.SourceElement}).");
        }

        var guardianDef = InMemoryData.Features.FirstOrDefault(f => f.ProfileBindingKey == "guardian_required");
        if (guardianDef is not null &&
            packageFeatures.TryGetValue(guardianDef.Id, out var guardianEntry) &&
            DomainValueHelper.TryParseBooleanFlexible(guardianEntry.Value, out bool needsGuardian) &&
            needsGuardian &&
            !breakdown.HasAdultsOrSeniors &&
            breakdown.ChildrenCount > 0)
        {
            return ServiceResult.Fail(
                $"Pakiet wymaga obecności pełnoletniego opiekuna ({guardianEntry.SourceElement}) – nie można zamówić samych biletów dziecięcych bez osoby dorosłej.");
        }

        return ServiceResult.Ok("Podział kategorii biletowych jest prawidłowy.");
    }

    private OrderOfferItem CreateOfferItemSnapshot(Offer offer, IEnumerable<Addon> selectedAddons, bool isMainOffer)
    {
        var attraction = _attractionService.GetAttractionById(offer.AttractionId);
        string attractionName = attraction?.Name ?? "(nieznana atrakcja)";
        var addonList = selectedAddons.OrderBy(a => a.Id).ToList();

        var effectiveFeatures = offer.GetEffectiveFeatures(attraction, addonList, InMemoryData.Features);
        var definitionsById = InMemoryData.Features.ToDictionary(f => f.Id);

        var featuresSnapshot = effectiveFeatures.Values
            .OrderBy(e => e.FeatureId)
            .Where(e => definitionsById.ContainsKey(e.FeatureId))
            .Select(e =>
            {
                var def = definitionsById[e.FeatureId];
                return $"{def.Name}: {def.FormatValue(e.Value)}";
            })
            .ToList();

        var addonSnapshots = addonList
            .Select(a => new OrderAddonItem(a.Id, a.Name, a.Price))
            .ToList();

        return new OrderOfferItem(
            offerId: offer.Id,
            offerName: offer.Name,
            attractionName: attractionName,
            date: offer.Date,
            basePrice: offer.Price,
            isMainOffer: isMainOffer,
            selectedAddons: addonSnapshots,
            effectiveFeaturesSnapshot: featuresSnapshot);
    }
}
