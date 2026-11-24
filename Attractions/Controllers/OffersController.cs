using System.Globalization;
using Attractions.Data;
using Attractions.DTOs.Api;
using Attractions.Helpers;
using Attractions.Models;
using Attractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Attractions.Controllers;

[ApiController]
[Route("api/offers")]
public class OffersController : ControllerBase
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;
    private readonly AddonService _addonService;
    private readonly OrderService _orderService;
    private readonly FeatureService _featureService;

    public OffersController(
        OfferService offerService,
        AttractionService attractionService,
        AddonService addonService,
        OrderService orderService,
        FeatureService featureService)
    {
        _offerService = offerService;
        _attractionService = attractionService;
        _addonService = addonService;
        _orderService = orderService;
        _featureService = featureService;
    }

    [HttpGet]
    public ActionResult<List<OfferSummaryDto>> GetOffers(
        [FromQuery] string filter = "available",
        [FromQuery] int? attractionId = null,
        [FromQuery] int? groupSize = null)
    {
        lock (InMemoryData.SyncRoot)
        {
            var offers = ResolveFilteredOffers(filter, attractionId, groupSize);
            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);

            var dtos = offers
                .Select(o => ApiMapper.ToSummaryDto(
                    o,
                    _attractionService.GetAttractionById(o.AttractionId),
                    _orderService,
                    definitionsById))
                .ToList();

            return Ok(dtos);
        }
    }

    [HttpPost("search")]
    public ActionResult<List<OfferSummaryDto>> SearchWithProfile([FromBody] OfferSearchRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var offers = ResolveFilteredOffers(request.Filter, request.AttractionId, request.GroupSize);
            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            ParticipantProfile? profile = request.Profile is not null
                ? ApiMapper.ToDomain(request.Profile)
                : null;

            var dtos = offers
                .Select(o => ApiMapper.ToSummaryDto(
                    o,
                    _attractionService.GetAttractionById(o.AttractionId),
                    _orderService,
                    definitionsById,
                    profile))
                .Where(dto => !request.OnlyCompatible || profile is null || (dto.Compatibility?.IsCompatible ?? true))
                .ToList();

            return Ok(dtos);
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<OfferDetailDto> GetById(int id, [FromQuery] bool onlyAvailableRelated = false)
    {
        lock (InMemoryData.SyncRoot)
        {
            var offer = _offerService.GetOfferById(id);
            if (offer is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            return Ok(BuildOfferDetailDto(offer, onlyAvailableRelated, activeProfile: null));
        }
    }

    [HttpPost("{id:int}/details-with-profile")]
    public ActionResult<OfferDetailDto> GetByIdWithProfile(
        int id,
        [FromQuery] bool onlyAvailableRelated = true,
        [FromBody] ParticipantProfileDto? profileDto = null)
    {
        lock (InMemoryData.SyncRoot)
        {
            var offer = _offerService.GetOfferById(id);
            if (offer is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            var profile = profileDto is not null ? ApiMapper.ToDomain(profileDto) : null;
            return Ok(BuildOfferDetailDto(offer, onlyAvailableRelated, profile));
        }
    }

    [HttpPost]
    public ActionResult<OfferDetailDto> Create([FromBody] UpsertOfferRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (!TryParseIsoDate(request.Date, out DateTime parsedDate))
            {
                return BadRequest(new ApiErrorResponse("Nieprawidłowy format daty oferty (wymagany RRRR-MM-DD)."));
            }

            var valRes = ApiMapper.ValidateFeatureInputs(request.Features, _featureService, out var normalizedFeatures);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var addRes = _offerService.AddOffer(
                request.AttractionId,
                request.Name,
                request.Description,
                request.Price,
                parsedDate,
                normalizedFeatures,
                request.MinParticipants,
                request.MaxParticipants);

            if (!addRes.IsSuccess || addRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(addRes.Message));
            }

            var detail = BuildOfferDetailDto(addRes.Data, onlyAvailableRelated: false, activeProfile: null);
            return CreatedAtAction(nameof(GetById), new { id = addRes.Data.Id }, detail);
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<OfferDetailDto> Update(int id, [FromBody] UpsertOfferRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_offerService.GetOfferById(id) is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            if (!TryParseIsoDate(request.Date, out DateTime parsedDate))
            {
                return BadRequest(new ApiErrorResponse("Nieprawidłowy format daty oferty (wymagany RRRR-MM-DD)."));
            }

            var valRes = ApiMapper.ValidateFeatureInputs(request.Features, _featureService, out var normalizedFeatures);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var updateRes = _offerService.UpdateOffer(
                id,
                request.AttractionId,
                request.Name,
                request.Description,
                request.Price,
                parsedDate,
                request.Status,
                normalizedFeatures,
                request.MinParticipants,
                request.MaxParticipants,
                updateMaxParticipants: true);

            if (!updateRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(updateRes.Message));
            }

            var updated = _offerService.GetOfferById(id)!;
            return Ok(BuildOfferDetailDto(updated, onlyAvailableRelated: false, activeProfile: null));
        }
    }

    [HttpPost("{id:int}/withdraw")]
    public ActionResult Withdraw(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_offerService.GetOfferById(id) is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            var res = _offerService.WithdrawOffer(id);
            return res.IsSuccess
                ? Ok(new { message = res.Message })
                : BadRequest(new ApiErrorResponse(res.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_offerService.GetOfferById(id) is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            var res = _offerService.DeleteOffer(id);
            return res.IsSuccess
                ? Ok(new { message = res.Message })
                : BadRequest(new ApiErrorResponse(res.Message));
        }
    }

    [HttpPost("{id:int}/validate")]
    public ActionResult<CompatibilityReportDto> ValidateAgainstProfile(int id, [FromBody] ParticipantProfileDto profileDto)
    {
        lock (InMemoryData.SyncRoot)
        {
            var offer = _offerService.GetOfferById(id);
            if (offer is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {id} nie istnieje."));
            }

            var attraction = _attractionService.GetAttractionById(offer.AttractionId);
            var profile = ApiMapper.ToDomain(profileDto);
            var report = offer.ValidateAgainst(profile, attraction, selectedAddons: null, _featureService.GetAllFeatures());

            return Ok(ApiMapper.ToDto(report));
        }
    }

    [HttpGet("{offerId:int}/addons")]
    public ActionResult<List<AddonDto>> GetAddons(int offerId)
    {
        lock (InMemoryData.SyncRoot)
        {
            var res = _addonService.GetAddonsForOffer(offerId);
            if (!res.IsSuccess || res.Data is null)
            {
                return NotFound(new ApiErrorResponse(res.Message));
            }

            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            return Ok(res.Data.Select(a => ApiMapper.ToDto(a, definitionsById)).ToList());
        }
    }

    [HttpPost("{offerId:int}/addons")]
    public ActionResult<AddonDto> AddAddon(int offerId, [FromBody] UpsertAddonRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var valRes = ApiMapper.ValidateFeatureInputs(request.FeatureModifiers, _featureService, out var normalizedMods);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var addRes = _addonService.AddAddon(offerId, request.Name, request.Description, request.Price, normalizedMods);
            if (!addRes.IsSuccess || addRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(addRes.Message));
            }

            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            return Ok(ApiMapper.ToDto(addRes.Data, definitionsById));
        }
    }

    [HttpPut("{offerId:int}/addons/{addonId:int}")]
    public ActionResult<AddonDto> UpdateAddon(int offerId, int addonId, [FromBody] UpsertAddonRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var valRes = ApiMapper.ValidateFeatureInputs(request.FeatureModifiers, _featureService, out var normalizedMods);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var updateRes = _addonService.UpdateAddon(
                offerId,
                addonId,
                request.Name,
                request.Description,
                request.Price,
                normalizedMods);

            if (!updateRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(updateRes.Message));
            }

            var offer = _offerService.GetOfferById(offerId)!;
            var updatedAddon = offer.Addons.First(a => a.Id == addonId);
            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);

            return Ok(ApiMapper.ToDto(updatedAddon, definitionsById));
        }
    }

    [HttpDelete("{offerId:int}/addons/{addonId:int}")]
    public ActionResult RemoveAddon(int offerId, int addonId)
    {
        lock (InMemoryData.SyncRoot)
        {
            var res = _addonService.RemoveAddon(offerId, addonId);
            return res.IsSuccess
                ? Ok(new { message = res.Message })
                : BadRequest(new ApiErrorResponse(res.Message));
        }
    }

    [HttpGet("{offerId:int}/relations")]
    public ActionResult<List<OfferSummaryDto>> GetRelations(int offerId, [FromQuery] bool onlyAvailable = false)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_offerService.GetOfferById(offerId) is null)
            {
                return NotFound(new ApiErrorResponse($"Oferta o ID {offerId} nie istnieje."));
            }

            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            var related = _offerService.GetRelatedOffers(offerId, onlyAvailable)
                .Select(o => ApiMapper.ToSummaryDto(
                    o,
                    _attractionService.GetAttractionById(o.AttractionId),
                    _orderService,
                    definitionsById))
                .ToList();

            return Ok(related);
        }
    }

    [HttpPost("relations")]
    public ActionResult LinkOffers([FromBody] LinkOffersRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var res = _offerService.LinkOffers(request.FirstOfferId, request.SecondOfferId);
            return res.IsSuccess
                ? Ok(new { message = res.Message })
                : BadRequest(new ApiErrorResponse(res.Message));
        }
    }

    [HttpDelete("{firstOfferId:int}/relations/{secondOfferId:int}")]
    public ActionResult UnlinkOffers(int firstOfferId, int secondOfferId)
    {
        lock (InMemoryData.SyncRoot)
        {
            var res = _offerService.UnlinkOffers(firstOfferId, secondOfferId);
            return res.IsSuccess
                ? Ok(new { message = res.Message })
                : BadRequest(new ApiErrorResponse(res.Message));
        }
    }

    private List<Offer> ResolveFilteredOffers(string? filter, int? attractionId, int? groupSize)
    {
        string normalizedFilter = DomainValueHelper.CleanText(filter).ToLowerInvariant();

        List<Offer> list = normalizedFilter switch
        {
            "all" => _offerService.GetAllOffers(),
            "planned" => _offerService.GetPlannedOffers(),
            "today" => _offerService.GetTodayOffers(),
            "withdrawn" => _offerService.GetWithdrawnOffers(),
            "group" => _offerService.GetGroupOnlyOffers(onlyAvailableForUser: true, groupSizeFilter: groupSize),
            "group4" => _offerService
                .GetGroupOnlyOffers(onlyAvailableForUser: true)
                .Where(o => o.MinParticipants == 4 && o.IsExactGroupSizeOffer(_attractionService.GetAttractionById(o.AttractionId)))
                .ToList(),
            _ => _offerService.GetAvailableOffersForUser()
        };

        if (attractionId.HasValue)
        {
            list = list.Where(o => o.AttractionId == attractionId.Value).ToList();
        }

        if (groupSize.HasValue && normalizedFilter != "group")
        {
            int size = groupSize.Value;
            list = list.Where(o =>
            {
                var attr = _attractionService.GetAttractionById(o.AttractionId);
                return size >= o.MinParticipants && size <= o.GetEffectiveMaxParticipants(attr);
            }).ToList();
        }

        return list;
    }

    private OfferDetailDto BuildOfferDetailDto(Offer offer, bool onlyAvailableRelated, ParticipantProfile? activeProfile)
    {
        var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
        var attraction = _attractionService.GetAttractionById(offer.AttractionId);
        var summaryDto = ApiMapper.ToSummaryDto(offer, attraction, _orderService, definitionsById, activeProfile);

        var attractionDto = attraction is not null
            ? ApiMapper.ToSummaryDto(attraction, _attractionService, definitionsById)
            : null;

        var directFeatures = offer.Features
            .OrderBy(f => f.FeatureId)
            .Select(f => ApiMapper.ToAssignedView(f, definitionsById))
            .ToList();

        var addons = offer.Addons
            .OrderBy(a => a.Id)
            .Select(a => ApiMapper.ToDto(a, definitionsById))
            .ToList();

        var related = _offerService.GetRelatedOffers(offer.Id, onlyAvailableRelated)
            .Select(r => ApiMapper.ToSummaryDto(
                r,
                _attractionService.GetAttractionById(r.AttractionId),
                _orderService,
                definitionsById,
                activeProfile))
            .ToList();

        return new OfferDetailDto(summaryDto, attractionDto, directFeatures, addons, related);
    }

    private static bool TryParseIsoDate(string? input, out DateTime parsed)
    {
        parsed = default;
        string cleaned = DomainValueHelper.CleanText(input);
        if (cleaned.Length == 0)
        {
            return false;
        }

        if (DateTime.TryParseExact(
                cleaned,
                new[] { "yyyy-MM-dd", "dd.MM.yyyy", "dd-MM-yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
        {
            parsed = parsed.Date;
            return true;
        }

        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            parsed = parsed.Date;
            return true;
        }

        return false;
    }
}
