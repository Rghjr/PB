using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.Services;

public class AttractionService
{
    public List<Attraction> GetAllAttractions()
    {
        return InMemoryData.Attractions
            .OrderBy(a => a.Id)
            .ToList();
    }

    public Attraction? GetAttractionById(int id)
    {
        return InMemoryData.Attractions.FirstOrDefault(a => a.Id == id);
    }

    public List<Offer> GetOffersForAttraction(int attractionId)
    {
        return InMemoryData.Offers
            .Where(o => o.AttractionId == attractionId)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .ToList();
    }

    public ServiceResult<Attraction> AddAttraction(
        string name,
        string description,
        string location,
        IEnumerable<FeatureAssignment>? features = null,
        int maxParticipants = 30)
    {
        var validation = ValidateFields(name, location, maxParticipants, out string cleanName, out string cleanLocation);
        if (!validation.IsSuccess)
        {
            return ServiceResult<Attraction>.Fail(validation.Message);
        }

        var attraction = new Attraction(
            id: InMemoryData.GetNextAttractionId(),
            name: cleanName,
            description: DomainValueHelper.CleanText(description),
            location: cleanLocation,
            maxParticipants: maxParticipants,
            features: features);

        InMemoryData.Attractions.Add(attraction);
        JsonFeatureStore.TrySaveAssignments(out _);

        return ServiceResult<Attraction>.Ok(
            attraction,
            $"Dodano atrakcję \"{attraction.Name}\" (ID: {attraction.Id}, maks. osób: {attraction.MaxParticipants}).");
    }

    public ServiceResult UpdateAttraction(
        int id,
        string name,
        string description,
        string location,
        IEnumerable<FeatureAssignment>? features = null,
        int? maxParticipants = null)
    {
        var attraction = GetAttractionById(id);
        if (attraction is null)
        {
            return ServiceResult.Fail($"Nie można edytować atrakcji – atrakcja o ID {id} nie istnieje.");
        }

        int resolvedMax = maxParticipants ?? attraction.MaxParticipants;
        var validation = ValidateFields(name, location, resolvedMax, out string cleanName, out string cleanLocation);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        attraction.UpdateDetails(
            cleanName,
            DomainValueHelper.CleanText(description),
            cleanLocation,
            resolvedMax,
            features);

        JsonFeatureStore.TrySaveAssignments(out _);
        return ServiceResult.Ok(
            $"Zaktualizowano atrakcję \"{attraction.Name}\" (ID: {attraction.Id}, maks. osób: {attraction.MaxParticipants}).");
    }

    public ServiceResult DeleteAttraction(int id)
    {
        var attraction = GetAttractionById(id);
        if (attraction is null)
        {
            return ServiceResult.Fail($"Nie można usunąć atrakcji – atrakcja o ID {id} nie istnieje.");
        }

        var offersToDelete = InMemoryData.Offers
            .Where(o => o.AttractionId == id)
            .ToList();

        var deletedOfferIds = offersToDelete
            .Select(o => o.Id)
            .ToHashSet();

        int deletedAddonsCount = offersToDelete.Sum(o => o.Addons.Count);

        foreach (var remainingOffer in InMemoryData.Offers.Where(o => !deletedOfferIds.Contains(o.Id)))
        {
            remainingOffer.RelatedOfferIds.RemoveAll(relatedId => deletedOfferIds.Contains(relatedId));
        }

        foreach (var offer in offersToDelete)
        {
            offer.Addons.Clear();
            offer.RelatedOfferIds.Clear();
            InMemoryData.Offers.Remove(offer);
        }

        InMemoryData.Attractions.Remove(attraction);
        JsonFeatureStore.TrySaveAssignments(out _);

        return ServiceResult.Ok(
            $"Usunięto atrakcję \"{attraction.Name}\" (ID: {id}), przypisane oferty ({offersToDelete.Count}) oraz ich dodatki ({deletedAddonsCount}).");
    }

    private static ServiceResult ValidateFields(
        string? name,
        string? location,
        int maxParticipants,
        out string cleanName,
        out string cleanLocation)
    {
        cleanName = DomainValueHelper.CleanText(name);
        cleanLocation = DomainValueHelper.CleanText(location);

        if (cleanName.Length == 0)
        {
            return ServiceResult.Fail("Nazwa atrakcji nie może być pusta.");
        }

        if (cleanLocation.Length == 0)
        {
            return ServiceResult.Fail("Lokalizacja atrakcji nie może być pusta.");
        }

        if (maxParticipants < 1)
        {
            return ServiceResult.Fail("Maksymalna liczba osób na atrakcji musi wynosić co najmniej 1.");
        }

        return ServiceResult.Ok("Dane atrakcji poprawne.");
    }
}
