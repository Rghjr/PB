using Attractions.Models;

namespace Attractions.DTOs.Api;

public record ApiErrorResponse(
    string Message,
    IReadOnlyList<string>? Reasons = null,
    object? Details = null);

public record FeatureDefinitionDto(
    int Id,
    string Name,
    FeatureValueType Type,
    string Description,
    FeatureComparisonRule ComparisonRule,
    string? Unit,
    List<string>? AllowedValues,
    string? ProfileBindingKey,
    int UsageCount);

public record FeatureUsageDto(
    int FeatureId,
    string FeatureName,
    int TotalCount,
    List<NamedEntityRefDto> Attractions,
    List<NamedEntityRefDto> Offers,
    List<NamedEntityRefDto> Addons);

public record NamedEntityRefDto(int Id, string Name);

public class UpsertFeatureRequest
{
    public string Name { get; set; } = string.Empty;
    public FeatureValueType Type { get; set; } = FeatureValueType.Boolean;
    public string Description { get; set; } = string.Empty;
    public FeatureComparisonRule? ComparisonRule { get; set; }
    public string? Unit { get; set; }
    public List<string>? AllowedValues { get; set; }
}

public class FeatureAssignmentInputDto
{
    public int FeatureId { get; set; }
    public string Value { get; set; } = string.Empty;
    public AddonEffectMode EffectMode { get; set; } = AddonEffectMode.TightenRequirement;
}

public record AssignedFeatureViewDto(
    int FeatureId,
    string FeatureName,
    FeatureValueType Type,
    string RawValue,
    string FormattedValue,
    AddonEffectMode EffectMode);

public record EffectiveFeatureViewDto(
    int FeatureId,
    string FeatureName,
    FeatureValueType Type,
    string RawValue,
    string FormattedValue,
    string SourceElement,
    bool IsDirectOfferFeature,
    bool OverridesAttractionDefault,
    bool ModifiedByAddon);

public record AttractionSummaryDto(
    int Id,
    string Name,
    string Description,
    string Location,
    int MaxParticipants,
    int OffersCount,
    int AvailableOffersCount,
    List<AssignedFeatureViewDto> Features);

public record AttractionDetailDto(
    int Id,
    string Name,
    string Description,
    string Location,
    int MaxParticipants,
    List<AssignedFeatureViewDto> Features,
    List<OfferSummaryDto> Offers);

public class UpsertAttractionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int MaxParticipants { get; set; } = 30;
    public List<FeatureAssignmentInputDto>? Features { get; set; }
}

public record AddonDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    List<AssignedFeatureViewDto> FeatureModifiers);

public class UpsertAddonRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<FeatureAssignmentInputDto>? FeatureModifiers { get; set; }
}

public record OfferSummaryDto(
    int Id,
    int AttractionId,
    string AttractionName,
    string AttractionLocation,
    string Name,
    string Description,
    decimal Price,
    string Date,
    OfferStatus Status,
    string StatusLabel,
    bool IsToday,
    bool IsFuturePlanned,
    bool IsAvailableForUser,
    bool IsGroupOffer,
    bool IsExactGroupSizeOffer,
    int MinParticipants,
    int? ExplicitMaxParticipants,
    int EffectiveMaxParticipants,
    string GroupSizeLabel,
    int BookedSpots,
    int RemainingSpots,
    int AddonsCount,
    int RelatedOffersCount,
    List<EffectiveFeatureViewDto> EffectiveFeatures,
    CompatibilityReportDto? Compatibility);

public record OfferDetailDto(
    OfferSummaryDto Offer,
    AttractionSummaryDto? Attraction,
    List<AssignedFeatureViewDto> DirectFeatures,
    List<AddonDto> Addons,
    List<OfferSummaryDto> RelatedOffers);

public class UpsertOfferRequest
{
    public int AttractionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Date { get; set; } = string.Empty;
    public OfferStatus Status { get; set; } = OfferStatus.Planned;
    public int MinParticipants { get; set; } = 1;
    public int? MaxParticipants { get; set; }
    public List<FeatureAssignmentInputDto>? Features { get; set; }
}

public class LinkOffersRequest
{
    public int FirstOfferId { get; set; }
    public int SecondOfferId { get; set; }
}

public class ParticipantProfileDto
{
    public List<int> ParticipantAges { get; set; } = new();
    public decimal? MinParticipantHeightCm { get; set; }
    public bool RequiresWheelchairAccess { get; set; }
    public int DisabledParticipantsCount { get; set; }
    public string? MaxPhysicalDemand { get; set; }
    public bool? ExplicitAdultGuardianPresent { get; set; }
    public bool RequiresChildFriendly { get; set; }
    public bool RequiresStrollerAccess { get; set; }
    public bool RequiresElevator { get; set; }
    public List<int> RequiredFacilityFeatureIds { get; set; } = new();
    public List<int> ConfirmedConditionFeatureIds { get; set; } = new();
    public Dictionary<int, List<decimal>> CustomNumericValues { get; set; } = new();
    public Dictionary<int, string> CustomOptionOrTextValues { get; set; } = new();
}

public class OfferSearchRequest
{
    public string Filter { get; set; } = "available";
    public int? AttractionId { get; set; }
    public int? GroupSize { get; set; }
    public bool OnlyCompatible { get; set; }
    public ParticipantProfileDto? Profile { get; set; }
}

public record RequirementCheckDto(
    int FeatureId,
    string FeatureName,
    string Status,
    string RequiredValueText,
    string ParticipantValueText,
    string SourceElement,
    string Message,
    string FormattedMessage);

public record CompatibilityReportDto(
    string TargetName,
    string TargetTypeLabel,
    bool IsCompatible,
    List<RequirementCheckDto> MetRequirements,
    List<RequirementCheckDto> UnmetRequirements,
    List<RequirementCheckDto> Warnings,
    IReadOnlyList<string> IncompatibilityReasons);

public class ParticipantGroupBreakdownDto
{
    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }
    public int YouthOrStudentsCount { get; set; }
    public int SeniorsCount { get; set; }
    public int DisabledCount { get; set; }
}

public record PriceCategoryLineDto(
    TicketCategory Category,
    string CategoryName,
    int Count,
    decimal DiscountPercent,
    decimal BaseUnitPrice,
    decimal FinalUnitPrice,
    decimal LineSubtotal);

public record PackagePriceBreakdownDto(
    decimal BasePricePerPerson,
    int TotalParticipants,
    decimal RegularGrossTotal,
    decimal SubtotalAfterCategoryDiscounts,
    decimal CategoryDiscountsSavings,
    decimal ExtraGroupDiscountPercent,
    decimal ExtraGroupDiscountAmount,
    decimal FinalTotalPrice,
    decimal TotalSavings,
    List<PriceCategoryLineDto> CategoryLines);

public class SelectedRelatedOfferInputDto
{
    public int OfferId { get; set; }
    public List<int> SelectedAddonIds { get; set; } = new();
}

public class PackageConfigurationInputDto
{
    public int BaseOfferId { get; set; }
    public List<int> SelectedAddonIds { get; set; } = new();
    public List<SelectedRelatedOfferInputDto> RelatedOffers { get; set; } = new();
}

public class EvaluatePackageRequest
{
    public PackageConfigurationInputDto Configuration { get; set; } = new();
    public ParticipantProfileDto? Profile { get; set; }
    public ParticipantGroupBreakdownDto? GroupBreakdown { get; set; }
}

public record PackageEvaluationResponse(
    int BaseOfferId,
    string BaseOfferName,
    string AttractionName,
    string Date,
    decimal BasePrice,
    decimal BaseAddonsPrice,
    decimal RelatedOffersBasePrice,
    decimal RelatedOffersAddonsPrice,
    decimal TotalPricePerPerson,
    int MinParticipants,
    int MaxParticipants,
    int MinRemainingSpots,
    string LimitingSpotsSource,
    bool HasGroupBoundsConflict,
    string GroupSizeLabel,
    List<AddonDto> SelectedBaseAddons,
    List<SelectedRelatedOfferViewDto> SelectedRelatedOffers,
    List<EffectiveFeatureViewDto> PackageEffectiveFeatures,
    CompatibilityReportDto? Compatibility,
    ParticipantGroupBreakdownDto SuggestedGroupBreakdown,
    PackagePriceBreakdownDto PriceBreakdown);

public record SelectedRelatedOfferViewDto(
    OfferSummaryDto Offer,
    List<AddonDto> AvailableAddons,
    List<AddonDto> SelectedAddons,
    decimal BasePrice,
    decimal AddonsPrice,
    decimal Subtotal);

public class PreviewPackageChangeRequest
{
    public PackageConfigurationInputDto Configuration { get; set; } = new();
    public ParticipantProfileDto Profile { get; set; } = new();
    public string ChangeType { get; set; } = string.Empty; // "baseAddon" | "relatedOffer" | "relatedAddon"
    public int TargetOfferId { get; set; }
    public int TargetAddonId { get; set; }
}

public record PreviewPackageChangeResponse(
    bool WouldBeCompatible,
    bool ResolvedPreviousIssues,
    List<RequirementCheckDto> NewIssues,
    CompatibilityReportDto SimulatedReport);

public class PlaceOrderApiRequest
{
    public PackageConfigurationInputDto Configuration { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Blik;
    public ParticipantGroupBreakdownDto GroupBreakdown { get; set; } = new();
    public ParticipantProfileDto? Profile { get; set; }
}

public record OrderDto(
    int Id,
    string OrderNumber,
    string CreatedAt,
    string CustomerName,
    string CustomerEmail,
    int TicketsCount,
    PaymentMethod PaymentMethod,
    string PaymentMethodLabel,
    decimal PricePerPerson,
    decimal RegularGrossPrice,
    decimal SubtotalAfterCategoryDiscounts,
    decimal CategoryDiscountsSavings,
    decimal ExtraGroupDiscountPercent,
    decimal ExtraGroupDiscountAmount,
    decimal TotalPrice,
    decimal TotalSavings,
    List<OrderItemDto> Items,
    List<PriceCategoryLineDto> ParticipantGroups);

public record OrderItemDto(
    int OfferId,
    string OfferName,
    string AttractionName,
    string Date,
    decimal BasePrice,
    bool IsMainOffer,
    decimal AddonsSubtotal,
    decimal ItemSubtotal,
    List<OrderAddonDto> SelectedAddons,
    List<string> EffectiveFeaturesSnapshot);

public record OrderAddonDto(int AddonId, string Name, decimal Price);
