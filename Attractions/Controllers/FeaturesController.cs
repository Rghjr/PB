using Attractions.Data;
using Attractions.DTOs.Api;
using Attractions.Helpers;
using Attractions.Models;
using Attractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Attractions.Controllers;

[ApiController]
[Route("api/features")]
public class FeaturesController : ControllerBase
{
    private readonly FeatureService _featureService;

    public FeaturesController(FeatureService featureService)
    {
        _featureService = featureService;
    }

    [HttpGet]
    public ActionResult<List<FeatureDefinitionDto>> GetAllFeatures()
    {
        lock (InMemoryData.SyncRoot)
        {
            var features = _featureService.GetAllFeatures()
                .Select(f => ApiMapper.ToDto(f, _featureService.GetFeatureUsage(f.Id).TotalCount))
                .ToList();

            return Ok(features);
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<FeatureUsageDto> GetFeatureUsage(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            var feature = _featureService.GetFeatureById(id);
            if (feature is null)
            {
                return NotFound(new ApiErrorResponse($"Cecha o ID {id} nie istnieje."));
            }

            var usage = _featureService.GetFeatureUsage(id);
            return Ok(ApiMapper.ToFeatureUsageDto(feature, usage));
        }
    }

    [HttpPost]
    public ActionResult<FeatureDefinitionDto> CreateFeature([FromBody] UpsertFeatureRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var resolvedRule = request.ComparisonRule ?? ResolveDefaultRuleForRequest(request);
            var result = _featureService.AddFeature(
                request.Name,
                request.Type,
                request.Description,
                resolvedRule,
                request.Unit,
                request.AllowedValues);

            if (!result.IsSuccess || result.Data is null)
            {
                return BadRequest(new ApiErrorResponse(result.Message));
            }

            var dto = ApiMapper.ToDto(result.Data, 0);
            return CreatedAtAction(nameof(GetFeatureUsage), new { id = dto.Id }, dto);
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<FeatureDefinitionDto> UpdateFeature(int id, [FromBody] UpsertFeatureRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var existing = _featureService.GetFeatureById(id);
            if (existing is null)
            {
                return NotFound(new ApiErrorResponse($"Cecha o ID {id} nie istnieje."));
            }

            var resolvedRule = request.ComparisonRule ?? existing.EffectiveRule;
            var result = _featureService.UpdateFeature(
                id,
                request.Name,
                request.Description,
                resolvedRule,
                request.Unit,
                request.AllowedValues);

            if (!result.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(result.Message));
            }

            var updated = _featureService.GetFeatureById(id)!;
            int usageCount = _featureService.GetFeatureUsage(id).TotalCount;
            return Ok(ApiMapper.ToDto(updated, usageCount));
        }
    }

    [HttpDelete("{id:int}")]
    public ActionResult DeleteFeature(int id, [FromQuery] bool force = false)
    {
        lock (InMemoryData.SyncRoot)
        {
            var existing = _featureService.GetFeatureById(id);
            if (existing is null)
            {
                return NotFound(new ApiErrorResponse($"Cecha o ID {id} nie istnieje."));
            }

            var usage = _featureService.GetFeatureUsage(id);
            if (usage.IsUsed && !force)
            {
                return Conflict(new ApiErrorResponse(
                    $"Cecha \"{existing.Name}\" jest aktualnie przypisana do {usage.TotalCount} obiektów.",
                    Details: ApiMapper.ToFeatureUsageDto(existing, usage)));
            }

            var deleteResult = _featureService.DeleteFeature(id, removeFromAssignedObjects: force);
            if (!deleteResult.IsSuccess)
            {
                return BadRequest(new ApiErrorResponse(deleteResult.Message));
            }

            return Ok(new { message = deleteResult.Message });
        }
    }

    private static FeatureComparisonRule ResolveDefaultRuleForRequest(UpsertFeatureRequest req)
    {
        var probe = new FeatureDefinition(
            id: 0,
            name: DomainValueHelper.CleanText(req.Name),
            type: req.Type,
            allowedValues: req.AllowedValues);

        return probe.ResolveDefaultRule();
    }
}
