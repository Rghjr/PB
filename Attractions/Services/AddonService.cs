using Attractions.Data;
using Attractions.DTOs;
using Attractions.Models;

namespace Attractions.Services;

public class AddonService
{
    private readonly OfferService _offerService;

    public AddonService(OfferService offerService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
    }

    public ServiceResult<List<Addon>> GetAddonsForOffer(int offerId)
    {
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult<List<Addon>>.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        var addons = offer.Addons
            .OrderBy(a => a.Id)
            .ToList();

        return ServiceResult<List<Addon>>.Ok(addons, $"Pobrano dodatki dla oferty \"{offer.Name}\".");
    }

    public ServiceResult<Addon> AddAddon(int offerId, string name, string description, decimal price)
    {
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult<Addon>.Fail($"Dodatek musi należeć do istniejącej oferty – oferta o ID {offerId} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult<Addon>.Fail("Nie można dodawać dodatków do wycofanej oferty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<Addon>.Fail("Nazwa dodatku nie może być pusta.");
        }

        if (price < 0)
        {
            return ServiceResult<Addon>.Fail("Cena dodatku nie może być ujemna.");
        }

        var addon = new Addon
        {
            Id = InMemoryData.GetNextAddonId(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = decimal.Round(price, 2)
        };

        offer.Addons.Add(addon);
        return ServiceResult<Addon>.Ok(
            addon,
            $"Dodano dodatek \"{addon.Name}\" (ID: {addon.Id}) do oferty \"{offer.Name}\".");
    }

    public ServiceResult UpdateAddon(int offerId, int addonId, string name, string description, decimal price)
    {
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult.Fail("Nie można edytować dodatków wycofanej oferty.");
        }

        var addon = offer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Nie można edytować dodatku – dodatek o ID {addonId} nie istnieje w ofercie \"{offer.Name}\".");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult.Fail("Nazwa dodatku nie może być pusta.");
        }

        if (price < 0)
        {
            return ServiceResult.Fail("Cena dodatku nie może być ujemna.");
        }

        addon.Name = name.Trim();
        addon.Description = description?.Trim() ?? string.Empty;
        addon.Price = decimal.Round(price, 2);

        return ServiceResult.Ok($"Zaktualizowano dodatek \"{addon.Name}\" (ID: {addon.Id}) w ofercie \"{offer.Name}\".");
    }

    public ServiceResult RemoveAddon(int offerId, int addonId)
    {
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        var addon = offer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Nie można usunąć nieistniejącego dodatku – brak dodatku o ID {addonId} w ofercie \"{offer.Name}\".");
        }

        offer.Addons.Remove(addon);
        return ServiceResult.Ok($"Usunięto dodatek \"{addon.Name}\" (ID: {addonId}) z oferty \"{offer.Name}\".");
    }
}
