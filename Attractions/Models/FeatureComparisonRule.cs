using System.Text.Json.Serialization;

namespace Attractions.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureComparisonRule
{
    /// <summary>
    /// Cecha typu Boolean określająca dostępność udogodnienia (np. wózek inwalidzki, winda).
    /// Wymagana tylko wtedy, gdy uczestnik zaznaczy potrzebę w profilu.
    /// W pakiecie udogodnienie jest dostępne, gdy wszystkie wybrane oferty je zapewniają.
    /// </summary>
    FacilityAvailable,

    /// <summary>
    /// Cecha typu Boolean określająca warunek stawiany uczestnikowi (np. wymagana obecność opiekuna).
    /// Jeśli oferta ma wartość true, uczestnik/grupa musi spełniać ten warunek.
    /// W pakiecie warunek obowiązuje, jeśli którakolwiek pozycja go wymaga.
    /// </summary>
    ConditionRequired,

    /// <summary>
    /// Cecha typu Number określająca próg minimalny (uczestnik >= wartość cechy, np. minimalny wiek, wzrost).
    /// W pakiecie obowiązuje najwyższy próg minimalny spośród wszystkich pozycji.
    /// </summary>
    MinThreshold,

    /// <summary>
    /// Cecha typu Number określająca próg maksymalny (uczestnik <= wartość cechy, np. maksymalny wiek, waga).
    /// W pakiecie obowiązuje najniższy próg maksymalny spośród wszystkich pozycji.
    /// </summary>
    MaxThreshold,

    /// <summary>
    /// Cecha typu OptionList o uporządkowanych poziomach intensywności/trudności (np. Niski, Średni, Wysoki).
    /// Poziom wymagany przez ofertę nie może przekraczać maksymalnego akceptowalnego poziomu uczestnika.
    /// W pakiecie obowiązuje najwyższy poziom spośród wszystkich pozycji.
    /// </summary>
    MaxLevelIndex,

    /// <summary>
    /// Cecha typu Text lub OptionList dopasowywana tekstowo (np. język oprowadzania).
    /// Weryfikowana wtedy, gdy uczestnik określi oczekiwaną wartość w profilu; w przeciwnym razie informacyjna.
    /// </summary>
    ExactOrContainsMatch
}
