using System.Text.Json.Serialization;

namespace Attractions.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureValueType
{
    Boolean,
    Number,
    Text,
    OptionList
}
