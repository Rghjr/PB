using System.Globalization;
using Attractions.Data;
using Attractions.DTOs;
using Attractions.Helpers;
using Attractions.Models;

namespace Attractions.Menus;

public static class ConsoleHelper
{
    private static readonly string[] AcceptedDateFormats =
    {
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd-MM-yyyy",
        "yyyy/MM/dd"
    };

    public static void ClearScreen()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
        }
        catch (IOException)
        {
            // Ignore console clear errors in non-interactive hosts.
        }
    }

    public static void Pause()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Naciśnij dowolny klawisz, aby kontynuować...");
        Console.ResetColor();

        if (Console.IsInputRedirected)
        {
            if (Console.ReadLine() is null)
            {
                throw new OperationCanceledException("Strumień wejściowy został zamknięty.");
            }

            Console.WriteLine();
        }
        else
        {
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
    }

    public static string ReadLineSafe()
    {
        string? input = Console.ReadLine();
        if (input is null)
        {
            throw new OperationCanceledException("Strumień wejściowy został zamknięty.");
        }

        return input.Trim();
    }

    public static string ReadMenuChoice(string prompt = "Wybierz opcję: ")
    {
        Console.Write(prompt);
        return ReadLineSafe();
    }

    public static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = ReadLineSafe();

            if (int.TryParse(input, out int result))
            {
                return result;
            }

            PrintError("Wprowadź poprawną liczbę całkowitą (ID).");
        }
    }

    public static int ReadIntInRangeOrDefault(string prompt, int min, int max, int defaultValue)
    {
        while (true)
        {
            Console.Write($"{prompt} [domyślnie: {defaultValue}]: ");
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }

            if (int.TryParse(input, out int value) && value >= min && value <= max)
            {
                return value;
            }

            PrintError($"Wprowadź liczbę całkowitą z zakresu od {min} do {max} lub naciśnij Enter.");
        }
    }

    public static int ReadIntOrKeepCurrent(string prompt, int currentValue)
    {
        while (true)
        {
            Console.Write($"{prompt} [aktualnie: {currentValue}, Enter = bez zmian]: ");
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                return currentValue;
            }

            if (int.TryParse(input, out int result))
            {
                return result;
            }

            PrintError("Wprowadź poprawną liczbę całkowitą lub naciśnij Enter, aby pozostawić obecną wartość.");
        }
    }

    public static string ReadRequiredString(string prompt, string fieldLabel)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = ReadLineSafe();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            PrintError($"Pole \"{fieldLabel}\" nie może być puste.");
        }
    }

    public static string ReadOptionalString(string prompt)
    {
        Console.Write(prompt);
        return ReadLineSafe();
    }

    public static string ReadOptionalEmail(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (DomainValueHelper.IsValidEmail(input))
            {
                return input;
            }

            PrintError("Nieprawidłowy format adresu e-mail (np. jan.kowalski@domena.pl) lub naciśnij Enter, aby pominąć.");
        }
    }

    public static string ReadStringOrKeepCurrent(
        string prompt,
        string currentValue,
        bool required,
        string fieldLabel,
        bool allowClearWithDash = false)
    {
        string displayCurrent = string.IsNullOrWhiteSpace(currentValue) ? "(brak)" : currentValue;
        string clearHint = allowClearWithDash ? ", '-' = wyczyść" : string.Empty;

        while (true)
        {
            Console.Write($"{prompt} [aktualnie: {displayCurrent}, Enter = bez zmian{clearHint}]: ");
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                if (required && string.IsNullOrWhiteSpace(currentValue))
                {
                    PrintError($"Pole \"{fieldLabel}\" jest wymagane i nie może pozostać puste.");
                    continue;
                }

                return currentValue;
            }

            if (allowClearWithDash && input == "-" && !required)
            {
                return string.Empty;
            }

            return input;
        }
    }

    public static decimal ReadNonNegativeDecimal(string prompt, string fieldLabel = "Cena")
    {
        while (true)
        {
            Console.Write(prompt);
            string input = ReadLineSafe().Replace(',', '.');

            if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            {
                PrintError($"Nieprawidłowy format pola \"{fieldLabel}\". Wprowadź liczbę, np. 49,99.");
                continue;
            }

            if (value < 0)
            {
                PrintError($"{fieldLabel} nie może być ujemna.");
                continue;
            }

            return decimal.Round(value, 2);
        }
    }

    public static decimal ReadNonNegativeDecimalOrKeepCurrent(string prompt, decimal currentValue, string fieldLabel = "Cena")
    {
        while (true)
        {
            Console.Write($"{prompt} [aktualnie: {currentValue:F2} zł, Enter = bez zmian]: ");
            string raw = ReadLineSafe();

            if (string.IsNullOrEmpty(raw))
            {
                return currentValue;
            }

            string normalized = raw.Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            {
                PrintError($"Nieprawidłowy format pola \"{fieldLabel}\". Wprowadź liczbę, np. 49,99.");
                continue;
            }

            if (value < 0)
            {
                PrintError($"{fieldLabel} nie może być ujemna.");
                continue;
            }

            return decimal.Round(value, 2);
        }
    }

    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = ReadLineSafe();

            if (TryParseDate(input, out DateTime parsedDate))
            {
                return parsedDate.Date;
            }

            PrintError("Nieprawidłowy format daty. Użyj formatu RRRR-MM-DD (np. 2026-11-25) lub DD.MM.RRRR.");
        }
    }

    public static DateTime ReadDateOrKeepCurrent(string prompt, DateTime currentValue)
    {
        while (true)
        {
            Console.Write($"{prompt} [aktualnie: {currentValue:yyyy-MM-dd}, Enter = bez zmian]: ");
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                return currentValue.Date;
            }

            if (TryParseDate(input, out DateTime parsedDate))
            {
                return parsedDate.Date;
            }

            PrintError("Nieprawidłowy format daty. Użyj formatu RRRR-MM-DD (np. 2026-11-25) lub naciśnij Enter.");
        }
    }

    public static OfferStatus ReadOfferStatusOrKeepCurrent(OfferStatus currentStatus)
    {
        string currentLabel = currentStatus == OfferStatus.Planned ? "Planned (Zaplanowana)" : "Withdrawn (Wycofana)";

        while (true)
        {
            Console.WriteLine($"Status oferty [aktualnie: {currentLabel}]:");
            Console.WriteLine("  1. Planned (Zaplanowana)");
            Console.WriteLine("  2. Withdrawn (Wycofana – spowoduje usunięcie aktywnych powiązań)");
            Console.Write("Wybierz status [Enter = bez zmian]: ");
            string input = ReadLineSafe();

            if (string.IsNullOrEmpty(input))
            {
                return currentStatus;
            }

            if (input == "1")
            {
                return OfferStatus.Planned;
            }

            if (input == "2")
            {
                return OfferStatus.Withdrawn;
            }

            PrintError("Wybierz 1, 2 lub naciśnij Enter, aby pozostawić aktualny status.");
        }
    }

    public static PaymentMethod ReadPaymentMethod()
    {
        Console.WriteLine("Wybierz sposób płatności:");
        Console.WriteLine("  1. BLIK [Domyślnie - Enter]");
        Console.WriteLine("  2. Karta płatnicza");
        Console.WriteLine("  3. Szybki przelew online");
        Console.WriteLine("  4. Płatność w kasie biletowej na miejscu");

        while (true)
        {
            string input = ReadMenuChoice("Sposób płatności (1-4): ");
            if (string.IsNullOrEmpty(input) || input == "1")
            {
                return PaymentMethod.Blik;
            }

            if (input == "2")
            {
                return PaymentMethod.Card;
            }

            if (input == "3")
            {
                return PaymentMethod.OnlineTransfer;
            }

            if (input == "4")
            {
                return PaymentMethod.CashAtTicketOffice;
            }

            PrintError("Wybierz 1, 2, 3 lub 4 (lub naciśnij Enter dla BLIK).");
        }
    }

    public static string FormatPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Blik => "BLIK",
            PaymentMethod.Card => "Karta płatnicza",
            PaymentMethod.OnlineTransfer => "Szybki przelew online",
            PaymentMethod.CashAtTicketOffice => "Płatność w kasie na miejscu",
            _ => method.ToString()
        };
    }

    public static bool ReadConfirmation(string prompt, bool defaultYes = false)
    {
        string hint = defaultYes ? "[T/n]" : "[t/N]";
        while (true)
        {
            Console.Write($"{prompt} {hint}: ");
            string input = ReadLineSafe().ToUpperInvariant();

            if (string.IsNullOrEmpty(input))
            {
                return defaultYes;
            }

            if (input is "T" or "TAK" or "Y" or "YES")
            {
                return true;
            }

            if (input is "N" or "NIE" or "NO")
            {
                return false;
            }

            PrintError("Wpisz T (Tak) lub N (Nie).");
        }
    }

    public static void PrintSectionHeader(string title)
    {
        Console.WriteLine("====================================");
        Console.WriteLine($"   {title}");
        Console.WriteLine("====================================");
    }

    public static void PrintResult(ServiceResult result)
    {
        if (result.IsSuccess)
        {
            PrintSuccess(result.Message);
        }
        else
        {
            PrintError(result.Message);
        }
    }

    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUKCES] {message}");
        Console.ResetColor();
    }

    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[BŁĄD] {message}");
        Console.ResetColor();
    }

    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    public static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[UWAGA] {message}");
        Console.ResetColor();
    }

    public static string FormatFeatureType(FeatureValueType type)
    {
        return type switch
        {
            FeatureValueType.Boolean => "Boolean (Tak/Nie)",
            FeatureValueType.Number => "Number (Liczba)",
            FeatureValueType.Text => "Text (Tekst)",
            FeatureValueType.OptionList => "OptionList (Lista wartości)",
            _ => type.ToString()
        };
    }

    public static string FormatComparisonRule(FeatureComparisonRule rule)
    {
        return rule switch
        {
            FeatureComparisonRule.FacilityAvailable => "Udogodnienie (dostępne = Tak)",
            FeatureComparisonRule.ConditionRequired => "Warunek wymagany od uczestnika",
            FeatureComparisonRule.MinThreshold => "Próg minimalny (uczestnik >= wartość)",
            FeatureComparisonRule.MaxThreshold => "Próg maksymalny (uczestnik <= wartość)",
            FeatureComparisonRule.MaxLevelIndex => "Maksymalny poziom trudności/intensywności",
            FeatureComparisonRule.ExactOrContainsMatch => "Dopasowanie tekstu / opcji",
            _ => rule.ToString()
        };
    }

    public static void DisplayFeaturesDictionary(
        IReadOnlyCollection<FeatureDefinition> features,
        Func<int, int>? usageCountResolver = null)
    {
        if (features.Count == 0)
        {
            PrintInfo("Słownik cech (Features.json) jest pusty.");
            return;
        }

        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
        Console.WriteLine($"{"ID",-4} | {"Nazwa cechy",-36} | {"Typ",-26} | {"Reguła weryfikacji",-36} | {"Użycia",-6}");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");

        foreach (var f in features.OrderBy(x => x.Id))
        {
            string usageText = usageCountResolver is not null ? usageCountResolver(f.Id).ToString() : "-";
            string typeWithMeta = f.Type switch
            {
                FeatureValueType.Number when !string.IsNullOrWhiteSpace(f.Unit) => $"Number ({f.Unit})",
                FeatureValueType.OptionList when f.AllowedValues is { Count: > 0 } => $"OptionList ({string.Join("/", f.AllowedValues)})",
                _ => f.Type.ToString()
            };

            Console.WriteLine(
                $"{f.Id,-4} | {Truncate(f.Name, 36),-36} | {Truncate(typeWithMeta, 26),-26} | {Truncate(FormatComparisonRule(f.EffectiveRule), 36),-36} | {usageText,-6}");

            if (!string.IsNullOrWhiteSpace(f.Description))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"     Opis: {f.Description}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
    }

    public static void DisplayCompatibilityReport(CompatibilityReport report, bool includeMetRequirements = true)
    {
        Console.WriteLine("------------------------------------------------------------");
        if (report.IsCompatible)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"MOŻNA SKORZYSTAĆ Z {report.TargetTypeLabel}: {report.TargetName}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"NIE MOŻNA SKORZYSTAĆ Z {report.TargetTypeLabel}: {report.TargetName}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Powody:");
            foreach (var unmet in report.UnmetRequirements)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  - {unmet.FormattedMessage}");
                Console.ResetColor();
            }
        }

        if (report.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Wymagania do dodatkowej weryfikacji (brak danych w profilu):");
            foreach (var warning in report.Warnings)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ! {warning.FormattedMessage}");
                Console.ResetColor();
            }
        }

        if (includeMetRequirements && report.MetRequirements.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Spełnione wymagania:");
            foreach (var met in report.MetRequirements)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"  + {met.FormattedMessage}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("------------------------------------------------------------");
    }

    public static List<FeatureAssignment> EditFeatureAssignmentsInteractive(
        string entityHeader,
        IReadOnlyCollection<FeatureDefinition> availableDefinitions,
        IEnumerable<FeatureAssignment> currentAssignments,
        Attraction? inheritedFromAttraction = null,
        bool isAddonEditor = false)
    {
        var workingList = currentAssignments.Select(a => a.Clone()).ToList();
        var defById = availableDefinitions.ToDictionary(d => d.Id);
        string? lastFeedback = null;
        bool lastSuccess = true;

        while (true)
        {
            ClearScreen();
            PrintSectionHeader(entityHeader);

            if (availableDefinitions.Count == 0)
            {
                PrintInfo("Brak zdefiniowanych cech w słowniku Features.json.");
                Pause();
                return workingList;
            }

            if (inheritedFromAttraction is not null)
            {
                Console.WriteLine($"Atrakcja nadrzędna: {inheritedFromAttraction.Name} (cechy oferty nadpisują lub rozszerzają cechy atrakcji)");
                Console.WriteLine();
            }

            Console.WriteLine("Dostępne cechy ze słownika Features.json:");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------------");
            Console.WriteLine($"{"ID",-4} | {"Nazwa cechy",-36} | {"Typ",-14} | {"Aktualne ustawienie",-46}");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------------");

            foreach (var def in availableDefinitions.OrderBy(d => d.Id))
            {
                var own = workingList.FirstOrDefault(x => x.FeatureId == def.Id);
                string? inheritedRaw = inheritedFromAttraction?.GetFeatureValue(def.Id);

                string stateLabel;
                if (own is not null)
                {
                    if (isAddonEditor)
                    {
                        string modeBadge = own.EffectMode == AddonEffectMode.ProvideAccommodation
                            ? "Udogodnienie"
                            : "Wymaganie";
                        stateLabel = $"[USTAWIONO: {def.FormatValue(own.Value)} | {modeBadge}]";
                    }
                    else if (inheritedRaw is not null)
                    {
                        stateLabel = $"[NADPISANO: {def.FormatValue(own.Value)}] (w atrakcji: {def.FormatValue(inheritedRaw)})";
                    }
                    else
                    {
                        stateLabel = $"[USTAWIONO: {def.FormatValue(own.Value)}]";
                    }
                }
                else if (inheritedRaw is not null)
                {
                    stateLabel = $"(dziedziczone z atrakcji: {def.FormatValue(inheritedRaw)})";
                }
                else
                {
                    stateLabel = "(nie przypisano)";
                }

                Console.WriteLine($"{def.Id,-4} | {Truncate(def.Name, 36),-36} | {def.Type,-14} | {Truncate(stateLabel, 46),-46}");
            }

            Console.WriteLine("----------------------------------------------------------------------------------------------------------------");

            if (!string.IsNullOrEmpty(lastFeedback))
            {
                if (lastSuccess)
                {
                    PrintSuccess(lastFeedback);
                }
                else
                {
                    PrintError(lastFeedback);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Instrukcja: podaj ID cechy, aby przypisać/zmienić wartość, '-ID' (np. -2) aby usunąć własne przypisanie, '0' = zatwierdź i wróć.");
            string rawChoice = ReadMenuChoice("Wybór: ");

            if (rawChoice == "0" || string.IsNullOrEmpty(rawChoice))
            {
                return workingList;
            }

            if (rawChoice.StartsWith('-') && int.TryParse(rawChoice[1..], out int removeId))
            {
                int removed = workingList.RemoveAll(x => x.FeatureId == removeId);
                if (removed > 0)
                {
                    lastFeedback = $"Usunięto własne przypisanie dla cechy ID {removeId}.";
                    lastSuccess = true;
                }
                else
                {
                    lastFeedback = $"Cecha ID {removeId} nie posiada własnego przypisania na tym obiekcie.";
                    lastSuccess = false;
                }

                continue;
            }

            if (!int.TryParse(rawChoice, out int selectedFeatureId) || !defById.TryGetValue(selectedFeatureId, out var selectedDef))
            {
                lastFeedback = "Podaj poprawne ID cechy ze słownika, '-ID' do usunięcia lub '0'.";
                lastSuccess = false;
                continue;
            }

            string? normalizedVal = PromptFeatureValueForDefinition(selectedDef);
            if (normalizedVal is null)
            {
                lastFeedback = "Anulowano zmianę wartości cechy.";
                lastSuccess = true;
                continue;
            }

            AddonEffectMode effectMode = AddonEffectMode.TightenRequirement;
            if (isAddonEditor)
            {
                Console.WriteLine("Określ sposób oddziaływania tej cechy dodatku na ofertę:");
                Console.WriteLine("  1. Zaostrza lub dodaje wymaganie (np. schody na wieżę, wyższy wiek) [Domyślnie]");
                Console.WriteLine("  2. Zapewnia udogodnienie / łagodzi barierę oferty (np. winda, meleks)");
                string modeInput = ReadMenuChoice("Wybierz tryb (1-2): ");
                if (modeInput == "2")
                {
                    effectMode = AddonEffectMode.ProvideAccommodation;
                }
            }

            workingList.RemoveAll(x => x.FeatureId == selectedDef.Id);
            workingList.Add(new FeatureAssignment(selectedDef.Id, normalizedVal, effectMode));

            lastFeedback = $"Przypisano cechę \"{selectedDef.Name}\" = {selectedDef.FormatValue(normalizedVal)}.";
            lastSuccess = true;
        }
    }

    public static string? PromptFeatureValueForDefinition(FeatureDefinition definition)
    {
        Console.WriteLine();
        Console.WriteLine($"Przypisywanie wartości dla cechy: \"{definition.Name}\" ({FormatFeatureType(definition.Type)})");
        if (!string.IsNullOrWhiteSpace(definition.Description))
        {
            Console.WriteLine($"Opis: {definition.Description}");
        }

        switch (definition.Type)
        {
            case FeatureValueType.Boolean:
                Console.WriteLine("  1. Tak (true)");
                Console.WriteLine("  2. Nie (false)");
                Console.WriteLine("  0. Anuluj");
                while (true)
                {
                    string input = ReadMenuChoice("Wybierz wartość (1/2 lub Tak/Nie): ");
                    if (input == "0")
                    {
                        return null;
                    }

                    if (input == "1")
                    {
                        return "true";
                    }

                    if (input == "2")
                    {
                        return "false";
                    }

                    if (definition.TryNormalizeValue(input, out string norm, out string err))
                    {
                        return norm;
                    }

                    PrintError(err);
                }

            case FeatureValueType.OptionList when definition.AllowedValues is { Count: > 0 }:
                for (int i = 0; i < definition.AllowedValues.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {definition.AllowedValues[i]}");
                }
                Console.WriteLine("  0. Anuluj");

                while (true)
                {
                    string input = ReadMenuChoice($"Wybierz opcję (1-{definition.AllowedValues.Count} lub nazwę): ");
                    if (input == "0")
                    {
                        return null;
                    }

                    if (definition.TryNormalizeValue(input, out string norm, out string err))
                    {
                        return norm;
                    }

                    PrintError(err);
                }

            case FeatureValueType.Number:
                string unitHint = string.IsNullOrWhiteSpace(definition.Unit) ? string.Empty : $" ({definition.Unit})";
                while (true)
                {
                    string input = ReadMenuChoice($"Podaj wartość liczbową{unitHint} (lub '-' = anuluj): ");
                    if (input == "-")
                    {
                        return null;
                    }

                    if (definition.TryNormalizeValue(input, out string norm, out string err))
                    {
                        return norm;
                    }

                    PrintError(err);
                }

            case FeatureValueType.Text:
            default:
                string textInput = ReadMenuChoice("Podaj wartość tekstową (Enter = anuluj): ");
                if (string.IsNullOrWhiteSpace(textInput))
                {
                    return null;
                }

                return textInput.Trim();
        }
    }

    public static string FormatOfferStatus(Offer offer)
    {
        if (offer.Status == OfferStatus.Withdrawn)
        {
            return "Wycofana";
        }

        if (offer.Date.Date == DateTime.Today)
        {
            return "Zaplanowana (DZISIAJ)";
        }

        if (offer.Date.Date > DateTime.Today)
        {
            return "Zaplanowana";
        }

        return "Zaplanowana (termin minął)";
    }

    public static void DisplayAttractionsList(IReadOnlyCollection<Attraction> attractions, Func<int, int> offerCountResolver)
    {
        if (attractions.Count == 0)
        {
            PrintInfo("Brak zarejestrowanych atrakcji.");
            return;
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------");
        Console.WriteLine($"{"ID",-4} | {"Nazwa atrakcji",-34} | {"Lokalizacja",-24} | {"Maks. os.",-9} | {"Oferty",-6}");
        Console.WriteLine("--------------------------------------------------------------------------------------------");

        foreach (var attraction in attractions)
        {
            int offersCount = offerCountResolver(attraction.Id);
            Console.WriteLine(
                $"{attraction.Id,-4} | {Truncate(attraction.Name, 34),-34} | {Truncate(attraction.Location, 24),-24} | {$"{attraction.MaxParticipants} os.",-9} | {offersCount,-6}");
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------");
    }

    public static void DisplayAttractionDetails(
        Attraction attraction,
        IReadOnlyCollection<Offer> assignedOffers,
        ParticipantProfile? activeProfile = null)
    {
        var defById = InMemoryData.Features.ToDictionary(f => f.Id);

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"ID atrakcji : {attraction.Id}");
        Console.WriteLine($"Nazwa       : {attraction.Name}");
        Console.WriteLine($"Lokalizacja : {attraction.Location}");
        Console.WriteLine($"Maks. osób  : {attraction.MaxParticipants} os. jednocześnie");
        Console.WriteLine($"Opis        : {(string.IsNullOrWhiteSpace(attraction.Description) ? "(brak opisu)" : attraction.Description)}");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Cechy ogólne obiektu ({attraction.Features.Count}):");
        if (attraction.Features.Count == 0)
        {
            Console.WriteLine("  (Brak przypisanych cech ogólnych)");
        }
        else
        {
            foreach (var f in attraction.Features.OrderBy(x => x.FeatureId))
            {
                if (defById.TryGetValue(f.FeatureId, out var def))
                {
                    Console.WriteLine($"  * {def.Name}: {def.FormatValue(f.Value)}");
                }
            }
        }

        if (activeProfile is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Weryfikacja ogólnej dostępności obiektu względem Twojego profilu:");
            DisplayCompatibilityReport(attraction.ValidateAgainst(activeProfile), includeMetRequirements: false);
        }

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Przypisane oferty ({assignedOffers.Count}):");

        if (assignedOffers.Count == 0)
        {
            Console.WriteLine("  (Brak ofert przypisanych do tej atrakcji)");
            return;
        }

        foreach (var offer in assignedOffers)
        {
            string profileTag = string.Empty;
            if (activeProfile is not null)
            {
                bool compatible = offer.ValidateAgainst(activeProfile, attraction).IsCompatible;
                profileTag = compatible ? " | [ZGODNA Z PROFILEM]" : " | [NIEZGODNA Z PROFILEM]";
            }

            Console.WriteLine(
                $"  - [ID: {offer.Id}] {offer.Name} | Data: {offer.Date:yyyy-MM-dd} | Cena: {offer.Price:F2} zł | Limit: {offer.FormatGroupSizeLimit(attraction)} | Status: {FormatOfferStatus(offer)}{profileTag}");
        }
    }

    public static void DisplayOffersList(
        IReadOnlyCollection<Offer> offers,
        Func<int, Attraction?> attractionResolver,
        ParticipantProfile? activeProfile = null)
    {
        if (offers.Count == 0)
        {
            PrintInfo("Brak ofert spełniających kryteria.");
            return;
        }

        if (activeProfile is null)
        {
            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine(
                $"{"ID",-4} | {"Nazwa oferty",-30} | {"Atrakcja",-22} | {"Data",-10} | {"Cena",-11} | {"Grupa / Limit",-15} | {"Status",-22}");
            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");

            foreach (var offer in offers)
            {
                var attraction = attractionResolver(offer.AttractionId);
                string attractionName = attraction?.Name ?? "(nieznana)";
                string priceText = $"{offer.Price:F2} zł";
                string groupText = offer.FormatGroupSizeLimit(attraction);

                Console.WriteLine(
                    $"{offer.Id,-4} | {Truncate(offer.Name, 30),-30} | {Truncate(attractionName, 22),-22} | {offer.Date:yyyy-MM-dd} | {priceText,11} | {Truncate(groupText, 15),-15} | {Truncate(FormatOfferStatus(offer), 22),-22}");
            }

            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");
            return;
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------------------------------------");
        Console.WriteLine(
            $"{"ID",-4} | {"Nazwa oferty",-28} | {"Atrakcja",-20} | {"Data",-10} | {"Cena",-11} | {"Grupa / Limit",-15} | {"Status",-20} | {"Profil",-14}");
        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------------------------------------");

        foreach (var offer in offers)
        {
            var attraction = attractionResolver(offer.AttractionId);
            string attractionName = attraction?.Name ?? "(nieznana)";
            string priceText = $"{offer.Price:F2} zł";
            string groupText = offer.FormatGroupSizeLimit(attraction);
            bool isCompatible = offer.ValidateAgainst(activeProfile, attraction).IsCompatible;
            string compatLabel = isCompatible ? "ZGODNA" : "NIEZGODNA (!)";

            Console.Write(
                $"{offer.Id,-4} | {Truncate(offer.Name, 28),-28} | {Truncate(attractionName, 20),-20} | {offer.Date:yyyy-MM-dd} | {priceText,11} | {Truncate(groupText, 15),-15} | {Truncate(FormatOfferStatus(offer), 20),-20} | ");
            Console.ForegroundColor = isCompatible ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{compatLabel,-14}");
            Console.ResetColor();
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------------------------------------");
    }

    public static void DisplayOfferDetails(
        Offer offer,
        Attraction? attraction,
        IReadOnlyCollection<Offer> relatedOffers,
        ParticipantProfile? activeProfile = null)
    {
        var defById = InMemoryData.Features.ToDictionary(f => f.Id);
        var effectiveFeatures = offer.GetEffectiveFeatures(attraction, selectedAddons: null, InMemoryData.Features);

        Console.WriteLine("============================================================");
        Console.WriteLine($"SZCZEGÓŁY OFERTY [ID: {offer.Id}]");
        Console.WriteLine("============================================================");
        Console.WriteLine($"Nazwa          : {offer.Name}");
        Console.WriteLine($"Atrakcja       : {(attraction is not null ? $"{attraction.Name} (ID: {attraction.Id}, {attraction.Location})" : "(brak)")}");
        Console.WriteLine($"Opis           : {(string.IsNullOrWhiteSpace(offer.Description) ? "(brak opisu)" : offer.Description)}");
        Console.WriteLine($"Cena bazowa    : {offer.Price:F2} zł / os. (bilet normalny)");
        Console.WriteLine($"Zniżki grupowe : Dzieci -50%, Młodzież/Student -30%, Senior -25%, OzN -50% + rabat grupowy (4-7 os.: -5%, 8+ os.: -10%)");
        Console.WriteLine($"Limit grupy    : {offer.FormatGroupSizeLimit(attraction)}{(attraction is not null ? $" (maks. pojemność atrakcji: {attraction.MaxParticipants} os.)" : string.Empty)}");
        Console.WriteLine($"Data realizacji: {offer.Date:yyyy-MM-dd}");
        Console.WriteLine($"Status         : {FormatOfferStatus(offer)}");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Wymagania i cechy oferty ({effectiveFeatures.Count}):");

        if (effectiveFeatures.Count == 0)
        {
            Console.WriteLine("  (Brak zdefiniowanych cech dla tej oferty i atrakcji)");
        }
        else
        {
            foreach (var entry in effectiveFeatures.Values.OrderBy(e => e.FeatureId))
            {
                if (!defById.TryGetValue(entry.FeatureId, out var def))
                {
                    continue;
                }

                string originBadge = offer.GetDirectFeatureValue(entry.FeatureId) is not null
                    ? (entry.OverridesAttractionDefault ? "[cecha oferty – nadpisuje atrakcję]" : "[cecha oferty]")
                    : "[dziedziczone z atrakcji]";

                Console.WriteLine($"  * {def.Name}: {def.FormatValue(entry.Value)} {originBadge}");
            }
        }

        if (activeProfile is not null)
        {
            Console.WriteLine();
            DisplayCompatibilityReport(offer.ValidateAgainst(activeProfile, attraction));
        }

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Dodatki do oferty ({offer.Addons.Count}):");

        if (offer.Addons.Count == 0)
        {
            Console.WriteLine("  (Brak zdefiniowanych dodatków)");
        }
        else
        {
            foreach (var addon in offer.Addons.OrderBy(a => a.Id))
            {
                string desc = string.IsNullOrWhiteSpace(addon.Description) ? string.Empty : $" – {addon.Description}";
                Console.WriteLine($"  - [ID: {addon.Id}] {addon.Name} ({addon.Price:F2} zł){desc}");
                PrintAddonModifiersIfAny(addon, defById, "      Wpływ na cechy: ", "; ");
            }
        }

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Oferty powiązane ({relatedOffers.Count}):");

        if (relatedOffers.Count == 0)
        {
            Console.WriteLine("  (Brak powiązanych ofert)");
        }
        else
        {
            foreach (var related in relatedOffers)
            {
                string compatSuffix = string.Empty;
                if (activeProfile is not null)
                {
                    bool ok = related.ValidateAgainst(activeProfile).IsCompatible;
                    compatSuffix = ok ? " | [ZGODNA]" : " | [NIEZGODNA]";
                }

                Console.WriteLine(
                    $"  - [ID: {related.Id}] {related.Name} | Data: {related.Date:yyyy-MM-dd} | Cena: {related.Price:F2} zł | Status: {FormatOfferStatus(related)}{compatSuffix}");
            }
        }

        Console.WriteLine("============================================================");
    }

    public static void DisplayAddonsList(Offer offer, IReadOnlyCollection<Addon> addons)
    {
        var defById = InMemoryData.Features.ToDictionary(f => f.Id);

        Console.WriteLine($"Oferta: [ID: {offer.Id}] {offer.Name} (Cena bazowa: {offer.Price:F2} zł | Status: {FormatOfferStatus(offer)})");
        Console.WriteLine("--------------------------------------------------------------------------------");

        if (addons.Count == 0)
        {
            PrintInfo("Ta oferta nie posiada żadnych dodatków.");
            return;
        }

        Console.WriteLine($"{"ID",-4} | {"Nazwa dodatku",-34} | {"Cena",-12} | Opis");
        Console.WriteLine("--------------------------------------------------------------------------------");

        foreach (var addon in addons)
        {
            string desc = string.IsNullOrWhiteSpace(addon.Description) ? "(brak opisu)" : addon.Description;
            Console.WriteLine($"{addon.Id,-4} | {Truncate(addon.Name, 34),-34} | {$"{addon.Price:F2} zł",12} | {desc}");
            PrintAddonModifiersIfAny(addon, defById, "       Wpływ na wymagania pakietu: ", " | ");
        }

        Console.WriteLine("--------------------------------------------------------------------------------");
    }

    public static void PrintAddonModifiersIfAny(
        Addon addon,
        IReadOnlyDictionary<int, FeatureDefinition> definitionsById,
        string prefixLabel = "        Cechy dodatku: ",
        string separator = " | ")
    {
        if (addon.FeatureModifiers.Count == 0)
        {
            return;
        }

        var modTexts = addon.FeatureModifiers
            .Where(m => definitionsById.ContainsKey(m.FeatureId))
            .Select(m =>
            {
                var d = definitionsById[m.FeatureId];
                string modeLabel = m.EffectMode == AddonEffectMode.ProvideAccommodation ? "Udogodnienie" : "Wymóg";
                return $"{modeLabel}: {d.Name} = {d.FormatValue(m.Value)}";
            })
            .ToList();

        if (modTexts.Count == 0)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"{prefixLabel}{string.Join(separator, modTexts)}");
        Console.ResetColor();
    }

    public static void DisplayConfigurationSummary(
        OfferConfiguration config,
        Func<int, Attraction?> attractionResolver,
        ParticipantProfile? activeProfile = null)
    {
        string attractionName = attractionResolver(config.BaseOffer.AttractionId)?.Name ?? "(nieznana atrakcja)";
        var defById = InMemoryData.Features.ToDictionary(f => f.Id);

        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine("          PODSUMOWANIE WYBRANEJ KONFIGURACJI OFERTY");
        Console.WriteLine("============================================================");
        Console.WriteLine($"Oferta główna       : [ID: {config.BaseOffer.Id}] {config.BaseOffer.Name}");
        Console.WriteLine($"Atrakcja            : {attractionName}");
        Console.WriteLine($"Data realizacji     : {config.BaseOffer.Date:yyyy-MM-dd}");
        Console.WriteLine($"Cena podstawowa     : {config.BasePrice:F2} zł (pierwotna cena oferty pozostaje bez zmian)");
        Console.WriteLine("------------------------------------------------------------");

        Console.WriteLine($"Wybrane dodatki do oferty głównej ({config.SelectedAddons.Count}):");
        if (config.SelectedAddons.Count == 0)
        {
            Console.WriteLine("  (Brak wybranych dodatków)");
        }
        else
        {
            foreach (var addon in config.SelectedAddons.OrderBy(a => a.Id))
            {
                Console.WriteLine($"  + [ID: {addon.Id}] {addon.Name} -> {addon.Price:F2} zł");
            }

            Console.WriteLine($"  Suma dodatków oferty głównej: {config.BaseAddonsPrice:F2} zł");
        }

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Wybrane powiązane oferty ({config.SelectedRelatedOffers.Count}):");
        if (config.SelectedRelatedOffers.Count == 0)
        {
            Console.WriteLine("  (Brak dołączonych ofert powiązanych)");
        }
        else
        {
            foreach (var relatedSelection in config.SelectedRelatedOffers.OrderBy(r => r.Offer.Id))
            {
                var relatedOffer = relatedSelection.Offer;
                Console.WriteLine(
                    $"  + [ID: {relatedOffer.Id}] {relatedOffer.Name} ({relatedOffer.Date:yyyy-MM-dd}) – cena bazowa: {relatedOffer.Price:F2} zł");

                if (relatedSelection.SelectedAddons.Count > 0)
                {
                    foreach (var addon in relatedSelection.SelectedAddons.OrderBy(a => a.Id))
                    {
                        Console.WriteLine($"      * Dodatek [ID: {addon.Id}] {addon.Name} -> {addon.Price:F2} zł");
                    }

                    Console.WriteLine($"      Podsuma z dodatkami: {relatedSelection.Subtotal:F2} zł");
                }
            }
        }

        var (pkgMin, pkgMax, minSrc, maxSrc) = config.GetPackageGroupSizeBounds(attractionResolver);
        Console.WriteLine("------------------------------------------------------------");
        if (pkgMin > pkgMax)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Limit grupy pakietu : SPRZECZNOŚĆ ({minSrc}: min. {pkgMin} os. vs {maxSrc}: maks. {pkgMax} os.)");
            Console.ResetColor();
        }
        else if (pkgMin > 1 && pkgMin == pkgMax)
        {
            Console.WriteLine($"Limit grupy pakietu : TYLKO GRUPA DOKŁADNIE {pkgMin} OS. [{minSrc}]");
        }
        else
        {
            Console.WriteLine($"Limit grupy pakietu : od {pkgMin} do {pkgMax} os. (maks. limit z: {maxSrc})");
        }

        var packageFeatures = config.GetPackageEffectiveFeatures(InMemoryData.Features, attractionResolver);
        if (packageFeatures.Count > 0)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Zagregowane wymagania i cechy całego pakietu:");
            foreach (var pf in packageFeatures.Values.OrderBy(x => x.FeatureId))
            {
                if (defById.TryGetValue(pf.FeatureId, out var def))
                {
                    Console.WriteLine($"  * {def.Name}: {def.FormatValue(pf.Value)}  [źródło: {pf.SourceElement}]");
                }
            }
        }

        if (activeProfile is not null)
        {
            Console.WriteLine();
            var report = config.ValidateAgainst(activeProfile, InMemoryData.Features, attractionResolver);
            DisplayCompatibilityReport(report, includeMetRequirements: true);
        }

        Console.WriteLine("============================================================");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"CENA PAKIETU (1 OSOBA / BILET): {config.TotalPrice:F2} zł");
        Console.ResetColor();
        Console.WriteLine("============================================================");
    }

    public static void DisplayOrdersList(IReadOnlyCollection<Order> orders)
    {
        if (orders.Count == 0)
        {
            PrintInfo("Nie złożono jeszcze żadnych zamówień w tej sesji.");
            return;
        }

        Console.WriteLine("----------------------------------------------------------------------------------------------------------------");
        Console.WriteLine(
            $"{"ID",-4} | {"Nr zamówienia",-15} | {"Data złożenia",-16} | {"Zamawiający",-24} | {"Bilety",-6} | {"Oferty",-6} | {"Kwota łączna",-14}");
        Console.WriteLine("----------------------------------------------------------------------------------------------------------------");

        foreach (var order in orders)
        {
            Console.WriteLine(
                $"{order.Id,-4} | {order.OrderNumber,-15} | {order.CreatedAt:yyyy-MM-dd HH:mm} | {Truncate(order.CustomerName, 24),-24} | {order.TicketsCount,-6} | {order.Items.Count,-6} | {$"{order.TotalPrice:F2} zł",14}");
        }

        Console.WriteLine("----------------------------------------------------------------------------------------------------------------");
    }

    public static void DisplayOrderDetails(Order order)
    {
        Console.WriteLine("====================================================================");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"           POTWIERDZENIE ZAMÓWIENIA NR {order.OrderNumber}");
        Console.ResetColor();
        Console.WriteLine("====================================================================");
        Console.WriteLine($"ID zamówienia   : {order.Id}");
        Console.WriteLine($"Data złożenia   : {order.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Zamawiający     : {order.CustomerName}");
        Console.WriteLine($"Adres e-mail    : {(string.IsNullOrWhiteSpace(order.CustomerEmail) ? "(nie podano)" : order.CustomerEmail)}");
        Console.WriteLine($"Liczba biletów  : {order.TicketsCount}");
        Console.WriteLine($"Forma płatności : {FormatPaymentMethod(order.PaymentMethod)}");
        Console.WriteLine("--------------------------------------------------------------------");
        Console.WriteLine("Pozycje w zamówieniu (konfiguracja na 1 bilet):");

        foreach (var item in order.Items)
        {
            string badge = item.IsMainOffer ? "[OFERTA GŁÓWNA]" : "[POWIĄZANA]    ";
            Console.WriteLine();
            Console.WriteLine($"  {badge} [ID: {item.OfferId}] {item.OfferName}");
            Console.WriteLine($"                  Atrakcja : {item.AttractionName}");
            Console.WriteLine($"                  Termin   : {item.Date:yyyy-MM-dd}");
            Console.WriteLine($"                  Cena baz.: {item.BasePrice:F2} zł");

            if (item.SelectedAddons.Count > 0)
            {
                Console.WriteLine("                  Wybrane dodatki:");
                foreach (var addon in item.SelectedAddons)
                {
                    Console.WriteLine($"                    + [ID: {addon.AddonId}] {addon.Name} -> {addon.Price:F2} zł");
                }
            }
            else
            {
                Console.WriteLine("                  Wybrane dodatki: (brak)");
            }

            if (item.EffectiveFeaturesSnapshot.Count > 0)
            {
                Console.WriteLine($"                  Cechy poz.: {string.Join(" | ", item.EffectiveFeaturesSnapshot)}");
            }

            Console.WriteLine($"                  Podsuma pozycji: {item.ItemSubtotal:F2} zł");
        }

        Console.WriteLine("--------------------------------------------------------------------");
        Console.WriteLine($"Cena bazowa pakietu (1 bilet normalny): {order.PricePerPerson:F2} zł");

        if (order.ParticipantGroups.Count > 0)
        {
            Console.WriteLine("Podział uczestników i naliczone ulgi kategorii:");
            foreach (var grp in order.ParticipantGroups)
            {
                string discText = grp.DiscountPercent > 0 ? $"(-{grp.DiscountPercent:0}%)" : "(0%)";
                Console.WriteLine(
                    $"  * {grp.CategoryName,-32} x {grp.Count,-3} | {grp.FinalUnitPrice,8:F2} zł/os. {discText,-7} = {grp.LineSubtotal,9:F2} zł");
            }

            if (order.CategoryDiscountsSavings > 0)
            {
                Console.WriteLine($"Suma po ulgach indywidualnych         : {order.SubtotalAfterCategoryDiscounts:F2} zł (oszczędność na ulgach: -{order.CategoryDiscountsSavings:F2} zł)");
            }

            if (order.ExtraGroupDiscountAmount > 0)
            {
                Console.WriteLine($"Dodatkowy rabat grupowy (-{order.ExtraGroupDiscountPercent:0}%)        : -{order.ExtraGroupDiscountAmount:F2} zł");
            }

            if (order.TotalSavings > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Łączna oszczędność na zniżkach        : -{order.TotalSavings:F2} zł (cena regularna: {order.RegularGrossPrice:F2} zł)");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine($"Liczba biletów                        : x {order.TicketsCount}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"RAZEM DO ZAPŁATY                      : {order.TotalPrice:F2} zł");
        Console.ResetColor();
        Console.WriteLine("====================================================================");
    }

    public static void DisplayPackagePriceBreakdown(PackagePriceBreakdown priceCalc)
    {
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("              KALKULACJA CENY GRUPOWEJ I ZNIŻEK BILETOWYCH");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine($"Cena bazowa pakietu (1 bilet normalny): {priceCalc.BasePricePerPerson:F2} zł");
        Console.WriteLine($"Łączna liczba uczestników             : {priceCalc.TotalParticipants} os.");
        Console.WriteLine("Podział na kategorie uczestników:");

        foreach (var line in priceCalc.CategoryLines)
        {
            string discBadge = line.DiscountPercent > 0 ? $"-{line.DiscountPercent:0}%" : "0%";
            Console.WriteLine(
                $"  - {line.CategoryName,-32} | {line.Count,2} os. x {line.FinalUnitPrice,8:F2} zł ({discBadge,4}) = {line.LineSubtotal,9:F2} zł");
        }

        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine($"Wartość regularna bez zniżek          : {priceCalc.RegularGrossTotal:F2} zł");
        if (priceCalc.CategoryDiscountsSavings > 0)
        {
            Console.WriteLine($"Zniżki ulgowe (dzieci/uczniowie/itd.) : -{priceCalc.CategoryDiscountsSavings:F2} zł");
        }

        if (priceCalc.ExtraGroupDiscountAmount > 0)
        {
            Console.WriteLine($"Dodatkowy rabat grupowy (-{priceCalc.ExtraGroupDiscountPercent:0}%)        : -{priceCalc.ExtraGroupDiscountAmount:F2} zł");
        }

        if (priceCalc.TotalSavings > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Łącznie oszczędzasz                   : {priceCalc.TotalSavings:F2} zł");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"KWOTA KOŃCOWA DO ZAPŁATY              : {priceCalc.FinalTotalPrice:F2} zł");
        Console.ResetColor();
        Console.WriteLine("--------------------------------------------------------------------------------");
    }

    public static ParticipantGroupBreakdown PromptGroupBreakdownInteractive(
        int minAllowed,
        int maxAllowed,
        ParticipantProfile? activeProfile = null)
    {
        if (activeProfile is not null)
        {
            var fromProfile = activeProfile.ToGroupBreakdown();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Wyliczony skład grupy z Twojego profilu: {fromProfile.FormatCompactSummary()}");
            Console.ResetColor();

            if (fromProfile.TotalCount >= minAllowed && fromProfile.TotalCount <= maxAllowed)
            {
                if (ReadConfirmation("Czy zastosować ten podział biletów i zniżek z profilu?", defaultYes: true))
                {
                    return fromProfile;
                }
            }
            else
            {
                PrintWarning(
                    $"Skład w profilu ({fromProfile.TotalCount} os.) nie mieści się w wymaganym limicie ({minAllowed}–{maxAllowed} os.). Podaj docelowy podział grupy:");
            }
        }

        Console.WriteLine();
        string boundsHint = minAllowed == maxAllowed
            ? $"dokładnie {minAllowed} os."
            : $"od {minAllowed} do {maxAllowed} os.";

        Console.WriteLine($"Określ skład uczestników / grupy (wymagane: {boundsHint}):");
        Console.WriteLine("  1. Szybki wybór – podaj tylko łączną liczbę biletów normalnych (Dorosły, 0% zniżki) [Domyślnie - Enter]");
        Console.WriteLine("  2. Podziel grupę na kategorie zniżkowe (Dorośli, Dzieci -50%, Młodzież/Studenci -30%, Seniorzy -25%, OzN -50%)");

        while (true)
        {
            string mode = ReadMenuChoice("Wybierz tryb (1-2): ");
            if (string.IsNullOrEmpty(mode) || mode == "1")
            {
                int defaultCount = Math.Clamp(activeProfile?.ParticipantsCount ?? minAllowed, minAllowed, maxAllowed);
                int count = minAllowed == maxAllowed
                    ? ReadIntInRangeOrDefault($"Liczba biletów (oferta wyłącznie dla {minAllowed} os.)", minAllowed, maxAllowed, minAllowed)
                    : ReadIntInRangeOrDefault($"Liczba biletów ({minAllowed}-{maxAllowed})", minAllowed, maxAllowed, defaultCount);

                return ParticipantGroupBreakdown.FromAdultsCount(count);
            }

            if (mode == "2")
            {
                break;
            }

            PrintError("Wybierz 1 lub 2.");
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"Wpisz liczbę osób w poszczególnych kategoriach (łącznie musi być {boundsHint}):");
            int adults = ReadIntInRangeOrDefault("  Dorośli – bilet normalny (0% zniżki)", 0, maxAllowed, defaultValue: 0);
            int children = ReadIntInRangeOrDefault("  Dzieci do 12 lat (-50% zniżki)", 0, maxAllowed, defaultValue: 0);
            int youth = ReadIntInRangeOrDefault("  Młodzież szkolna / Studenci 13–26 lat (-30% zniżki)", 0, maxAllowed, defaultValue: 0);
            int seniors = ReadIntInRangeOrDefault("  Seniorzy 65+ (-25% zniżki)", 0, maxAllowed, defaultValue: 0);
            int disabled = ReadIntInRangeOrDefault("  Osoby z niepełnosprawnością (-50% zniżki)", 0, maxAllowed, defaultValue: 0);

            var breakdown = new ParticipantGroupBreakdown(adults, children, youth, seniors, disabled);

            if (breakdown.TotalCount < minAllowed || breakdown.TotalCount > maxAllowed)
            {
                PrintError(
                    $"Łączna liczba wskazanych osób wynosi {breakdown.TotalCount}, natomiast pakiet wymaga {boundsHint}. Spróbuj ponownie.");
                continue;
            }

            return breakdown;
        }
    }

    private static bool TryParseDate(string input, out DateTime parsedDate)
    {
        if (DateTime.TryParseExact(
                input,
                AcceptedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate))
        {
            return true;
        }

        return DateTime.TryParse(input, CultureInfo.GetCultureInfo("pl-PL"), DateTimeStyles.None, out parsedDate);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }
}
