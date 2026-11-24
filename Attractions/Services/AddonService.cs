using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
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

    public ServiceResult<Addon> AddAddon(
        int offerId,
        string name,
        string description,
        decimal price,
        IEnumerable<FeatureAssignment>? featureModifiers = null)
    {
        var validation = ValidateEditableOfferAndFields(
            offerId,
            name,
            price,
            "Nie można dodawać dodatków do wycofanej oferty.",
            out var offer,
            out string cleanName);

        if (!validation.IsSuccess || offer is null)
        {
            return ServiceResult<Addon>.Fail(validation.Message);
        }

        var addon = new Addon(
            id: InMemoryData.GetNextAddonId(),
            name: cleanName,
            description: DomainValueHelper.CleanText(description),
            price: price,
            featureModifiers: featureModifiers);

        offer.Addons.Add(addon);
        JsonFeatureStore.TrySaveAssignments(out _);

        return ServiceResult<Addon>.Ok(
            addon,
            $"Dodano dodatek \"{addon.Name}\" (ID: {addon.Id}) do oferty \"{offer.Name}\".");
    }

    public ServiceResult UpdateAddon(
        int offerId,
        int addonId,
        string name,
        string description,
        decimal price,
        IEnumerable<FeatureAssignment>? featureModifiers = null)
    {
        var validation = ValidateEditableOfferAndFields(
            offerId,
            name,
            price,
            "Nie można edytować dodatków wycofanej oferty.",
            out var offer,
            out string cleanName);

        if (!validation.IsSuccess || offer is null)
        {
            return validation;
        }

        var addon = offer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Nie można edytować dodatku – dodatek o ID {addonId} nie istnieje w ofercie \"{offer.Name}\".");
        }

        addon.UpdateDetails(
            cleanName,
            DomainValueHelper.CleanText(description),
            price,
            featureModifiers);

        JsonFeatureStore.TrySaveAssignments(out _);
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
        JsonFeatureStore.TrySaveAssignments(out _);
        return ServiceResult.Ok($"Usunięto dodatek \"{addon.Name}\" (ID: {addonId}) z oferty \"{offer.Name}\".");
    }

    private ServiceResult ValidateEditableOfferAndFields(
        int offerId,
        string? name,
        decimal price,
        string withdrawnErrorMessage,
        out Offer? offer,
        out string cleanName)
    {
        cleanName = DomainValueHelper.CleanText(name);
        offer = _offerService.GetOfferById(offerId);

        if (offer is null)
        {
            return ServiceResult.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult.Fail(withdrawnErrorMessage);
        }

        if (cleanName.Length == 0)
        {
            return ServiceResult.Fail("Nazwa dodatku nie może być pusta.");
        }

        if (price < 0)
        {
            return ServiceResult.Fail("Cena dodatku nie może być ujemna.");
        }

        return ServiceResult.Ok("Dane dodatku poprawne.");
    }
}
