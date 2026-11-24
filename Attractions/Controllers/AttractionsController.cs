using Attractions.Data;
using Attractions.DTOs.Api;
using Attractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Attractions.Controllers;

[ApiController]
[Route("api/attractions")]
public class AttractionsController : ControllerBase
{
    private readonly AttractionService _attractionService;
    private readonly OrderService _orderService;
    private readonly FeatureService _featureService;

    public AttractionsController(
        AttractionService attractionService,
        OrderService orderService,
        FeatureService featureService)
    {
        _attractionService = attractionService;
        _orderService = orderService;
        _featureService = featureService;
    }

    [HttpGet]
    public ActionResult<List<AttractionSummaryDto>> GetAll([FromQuery] bool onlyWithAvailableOffers = false)
    {
        lock (InMemoryData.SyncRoot)
        {
            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            var list = onlyWithAvailableOffers
                ? _orderService.GetAttractionsWithAvailableOffers()
                : _attractionService.GetAllAttractions();

            var dtos = list
                .Select(a => ApiMapper.ToSummaryDto(a, _attractionService, definitionsById))
                .ToList();

            return Ok(dtos);
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<AttractionDetailDto> GetById(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            var attraction = _attractionService.GetAttractionById(id);
            if (attraction is null)
            {
                return NotFound(new ApiErrorResponse($"Atrakcja o ID {id} nie istnieje."));
            }

            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            var offers = _attractionService.GetOffersForAttraction(id)
                .Select(o => ApiMapper.ToSummaryDto(o, attraction, _orderService, definitionsById))
                .ToList();

            var assignedFeatures = attraction.Features
                .OrderBy(f => f.FeatureId)
                .Select(f => ApiMapper.ToAssignedView(f, definitionsById))
                .ToList();

            return Ok(new AttractionDetailDto(
                attraction.Id,
                attraction.Name,
                attraction.Description,
                attraction.Location,
                attraction.MaxParticipants,
                assignedFeatures,
                offers));
        }
    }

    [HttpPost]
    public ActionResult<AttractionSummaryDto> Create([FromBody] UpsertAttractionRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var valRes = ApiMapper.ValidateFeatureInputs(request.Features, _featureService, out var normalizedFeatures);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var addRes = _attractionService.AddAttraction(
                request.Name,
                request.Description,
                request.Location,
                normalizedFeatures,
                request.MaxParticipants);

            if (!addRes.IsSuccess || addRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(addRes.Message));
            }

            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            var dto = ApiMapper.ToSummaryDto(addRes.Data, _attractionService, definitionsById);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<AttractionSummaryDto> Update(int id, [FromBody] UpsertAttractionRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_attractionService.GetAttractionById(id) is null)
            {
                return NotFound(new ApiErrorResponse($"Atrakcja o ID {id} nie istnieje."));
            }

            var valRes = ApiMapper.ValidateFeatureInputs(request.Features, _featureService, out var normalizedFeatures);
            if (!valRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(valRes.Message));
            }

            var updateRes = _attractionService.UpdateAttraction(
                id,
                request.Name,
                request.Description,
                request.Location,
                normalizedFeatures,
                request.MaxParticipants);

            if (!updateRes.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(updateRes.Message));
            }

            var updated = _attractionService.GetAttractionById(id)!;
            var definitionsById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
            return Ok(ApiMapper.ToSummaryDto(updated, _attractionService, definitionsById));
        }
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            if (_attractionService.GetAttractionById(id) is null)
            {
                return NotFound(new ApiErrorResponse($"Atrakcja o ID {id} nie istnieje."));
            }

            var res = _attractionService.DeleteAttraction(id);
            if (!res.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(res.Message));
            }

            return Ok(new { message = res.Message });
        }
    }

    [HttpPost("{id:int}/validate")]
    public ActionResult<CompatibilityReportDto> ValidateAgainstProfile(int id, [FromBody] ParticipantProfileDto profileDto)
    {
        lock (InMemoryData.SyncRoot)
        {
            var attraction = _attractionService.GetAttractionById(id);
            if (attraction is null)
            {
                return NotFound(new ApiErrorResponse($"Atrakcja o ID {id} nie istnieje."));
            }

            var profile = ApiMapper.ToDomain(profileDto);
            var report = attraction.ValidateAgainst(profile, _featureService.GetAllFeatures());
            return Ok(ApiMapper.ToDto(report));
        }
    }
}
