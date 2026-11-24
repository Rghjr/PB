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
        var availableCheck = _offerService.GetAvailableOfferForUser(offerId);
        if (!availableCheck.IsSuccess || availableCheck.Data is null)
        {
            return ServiceResult<OfferConfiguration>.Fail(availableCheck.Message);
        }

        var configuration = new OfferConfiguration(availableCheck.Data);
        return ServiceResult<OfferConfiguration>.Ok(
            configuration,
            $"Rozpoczęto konfigurację oferty \"{availableCheck.Data.Name}\" (ID: {availableCheck.Data.Id}).");
    }

    public ServiceResult SynchronizeConfiguration(OfferConfiguration configuration)
    {
        var availableCheck = _offerService.GetAvailableOfferForUser(configuration.BaseOffer.Id);
        if (!availableCheck.IsSuccess || availableCheck.Data is null)
        {
            return ServiceResult.Fail(availableCheck.Message);
        }

        var existingBaseOffer = availableCheck.Data;
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

        return ToggleAddonInList(
            configuration.BaseOffer,
            configuration.SelectedAddons,
            addonId,
            $"oferty \"{configuration.BaseOffer.Name}\"");
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

        var availableRelated = _offerService.GetAvailableOfferForUser(relatedOfferId);
        if (!availableRelated.IsSuccess || availableRelated.Data is null)
        {
            return ServiceResult<SelectedRelatedOffer>.Fail(availableRelated.Message);
        }

        var relatedOffer = availableRelated.Data;
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

        return ToggleAddonInList(
            selectedRelated.Offer,
            selectedRelated.SelectedAddons,
            addonId,
            $"oferty powiązanej \"{selectedRelated.Offer.Name}\"");
    }

    public (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues, bool ResolvedPreviousIssues)
        PreviewBaseAddonImpact(OfferConfiguration configuration, int addonId, ParticipantProfile profile)
    {
        return PreviewMutationImpact(configuration, profile, clone =>
        {
            var addon = clone.BaseOffer.Addons.FirstOrDefault(a => a.Id == addonId);
            if (addon is not null && clone.SelectedAddons.All(a => a.Id != addonId))
            {
                clone.SelectedAddons.Add(addon);
            }
        });
    }

    public (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues)
        PreviewRelatedOfferImpact(OfferConfiguration configuration, int relatedOfferId, ParticipantProfile profile)
    {
        var (report, newIssues, _) = PreviewMutationImpact(configuration, profile, clone =>
        {
            var relatedOffer = _offerService.GetOfferById(relatedOfferId);
            if (relatedOffer is not null && clone.SelectedRelatedOffers.All(r => r.Offer.Id != relatedOfferId))
            {
                clone.SelectedRelatedOffers.Add(new SelectedRelatedOffer(relatedOffer));
            }
        });

        return (report, newIssues);
    }

    public (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues, bool ResolvedPreviousIssues)
        PreviewRelatedOfferAddonImpact(
            OfferConfiguration configuration,
            int relatedOfferId,
            int addonId,
            ParticipantProfile profile)
    {
        return PreviewMutationImpact(configuration, profile, clone =>
        {
            var selectedRelated = clone.SelectedRelatedOffers.FirstOrDefault(r => r.Offer.Id == relatedOfferId);
            var addon = selectedRelated?.Offer.Addons.FirstOrDefault(a => a.Id == addonId);
            if (selectedRelated is not null && addon is not null && selectedRelated.SelectedAddons.All(a => a.Id != addonId))
            {
                selectedRelated.SelectedAddons.Add(addon);
            }
        });
    }

    private static ServiceResult ToggleAddonInList(
        Offer ownerOffer,
        List<Addon> selectedList,
        int addonId,
        string targetDescription)
    {
        var addon = ownerOffer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (addon is null)
        {
            return ServiceResult.Fail($"Dodatek o ID {addonId} nie należy do {targetDescription}.");
        }

        var existing = selectedList.FirstOrDefault(a => a.Id == addonId);
        if (existing is not null)
        {
            selectedList.Remove(existing);
            return ServiceResult.Ok($"Usunięto dodatek \"{addon.Name}\" z {targetDescription}.");
        }

        selectedList.Add(addon);
        return ServiceResult.Ok($"Dodano dodatek \"{addon.Name}\" (+{addon.Price:F2} zł) do {targetDescription}.");
    }

    private static (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues, bool ResolvedPreviousIssues)
        PreviewMutationImpact(
            OfferConfiguration configuration,
            ParticipantProfile profile,
            Action<OfferConfiguration> applyChange)
    {
        var beforeReport = configuration.ValidateAgainst(profile);
        var clone = CloneConfiguration(configuration);
        applyChange(clone);

        var afterReport = clone.ValidateAgainst(profile);
        var beforeKeys = beforeReport.UnmetRequirements.Select(b => (b.FeatureId, b.Message)).ToHashSet();
        var newIssues = afterReport.UnmetRequirements
            .Where(a => !beforeKeys.Contains((a.FeatureId, a.Message)))
            .ToList();

        bool resolved = afterReport.UnmetRequirements.Count < beforeReport.UnmetRequirements.Count;
        return (afterReport, newIssues, resolved);
    }

    private static OfferConfiguration CloneConfiguration(OfferConfiguration source)
    {
        var copiedRelated = source.SelectedRelatedOffers
            .Select(rel => new SelectedRelatedOffer(rel.Offer, rel.SelectedAddons));

        return new OfferConfiguration(source.BaseOffer, source.SelectedAddons, copiedRelated);
    }
}
