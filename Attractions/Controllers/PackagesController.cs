using Attractions.Data;
using Attractions.DTOs.Api;
using Attractions.Helpers;
using Attractions.Models;
using Attractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Attractions.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly OfferExtensionService _extensionService;
    private readonly AttractionService _attractionService;
    private readonly OrderService _orderService;
    private readonly FeatureService _featureService;

    public PackagesController(
        OfferExtensionService extensionService,
        AttractionService attractionService,
        OrderService orderService,
        FeatureService featureService)
    {
        _extensionService = extensionService;
        _attractionService = attractionService;
        _orderService = orderService;
        _featureService = featureService;
    }

    [HttpGet("discount-policy")]
    public ActionResult GetDiscountPolicy()
    {
        var categories = Enum.GetValues<TicketCategory>()
            .Select(c => new
            {
                category = c,
                name = TicketDiscountPolicy.GetCategoryDisplayName(c),
                discountPercent = TicketDiscountPolicy.GetCategoryDiscountPercent(c)
            })
            .ToList();

        return Ok(new
        {
            categories,
            groupTiers = new[]
            {
                new { minParticipants = 4, maxParticipants = 7, extraDiscountPercent = 5m },
                new { minParticipants = 8, maxParticipants = 100, extraDiscountPercent = 10m }
            }
        });
    }

    [HttpPost("evaluate")]
    public ActionResult<PackageEvaluationResponse> EvaluatePackage([FromBody] EvaluatePackageRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var buildRes = ApiMapper.ReconstructConfiguration(request.Configuration, _extensionService);
            if (!buildRes.IsSuccess || buildRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(buildRes.Message));
            }

            var config = buildRes.Data;
            var allDefinitions = _featureService.GetAllFeatures();
            var definitionsById = allDefinitions.ToDictionary(f => f.Id);

            ParticipantProfile? profile = request.Profile is not null
                ? ApiMapper.ToDomain(request.Profile)
                : null;

            var (pkgMin, pkgMax, minSrc, maxSrc) = config.GetPackageGroupSizeBounds(_attractionService.GetAttractionById);
            bool hasConflict = pkgMin > pkgMax;
            var (minRemainingSpots, limitingSource) = _orderService.GetPackageRemainingSpots(config);

            string groupSizeLabel = hasConflict
                ? $"SPRZECZNOŚĆ ({minSrc}: min. {pkgMin} vs {maxSrc}: maks. {pkgMax})"
                : (pkgMin > 1 && pkgMin == pkgMax
                    ? $"TYLKO GRUPA {pkgMin} OS."
                    : $"{pkgMin}–{pkgMax} os.");

            var suggestedBreakdown = profile is not null
                ? profile.ToGroupBreakdown()
                : ParticipantGroupBreakdown.FromAdultsCount(Math.Max(1, pkgMin));

            var effectiveBreakdown = request.GroupBreakdown is not null
                ? ApiMapper.ToDomain(request.GroupBreakdown)
                : suggestedBreakdown;

            if (effectiveBreakdown.TotalCount < 1)
            {
                effectiveBreakdown = suggestedBreakdown;
            }

            var priceCalc = config.CalculatePriceBreakdown(effectiveBreakdown);

            CompatibilityReportDto? compatibilityDto = profile is not null
                ? ApiMapper.ToDto(config.ValidateAgainst(profile, allDefinitions, _attractionService.GetAttractionById))
                : null;

            var packageFeatures = config
                .GetPackageEffectiveFeatures(allDefinitions, _attractionService.GetAttractionById)
                .Values
                .OrderBy(f => f.FeatureId)
                .Select(f => ApiMapper.ToEffectiveView(f, definitionsById))
                .ToList();

            var selectedBaseAddons = config.SelectedAddons
                .OrderBy(a => a.Id)
                .Select(a => ApiMapper.ToDto(a, definitionsById))
                .ToList();

            var selectedRelatedDtos = config.SelectedRelatedOffers
                .OrderBy(r => r.Offer.Id)
                .Select(r =>
                {
                    var relAttr = _attractionService.GetAttractionById(r.Offer.AttractionId);
                    var relSummary = ApiMapper.ToSummaryDto(r.Offer, relAttr, _orderService, definitionsById, profile);
                    var availAddons = r.Offer.Addons.OrderBy(a => a.Id).Select(a => ApiMapper.ToDto(a, definitionsById)).ToList();
                    var chosenAddons = r.SelectedAddons.OrderBy(a => a.Id).Select(a => ApiMapper.ToDto(a, definitionsById)).ToList();

                    return new SelectedRelatedOfferViewDto(
                        relSummary,
                        availAddons,
                        chosenAddons,
                        r.BasePrice,
                        r.AddonsPrice,
                        r.Subtotal);
                })
                .ToList();

            var baseAttraction = _attractionService.GetAttractionById(config.BaseOffer.AttractionId);

            return Ok(new PackageEvaluationResponse(
                config.BaseOffer.Id,
                config.BaseOffer.Name,
                baseAttraction?.Name ?? "(nieznana atrakcja)",
                config.BaseOffer.Date.ToString("yyyy-MM-dd"),
                config.BasePrice,
                config.BaseAddonsPrice,
                config.RelatedOffersBasePrice,
                config.RelatedOffersAddonsPrice,
                config.TotalPrice,
                pkgMin,
                pkgMax,
                minRemainingSpots,
                limitingSource,
                hasConflict,
                groupSizeLabel,
                selectedBaseAddons,
                selectedRelatedDtos,
                packageFeatures,
                compatibilityDto,
                ApiMapper.ToDto(suggestedBreakdown),
                ApiMapper.ToDto(priceCalc)));
        }
    }

    [HttpPost("preview-change")]
    public ActionResult<PreviewPackageChangeResponse> PreviewChange([FromBody] PreviewPackageChangeRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var buildRes = ApiMapper.ReconstructConfiguration(request.Configuration, _extensionService);
            if (!buildRes.IsSuccess || buildRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(buildRes.Message));
            }

            var config = buildRes.Data;
            var profile = ApiMapper.ToDomain(request.Profile);

            string kind = DomainValueHelper.CleanText(request.ChangeType).ToLowerInvariant();
            return kind switch
            {
                "baseaddon" => ToPreviewResponse(_extensionService.PreviewBaseAddonImpact(config, request.TargetAddonId, profile)),
                "relatedoffer" => ToPreviewResponse(
                    _extensionService.PreviewRelatedOfferImpact(config, request.TargetOfferId, profile),
                    resolved: false),
                "relatedaddon" => ToPreviewResponse(
                    _extensionService.PreviewRelatedOfferAddonImpact(config, request.TargetOfferId, request.TargetAddonId, profile)),
                _ => BadRequest(new ApiErrorResponse("Nieznany typ symulowanej zmiany pakietu."))
            };
        }
    }

    private ActionResult<PreviewPackageChangeResponse> ToPreviewResponse(
        (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues, bool ResolvedPreviousIssues) tuple)
    {
        return Ok(new PreviewPackageChangeResponse(
            tuple.SimulatedReport.IsCompatible,
            tuple.ResolvedPreviousIssues,
            tuple.NewIssues.Select(ApiMapper.ToDto).ToList(),
            ApiMapper.ToDto(tuple.SimulatedReport)));
    }

    private ActionResult<PreviewPackageChangeResponse> ToPreviewResponse(
        (CompatibilityReport SimulatedReport, List<RequirementCheckResult> NewIssues) tuple,
        bool resolved)
    {
        return Ok(new PreviewPackageChangeResponse(
            tuple.SimulatedReport.IsCompatible,
            resolved,
            tuple.NewIssues.Select(ApiMapper.ToDto).ToList(),
            ApiMapper.ToDto(tuple.SimulatedReport)));
    }
}
