using System.Text.Json.Serialization;

namespace Attractions.Models;

public class FeatureAssignment
{
    [JsonPropertyName("featureId")]
    public int FeatureId { get; }

    [JsonPropertyName("value")]
    public string Value { get; }

    [JsonPropertyName("effectMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AddonEffectMode EffectMode { get; }

    [JsonConstructor]
    public FeatureAssignment(
        int featureId,
        string value,
        AddonEffectMode effectMode = AddonEffectMode.TightenRequirement)
    {
        FeatureId = featureId;
        Value = value ?? string.Empty;
        EffectMode = effectMode;
    }

    public FeatureAssignment Clone()
    {
        return new FeatureAssignment(FeatureId, Value, EffectMode);
    }
}
