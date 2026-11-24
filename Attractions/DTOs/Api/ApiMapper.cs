using Attractions.Data;
using Attractions.Helpers;
using Attractions.Menus;
using Attractions.Models;
using Attractions.Services;

namespace Attractions.DTOs.Api;

public static class ApiMapper
{
    public static FeatureDefinitionDto ToDto(FeatureDefinition def, int usageCount)
    {
        return new FeatureDefinitionDto(
            def.Id,
            def.Name,
            def.Type,
            def.Description,
            def.EffectiveRule,
            def.Unit,
            def.AllowedValues,
            def.ProfileBindingKey,
            usageCount);
    }

    public static FeatureUsageDto ToFeatureUsageDto(FeatureDefinition feature, FeatureUsageInfo usage)
    {
        return new FeatureUsageDto(
            feature.Id,
            feature.Name,
            usage.TotalCount,
            usage.Attractions.Select(a => new NamedEntityRefDto(a.Id, a.Name)).ToList(),
            usage.Offers.Select(o => new NamedEntityRefDto(o.Id, o.Name)).ToList(),
            usage.Addons.Select(x => new NamedEntityRefDto(x.Addon.Id, $"{x.Addon.Name} (oferta #{x.ParentOffer.Id})")).ToList());
    }

    public static ServiceResult ValidateFeatureInputs(
        IEnumerable<FeatureAssignmentInputDto>? inputs,
        FeatureService featureService,
        out List<FeatureAssignment>? normalizedFeatures)
    {
        normalizedFeatures = null;
        if (inputs is null)
        {
            return ServiceResult.Ok("Brak przypisań cech do walidacji.");
        }

        var raw = inputs.Select(f => new FeatureAssignment(f.FeatureId, f.Value, f.EffectMode));
        var valRes = featureService.ValidateAndNormalizeAssignments(raw, out var list);
        if (!valRes.IsSuccess)
        {
            return valRes;
        }

        normalizedFeatures = list;
        return ServiceResult.Ok("Przypisania cech są poprawne.");
    }

    public static AssignedFeatureViewDto ToAssignedView(
        FeatureAssignment assignment,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById)
    {
        if (definitionsById.TryGetValue(assignment.FeatureId, out var def))
        {
            return new AssignedFeatureViewDto(
                def.Id,
                def.Name,
                def.Type,
                assignment.Value,
                def.FormatValue(assignment.Value),
                assignment.EffectMode);
        }

        return new AssignedFeatureViewDto(
            assignment.FeatureId,
            $"Cecha #{assignment.FeatureId}",
            FeatureValueType.Text,
            assignment.Value,
            assignment.Value,
            assignment.EffectMode);
    }

    public static EffectiveFeatureViewDto ToEffectiveView(
        EffectiveFeatureValue entry,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById,
        Offer? sourceOffer = null)
    {
        bool isDirect = sourceOffer?.GetDirectFeatureValue(entry.FeatureId) is not null;

        if (definitionsById.TryGetValue(entry.FeatureId, out var def))
        {
            return new EffectiveFeatureViewDto(
                def.Id,
                def.Name,
                def.Type,
                entry.Value,
                def.FormatValue(entry.Value),
                entry.SourceElement,
                isDirect,
                entry.OverridesAttractionDefault,
                entry.ModifiedByAddon);
        }

        return new EffectiveFeatureViewDto(
            entry.FeatureId,
            $"Cecha #{entry.FeatureId}",
            FeatureValueType.Text,
            entry.Value,
            entry.Value,
            entry.SourceElement,
            isDirect,
            entry.OverridesAttractionDefault,
            entry.ModifiedByAddon);
    }

    public static AttractionSummaryDto ToSummaryDto(
        Attraction attraction,
        AttractionService attractionService,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById)
    {
        var offers = attractionService.GetOffersForAttraction(attraction.Id);
        int availableCount = offers.Count(o => o.IsAvailableForUser);

        var assignedViews = attraction.Features
            .OrderBy(f => f.FeatureId)
            .Select(f => ToAssignedView(f, definitionsById))
            .ToList();

        return new AttractionSummaryDto(
            attraction.Id,
            attraction.Name,
            attraction.Description,
            attraction.Location,
            attraction.MaxParticipants,
            offers.Count,
            availableCount,
            assignedViews);
    }

    public static AddonDto ToDto(Addon addon, IReadOnlyDictionary<int, FeatureDefinition> definitionsById)
    {
        var mods = addon.FeatureModifiers
            .OrderBy(m => m.FeatureId)
            .Select(m => ToAssignedView(m, definitionsById))
            .ToList();

        return new AddonDto(addon.Id, addon.Name, addon.Description, addon.Price, mods);
    }

    public static OfferSummaryDto ToSummaryDto(
        Offer offer,
        Attraction? attraction,
        OrderService orderService,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById,
        ParticipantProfile? activeProfile = null)
    {
        var effectiveDict = offer.GetEffectiveFeatures(attraction, selectedAddons: null, InMemoryData.Features);
        var effectiveList = effectiveDict.Values
            .OrderBy(e => e.FeatureId)
            .Select(e => ToEffectiveView(e, definitionsById, offer))
            .ToList();

        int effectiveMax = offer.GetEffectiveMaxParticipants(attraction);
        int booked = orderService.GetBookedParticipantsForOffer(offer.Id);
        int remainingOffer = Math.Max(0, effectiveMax - booked);

        if (attraction is not null)
        {
            int remainingAttr = orderService.GetRemainingAttractionSpotsOnDate(attraction, offer.Date);
            remainingOffer = Math.Min(remainingOffer, remainingAttr);
        }

        CompatibilityReportDto? compatDto = activeProfile is not null
            ? ToDto(offer.ValidateAgainst(activeProfile, attraction, selectedAddons: null, InMemoryData.Features))
            : null;

        return new OfferSummaryDto(
            offer.Id,
            offer.AttractionId,
            attraction?.Name ?? "(nieznana atrakcja)",
            attraction?.Location ?? string.Empty,
            offer.Name,
            offer.Description,
            offer.Price,
            offer.Date.ToString("yyyy-MM-dd"),
            offer.Status,
            ConsoleHelper.FormatOfferStatus(offer),
            offer.IsToday,
            offer.IsFuturePlanned,
            offer.IsAvailableForUser,
            offer.IsGroupOffer,
            offer.IsExactGroupSizeOffer(attraction),
            offer.MinParticipants,
            offer.MaxParticipants,
            effectiveMax,
            offer.FormatGroupSizeLimit(attraction),
            booked,
            remainingOffer,
            offer.Addons.Count,
            offer.RelatedOfferIds.Count,
            effectiveList,
            compatDto);
    }

    public static CompatibilityReportDto ToDto(CompatibilityReport report)
    {
        return new CompatibilityReportDto(
            report.TargetName,
            report.TargetTypeLabel,
            report.IsCompatible,
            report.MetRequirements.Select(ToDto).ToList(),
            report.UnmetRequirements.Select(ToDto).ToList(),
            report.Warnings.Select(ToDto).ToList(),
            report.IncompatibilityReasons);
    }

    public static RequirementCheckDto ToDto(RequirementCheckResult r)
    {
        return new RequirementCheckDto(
            r.FeatureId,
            r.FeatureName,
            r.Status.ToString(),
            r.RequiredValueText,
            r.ParticipantValueText,
            r.SourceElement,
            r.Message,
            r.FormattedMessage);
    }

    public static ParticipantProfile ToDomain(ParticipantProfileDto dto)
    {
        var ages = dto.ParticipantAges?.Where(a => a is >= 0 and <= 120).ToList() ?? new List<int>();
        var customNums = (dto.CustomNumericValues ?? new Dictionary<int, List<decimal>>())
            .Where(kv => kv.Value is { Count: > 0 })
            .ToDictionary(kv => kv.Key, kv => new List<decimal>(kv.Value));

        var customTexts = (dto.CustomOptionOrTextValues ?? new Dictionary<int, string>())
            .Select(kv => (kv.Key, Val: DomainValueHelper.CleanText(kv.Value)))
            .Where(x => x.Val.Length > 0)
            .ToDictionary(x => x.Key, x => x.Val);

        return new ParticipantProfile(
            participantAges: ages,
            minParticipantHeightCm: dto.MinParticipantHeightCm,
            requiresWheelchairAccess: dto.RequiresWheelchairAccess,
            disabledParticipantsCount: Math.Max(0, dto.DisabledParticipantsCount),
            maxPhysicalDemand: DomainValueHelper.CleanNullableText(dto.MaxPhysicalDemand),
            explicitAdultGuardianPresent: dto.ExplicitAdultGuardianPresent,
            requiresChildFriendly: dto.RequiresChildFriendly,
            requiresStrollerAccess: dto.RequiresStrollerAccess,
            requiresElevator: dto.RequiresElevator,
            requiredFacilityFeatureIds: dto.RequiredFacilityFeatureIds,
            confirmedConditionFeatureIds: dto.ConfirmedConditionFeatureIds,
            customNumericValues: customNums,
            customOptionOrTextValues: customTexts);
    }

    public static ParticipantGroupBreakdown ToDomain(ParticipantGroupBreakdownDto dto)
    {
        return new ParticipantGroupBreakdown(
            dto.AdultsCount,
            dto.ChildrenCount,
            dto.YouthOrStudentsCount,
            dto.SeniorsCount,
            dto.DisabledCount);
    }

    public static ParticipantGroupBreakdownDto ToDto(ParticipantGroupBreakdown domain)
    {
        return new ParticipantGroupBreakdownDto
        {
            AdultsCount = domain.AdultsCount,
            ChildrenCount = domain.ChildrenCount,
            YouthOrStudentsCount = domain.YouthOrStudentsCount,
            SeniorsCount = domain.SeniorsCount,
            DisabledCount = domain.DisabledCount
        };
    }

    public static PackagePriceBreakdownDto ToDto(PackagePriceBreakdown calc)
    {
        return new PackagePriceBreakdownDto(
            calc.BasePricePerPerson,
            calc.TotalParticipants,
            calc.RegularGrossTotal,
            calc.SubtotalAfterCategoryDiscounts,
            calc.CategoryDiscountsSavings,
            calc.ExtraGroupDiscountPercent,
            calc.ExtraGroupDiscountAmount,
            calc.FinalTotalPrice,
            calc.TotalSavings,
            calc.CategoryLines.Select(ToDto).ToList());
    }

    public static PriceCategoryLineDto ToDto(OrderParticipantGroupItem item)
    {
        return new PriceCategoryLineDto(
            item.Category,
            item.CategoryName,
            item.Count,
            item.DiscountPercent,
            item.BaseUnitPrice,
            item.FinalUnitPrice,
            item.LineSubtotal);
    }

    public static OrderDto ToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            order.CustomerName,
            order.CustomerEmail,
            order.TicketsCount,
            order.PaymentMethod,
            ConsoleHelper.FormatPaymentMethod(order.PaymentMethod),
            order.PricePerPerson,
            order.RegularGrossPrice,
            order.SubtotalAfterCategoryDiscounts,
            order.CategoryDiscountsSavings,
            order.ExtraGroupDiscountPercent,
            order.ExtraGroupDiscountAmount,
            order.TotalPrice,
            order.TotalSavings,
            order.Items.Select(i => new OrderItemDto(
                i.OfferId,
                i.OfferName,
                i.AttractionName,
                i.Date.ToString("yyyy-MM-dd"),
                i.BasePrice,
                i.IsMainOffer,
                i.AddonsSubtotal,
                i.ItemSubtotal,
                i.SelectedAddons.Select(a => new OrderAddonDto(a.AddonId, a.Name, a.Price)).ToList(),
                i.EffectiveFeaturesSnapshot)).ToList(),
            order.ParticipantGroups.Select(ToDto).ToList());
    }

    public static ServiceResult<OfferConfiguration> ReconstructConfiguration(
        PackageConfigurationInputDto input,
        OfferExtensionService extensionService)
    {
        var createRes = extensionService.CreateConfiguration(input.BaseOfferId);
        if (!createRes.IsSuccess || createRes.Data is null)
        {
            return createRes;
        }

        var config = createRes.Data;

        foreach (int addonId in input.SelectedAddonIds.Distinct())
        {
            var toggleRes = extensionService.ToggleBaseAddon(config, addonId);
            if (!toggleRes.IsSuccess)
            {
                return ServiceResult<OfferConfiguration>.Fail(toggleRes.Message);
            }
        }

        foreach (var relInput in input.RelatedOffers)
        {
            var addRelRes = extensionService.AddRelatedOffer(config, relInput.OfferId);
            if (!addRelRes.IsSuccess)
            {
                return ServiceResult<OfferConfiguration>.Fail(addRelRes.Message);
            }

            foreach (int relAddonId in relInput.SelectedAddonIds.Distinct())
            {
                var relAddonRes = extensionService.ToggleRelatedOfferAddon(config, relInput.OfferId, relAddonId);
                if (!relAddonRes.IsSuccess)
                {
                    return ServiceResult<OfferConfiguration>.Fail(relAddonRes.Message);
                }
            }
        }

        return ServiceResult<OfferConfiguration>.Ok(config, "Zbudowano konfigurację pakietu.");
    }
}
