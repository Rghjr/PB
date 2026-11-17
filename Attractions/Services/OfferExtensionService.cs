using Attractions.DTOs;
using Attractions.Models;

namespace Attractions.Services;

public class OfferExtensionService
{
    private readonly OfferService _offerService;

    public OfferExtensionService(OfferService offerService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
    }

    public ServiceResult<OfferConfiguration> CreateConfiguration(int offerId)
    {
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            return ServiceResult<OfferConfiguration>.Fail($"Oferta o ID {offerId} nie istnieje.");
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult<OfferConfiguration>.Fail("Wycofanej oferty nie można rozszerzać o dodatki ani powiązane oferty.");
        }

        if (offer.Date.Date < DateTime.Today)
        {
            return ServiceResult<OfferConfiguration>.Fail(
                $"Termin oferty \"{offer.Name}\" ({offer.Date:yyyy-MM-dd}) już minął – oferta nie jest dostępna dla użytkownika.");
        }

        var configuration = new OfferConfiguration(offer);
        return ServiceResult<OfferConfiguration>.Ok(
            configuration,
            $"Rozpoczęto konfigurację oferty \"{offer.Name}\" (ID: {offer.Id}).");
    }

    public ServiceResult SynchronizeConfiguration(OfferConfiguration configuration)
    {
        var existingBaseOffer = _offerService.GetOfferById(configuration.BaseOffer.Id);
        if (existingBaseOffer is null)
        {
            return ServiceResult.Fail("Konfigurowana oferta została usunięta i nie jest już dostępna.");
        }

        if (existingBaseOffer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult.Fail("Konfigurowana oferta została wycofana i nie można jej dalej rozszerzać.");
        }

        if (existingBaseOffer.Date.Date < DateTime.Today)
        {
            return ServiceResult.Fail("Termin konfigurowanej oferty już minął.");
        }

        var validBaseAddonIds = existingBaseOffer.Addons.Select(a => a.Id).ToHashSet();
        configuration.SelectedAddons.RemoveAll(a => !validBaseAddonIds.Contains(a.Id));

        var validRelatedIds = _offerService
            .GetRelatedOffers(existingBaseOffer.Id, onlyAvailableForUser: true)
            .Select(o => o.Id)
            .ToHashSet();

        configuration.SelectedRelatedOffers.RemoveAll(r => !validRelatedIds.Contains(r.Offer.Id));

        foreach (var selectedRelated in configuration.SelectedRelatedOffers)
        {
            var validAddonIds = selectedRelated.Offer.Addons.Select(a => a.Id).ToHashSet();
            selectedRelated.SelectedAddons.RemoveAll(a => !validAddonIds.Contains(a.Id));
        }

        return ServiceResult.Ok("Konfiguracja jest aktualna.");
    }

    public ServiceResult ToggleBaseAddon(OfferConfiguration configuration, int addonId)
    {
        var syncResult = SynchronizeConfiguration(configuration);
        if (!syncResult.IsSuccess)
        {
            return syncResult;
        }

        var addon = configuration.BaseOffer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Dodatek o ID {addonId} nie należy do oferty \"{configuration.BaseOffer.Name}\".");
        }

        var existing = configuration.SelectedAddons.FirstOrDefault(a => a.Id == addonId);
        if (existing is not null)
        {
            configuration.SelectedAddons.Remove(existing);
            return ServiceResult.Ok($"Usunięto dodatek \"{addon.Name}\" z konfiguracji oferty.");
        }

        configuration.SelectedAddons.Add(addon);
        return ServiceResult.Ok($"Dodano dodatek \"{addon.Name}\" (+{addon.Price:F2} zł) do konfiguracji oferty.");
    }

    public ServiceResult ClearBaseAddons(OfferConfiguration configuration)
    {
        configuration.SelectedAddons.Clear();
        return ServiceResult.Ok("Wyczyszczono wszystkie wybrane dodatki dla oferty głównej.");
    }

    public SelectedRelatedOffer? GetSelectedRelatedOffer(OfferConfiguration configuration, int relatedOfferId)
    {
        return configuration.SelectedRelatedOffers.FirstOrDefault(r => r.Offer.Id == relatedOfferId);
    }

    public ServiceResult<SelectedRelatedOffer> AddRelatedOffer(OfferConfiguration configuration, int relatedOfferId)
    {
        var syncResult = SynchronizeConfiguration(configuration);
        if (!syncResult.IsSuccess)
        {
            return ServiceResult<SelectedRelatedOffer>.Fail(syncResult.Message);
        }

        if (!configuration.BaseOffer.RelatedOfferIds.Contains(relatedOfferId))
        {
            return ServiceResult<SelectedRelatedOffer>.Fail(
                $"Oferta o ID {relatedOfferId} nie jest powiązana z ofertą \"{configuration.BaseOffer.Name}\".");
        }

        var relatedOffer = _offerService.GetOfferById(relatedOfferId);
        if (relatedOffer is null)
        {
            return ServiceResult<SelectedRelatedOffer>.Fail($"Powiązana oferta o ID {relatedOfferId} nie istnieje.");
        }

        if (relatedOffer.Status == OfferStatus.Withdrawn)
        {
            return ServiceResult<SelectedRelatedOffer>.Fail("Nie można rozszerzyć oferty o wycofaną ofertę powiązaną.");
        }

        if (relatedOffer.Date.Date < DateTime.Today)
        {
            return ServiceResult<SelectedRelatedOffer>.Fail("Termin wybranej oferty powiązanej już minął.");
        }

        var alreadySelected = GetSelectedRelatedOffer(configuration, relatedOfferId);
        if (alreadySelected is not null)
        {
            return ServiceResult<SelectedRelatedOffer>.Ok(
                alreadySelected,
                $"Oferta powiązana \"{relatedOffer.Name}\" znajduje się już w konfiguracji.");
        }

        var selection = new SelectedRelatedOffer(relatedOffer);
        configuration.SelectedRelatedOffers.Add(selection);

        return ServiceResult<SelectedRelatedOffer>.Ok(
            selection,
            $"Dołączono powiązaną ofertę \"{relatedOffer.Name}\" (+{relatedOffer.Price:F2} zł).");
    }

    public ServiceResult RemoveRelatedOffer(OfferConfiguration configuration, int relatedOfferId)
    {
        var existing = GetSelectedRelatedOffer(configuration, relatedOfferId);
        if (existing is null)
        {
            return ServiceResult.Fail($"Oferta o ID {relatedOfferId} nie znajduje się w aktualnej konfiguracji.");
        }

        configuration.SelectedRelatedOffers.Remove(existing);
        return ServiceResult.Ok($"Usunięto powiązaną ofertę \"{existing.Offer.Name}\" (wraz z jej wybranymi dodatkami) z konfiguracji.");
    }

    public ServiceResult ToggleRelatedOfferAddon(OfferConfiguration configuration, int relatedOfferId, int addonId)
    {
        var syncResult = SynchronizeConfiguration(configuration);
        if (!syncResult.IsSuccess)
        {
            return syncResult;
        }

        var selectedRelated = GetSelectedRelatedOffer(configuration, relatedOfferId);
        if (selectedRelated is null)
        {
            return ServiceResult.Fail($"Oferta powiązana o ID {relatedOfferId} nie została jeszcze dołączona do konfiguracji.");
        }

        var addon = selectedRelated.Offer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Dodatek o ID {addonId} nie należy do oferty powiązanej \"{selectedRelated.Offer.Name}\".");
        }

        var existingAddon = selectedRelated.SelectedAddons.FirstOrDefault(a => a.Id == addonId);
        if (existingAddon is not null)
        {
            selectedRelated.SelectedAddons.Remove(existingAddon);
            return ServiceResult.Ok($"Usunięto dodatek \"{addon.Name}\" z oferty powiązanej \"{selectedRelated.Offer.Name}\".");
        }

        selectedRelated.SelectedAddons.Add(addon);
        return ServiceResult.Ok(
            $"Dodano dodatek \"{addon.Name}\" (+{addon.Price:F2} zł) do oferty powiązanej \"{selectedRelated.Offer.Name}\".");
    }
}
