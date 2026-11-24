using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.Services;

public class OfferService
{
    private readonly AttractionService _attractionService;

    public OfferService(AttractionService attractionService)
    {
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
    }

    public List<Offer> GetAllOffers()
    {
        return InMemoryData.Offers
            .OrderBy(o => o.Id)
            .ToList();
    }

    public Offer? GetOfferById(int id)
    {
        return InMemoryData.Offers.FirstOrDefault(o => o.Id == id);
    }

    public List<Offer> GetPlannedOffers()
    {
        DateTime today = DateTime.Today;
        return InMemoryData.Offers
            .Where(o => o.Status == OfferStatus.Planned && o.Date.Date > today)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public List<Offer> GetTodayOffers()
    {
        DateTime today = DateTime.Today;
        return InMemoryData.Offers
            .Where(o => o.Status == OfferStatus.Planned && o.Date.Date == today)
            .OrderBy(o => o.Id)
            .ToList();
    }

    public List<Offer> GetWithdrawnOffers()
    {
        return InMemoryData.Offers
            .Where(o => o.Status == OfferStatus.Withdrawn)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public List<Offer> GetAvailableOffersForUser()
    {
        return InMemoryData.Offers
            .Where(o => o.IsAvailableForUser)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public List<Offer> GetGroupOnlyOffers(bool onlyAvailableForUser = true, int? groupSizeFilter = null)
    {
        var query = InMemoryData.Offers.Where(o => o.IsGroupOffer);
        if (onlyAvailableForUser)
        {
            query = query.Where(o => o.IsAvailableForUser);
        }

        if (groupSizeFilter.HasValue)
        {
            int size = groupSizeFilter.Value;
            query = query.Where(o =>
            {
                var attr = _attractionService.GetAttractionById(o.AttractionId);
                return size >= o.MinParticipants && size <= o.GetEffectiveMaxParticipants(attr);
            });
        }

        return query
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public List<Offer> GetCompatibleOffersForUser(ParticipantProfile profile)
    {
        return EvaluateAvailableOffersForProfile(profile)
            .Where(x => x.Report.IsCompatible)
            .Select(x => x.Offer)
            .ToList();
    }

    public List<(Offer Offer, CompatibilityReport Report)> EvaluateAvailableOffersForProfile(ParticipantProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return GetAvailableOffersForUser()
            .Select(offer =>
            {
                var attraction = _attractionService.GetAttractionById(offer.AttractionId);
                var report = offer.ValidateAgainst(profile, attraction, selectedAddons: null, InMemoryData.Features);
                return (offer, report);
            })
            .ToList();
    }

    public ServiceResult<Offer> GetAvailableOfferForUser(int offerId)
    {
        var offer = GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult<Offer>.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult<Offer>.Fail($"Oferta \"{offer.Name}\" (ID: {offer.Id}) została wycofana i nie jest dostępna.");
        }

        if (offer.Date.Date < DateTime.Today)
        {
            return ServiceResult<Offer>.Fail($"Termin oferty \"{offer.Name}\" ({offer.Date:yyyy-MM-dd}) już minął i nie jest ona dostępna.");
        }

        return ServiceResult<Offer>.Ok(offer, "Oferta jest dostępna.");
    }

    public ServiceResult<Offer> AddOffer(
        int attractionId,
        string name,
        string description,
        decimal price,
        DateTime date,
        IEnumerable<FeatureAssignment>? features = null,
        int minParticipants = 1,
        int? maxParticipants = null)
    {
        var validation = ValidateOfferInput(
            attractionId,
            name,
            price,
            minParticipants,
            maxParticipants,
            out var attraction,
            out string cleanName);

        if (!validation.IsSuccess || attraction is null)
        {
            return ServiceResult<Offer>.Fail(validation.Message);
        }

        var offer = new Offer(
            id: InMemoryData.GetNextOfferId(),
            attractionId: attractionId,
            name: cleanName,
            description: DomainValueHelper.CleanText(description),
            price: price,
            date: date,
            status: OfferStatus.Planned,
            minParticipants: minParticipants,
            maxParticipants: maxParticipants,
            features: features);

        InMemoryData.Offers.Add(offer);
        JsonFeatureStore.TrySaveAssignments(out _);

        return ServiceResult<Offer>.Ok(
            offer,
            $"Dodano ofertę \"{offer.Name}\" (ID: {offer.Id}, limit osób: {offer.FormatGroupSizeLimit(attraction)}) dla atrakcji \"{attraction.Name}\".");
    }

    public ServiceResult UpdateOffer(
        int id,
        int attractionId,
        string name,
        string description,
        decimal price,
        DateTime date,
        OfferStatus status,
        IEnumerable<FeatureAssignment>? features = null,
        int? minParticipants = null,
        int? maxParticipants = null,
        bool updateMaxParticipants = false)
    {
        var offer = GetOfferById(id);
        if (offer is null)
        {
            return ServiceResult.Fail($"Nie można edytować oferty – oferta o ID {id} nie istnieje.");
        }

        int resolvedMin = minParticipants ?? offer.MinParticipants;
        int? resolvedMax = updateMaxParticipants ? maxParticipants : offer.MaxParticipants;

        var validation = ValidateOfferInput(
            attractionId,
            name,
            price,
            resolvedMin,
            resolvedMax,
            out var attraction,
            out string cleanName);

        if (!validation.IsSuccess || attraction is null)
        {
            return validation;
        }

        int removedLinksCount = status == OfferStatus.Withdrawn && offer.RelatedOfferIds.Count > 0
            ? RemoveAllBidirectionalLinks(offer)
            : 0;

        offer.UpdateDetails(
            attractionId,
            cleanName,
            DomainValueHelper.CleanText(description),
            price,
            date,
            status,
            resolvedMin,
            resolvedMax,
            features);

        JsonFeatureStore.TrySaveAssignments(out _);

        string linksNote = removedLinksCount > 0
            ? $" Automatycznie usunięto powiązania z innymi ofertami ({removedLinksCount})."
            : string.Empty;

        return ServiceResult.Ok(
            $"Zaktualizowano ofertę \"{offer.Name}\" (ID: {offer.Id}, limit osób: {offer.FormatGroupSizeLimit(attraction)}).{linksNote}");
    }

    public ServiceResult WithdrawOffer(int id)
    {
        var offer = GetOfferById(id);
        if (offer is null)
        {
            return ServiceResult.Fail($"Oferta o ID {id} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult.Fail($"Oferta \"{offer.Name}\" (ID: {offer.Id}) jest już wycofana.");
        }

        int removedLinksCount = RemoveAllBidirectionalLinks(offer);
        offer.SetStatus(OfferStatus.Withdrawn);

        string linksNote = removedLinksCount > 0
            ? $" Usunięto również jej powiązania z innymi ofertami ({removedLinksCount})."
            : string.Empty;

        return ServiceResult.Ok($"Wycofano ofertę \"{offer.Name}\" (ID: {offer.Id}).{linksNote}");
    }

    public ServiceResult DeleteOffer(int id)
    {
        var offer = GetOfferById(id);
        if (offer is null)
        {
            return ServiceResult.Fail($"Nie można usunąć oferty – oferta o ID {id} nie istnieje.");
        }

        int removedAddonsCount = offer.Addons.Count;
        int removedLinksCount = RemoveAllBidirectionalLinks(offer);

        offer.Addons.Clear();
        InMemoryData.Offers.Remove(offer);
        JsonFeatureStore.TrySaveAssignments(out _);

        return ServiceResult.Ok(
            $"Usunięto ofertę \"{offer.Name}\" (ID: {id}), jej dodatki ({removedAddonsCount}) oraz powiązania ({removedLinksCount}).");
    }

    public List<Offer> GetRelatedOffers(int offerId, bool onlyAvailableForUser = false)
    {
        var offer = GetOfferById(offerId);
        if (offer is null || offer.RelatedOfferIds.Count == 0)
        {
            return new List<Offer>();
        }

        var query = InMemoryData.Offers
            .Where(o => offer.RelatedOfferIds.Contains(o.Id));

        if (onlyAvailableForUser)
        {
            query = query.Where(o => o.IsAvailableForUser);
        }

        return query
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public ServiceResult LinkOffers(int firstOfferId, int secondOfferId)
    {
        var firstOffer = GetOfferById(firstOfferId);
        if (firstOffer is null)
        {
            return ServiceResult.Fail($"Pierwsza wskazana oferta (ID: {firstOfferId}) nie istnieje.");
        }

        var secondOffer = GetOfferById(secondOfferId);
        if (secondOffer is null)
        {
            return ServiceResult.Fail($"Druga wskazana oferta (ID: {secondOfferId}) nie istnieje.");
        }

        if (firstOfferId == secondOfferId)
        {
            return ServiceResult.Fail("Oferta nie może być powiązana sama ze sobą.");
        }

        if (firstOffer.Status == OfferStatus.Withdrawn || secondOffer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult.Fail("Nie można łączyć ofert wycofanych – obie oferty muszą mieć status Zaplanowana.");
        }

        if (firstOffer.RelatedOfferIds.Contains(secondOfferId) || secondOffer.RelatedOfferIds.Contains(firstOfferId))
        {
            return ServiceResult.Fail("Nie można tworzyć duplikatów powiązań – wybrane oferty są już ze sobą połączone.");
        }

        firstOffer.RelatedOfferIds.Add(secondOfferId);
        secondOffer.RelatedOfferIds.Add(firstOfferId);

        return ServiceResult.Ok(
            $"Utworzono dwukierunkowe powiązanie między ofertą \"{firstOffer.Name}\" (ID: {firstOffer.Id}) a \"{secondOffer.Name}\" (ID: {secondOffer.Id}).");
    }

    public ServiceResult UnlinkOffers(int firstOfferId, int secondOfferId)
    {
        var firstOffer = GetOfferById(firstOfferId);
        if (firstOffer is null)
        {
            return ServiceResult.Fail($"Pierwsza wskazana oferta (ID: {firstOfferId}) nie istnieje.");
        }

        var secondOffer = GetOfferById(secondOfferId);
        if (secondOffer is null)
        {
            return ServiceResult.Fail($"Druga wskazana oferta (ID: {secondOfferId}) nie istnieje.");
        }

        bool removedFirst = firstOffer.RelatedOfferIds.Remove(secondOfferId);
        bool removedSecond = secondOffer.RelatedOfferIds.Remove(firstOfferId);

        if (!removedFirst && !removedSecond)
        {
            return ServiceResult.Fail("Wskazane oferty nie są ze sobą powiązane.");
        }

        return ServiceResult.Ok(
            $"Usunięto dwukierunkowe powiązanie między ofertą \"{firstOffer.Name}\" (ID: {firstOffer.Id}) a \"{secondOffer.Name}\" (ID: {secondOffer.Id}).");
    }

    private ServiceResult ValidateOfferInput(
        int attractionId,
        string? name,
        decimal price,
        int minParticipants,
        int? maxParticipants,
        out Attraction? attraction,
        out string cleanName)
    {
        cleanName = DomainValueHelper.CleanText(name);
        attraction = _attractionService.GetAttractionById(attractionId);

        if (attraction is null)
        {
            return ServiceResult.Fail($"Oferta musi być przypisana do istniejącej atrakcji – atrakcja o ID {attractionId} nie istnieje.");
        }

        if (cleanName.Length == 0)
        {
            return ServiceResult.Fail("Nazwa oferty nie może być pusta.");
        }

        if (price < 0)
        {
            return ServiceResult.Fail("Cena oferty nie może być ujemna.");
        }

        if (minParticipants < 1)
        {
            return ServiceResult.Fail("Minimalna liczba uczestników musi wynosić co najmniej 1.");
        }

        if (minParticipants > attraction.MaxParticipants)
        {
            return ServiceResult.Fail(
                $"Minimalna wielkość grupy ({minParticipants} os.) przekracza maksymalną pojemność atrakcji \"{attraction.Name}\" ({attraction.MaxParticipants} os.).");
        }

        if (maxParticipants.HasValue)
        {
            if (maxParticipants.Value < minParticipants)
            {
                return ServiceResult.Fail(
                    $"Maksymalna liczba osób oferty ({maxParticipants.Value}) nie może być mniejsza niż minimalna liczba osób ({minParticipants}).");
            }

            if (maxParticipants.Value > attraction.MaxParticipants)
            {
                return ServiceResult.Fail(
                    $"Maksymalna liczba osób oferty ({maxParticipants.Value}) nie może przekraczać maksymalnej pojemności atrakcji \"{attraction.Name}\" ({attraction.MaxParticipants} os.).");
            }
        }

        return ServiceResult.Ok("Dane oferty poprawne.");
    }

    private static int RemoveAllBidirectionalLinks(Offer offer)
    {
        int count = offer.RelatedOfferIds.Count;

        foreach (var otherOffer in InMemoryData.Offers.Where(o => o.Id != offer.Id))
        {
            otherOffer.RelatedOfferIds.Remove(offer.Id);
        }

        offer.RelatedOfferIds.Clear();
        return count;
    }
}
