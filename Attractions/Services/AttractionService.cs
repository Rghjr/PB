using Attractions.Data;
using Attractions.DTOs;
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

    public ServiceResult<Attraction> AddAttraction(string name, string description, string location)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<Attraction>.Fail("Nazwa atrakcji nie może być pusta.");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return ServiceResult<Attraction>.Fail("Lokalizacja atrakcji nie może być pusta.");
        }

        var attraction = new Attraction
        {
            Id = InMemoryData.GetNextAttractionId(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Location = location.Trim()
        };

        InMemoryData.Attractions.Add(attraction);
        return ServiceResult<Attraction>.Ok(attraction, $"Dodano atrakcję \"{attraction.Name}\" (ID: {attraction.Id}).");
    }

    public ServiceResult UpdateAttraction(int id, string name, string description, string location)
    {
        var attraction = GetAttractionById(id);
        if (attraction is null)
        {
            return ServiceResult.Fail($"Nie można edytować atrakcji – atrakcja o ID {id} nie istnieje.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult.Fail("Nazwa atrakcji nie może być pusta.");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return ServiceResult.Fail("Lokalizacja atrakcji nie może być pusta.");
        }

        attraction.Name = name.Trim();
        attraction.Description = description?.Trim() ?? string.Empty;
        attraction.Location = location.Trim();

        return ServiceResult.Ok($"Zaktualizowano atrakcję \"{attraction.Name}\" (ID: {attraction.Id}).");
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

        return ServiceResult.Ok(
            $"Usunięto atrakcję \"{attraction.Name}\" (ID: {id}), przypisane oferty ({offersToDelete.Count}) oraz ich dodatki ({deletedAddonsCount}).");
    }
}
