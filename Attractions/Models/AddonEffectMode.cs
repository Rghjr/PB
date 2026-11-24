using System.Text.Json.Serialization;

namespace Attractions.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AddonEffectMode
{
    /// <summary>
    /// Dodatek zaostrza lub dodaje wymagania (np. wejście na wieżę bez windy, podwyższony wiek, wyższy wysiłek).
    /// </summary>
    TightenRequirement,

    /// <summary>
    /// Dodatek zapewnia udogodnienie lub łagodzi wymagania oferty bazowej (np. zjazd windą, podjazd meleksem).
    /// </summary>
    ProvideAccommodation
}
