using System.Globalization;
using Attractions.DTOs;
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

            int atIndex = input.IndexOf('@');
            int dotIndex = atIndex > 0 ? input.IndexOf('.', atIndex + 2) : -1;
            if (!input.Contains(' ') && atIndex > 0 && atIndex == input.LastIndexOf('@') && dotIndex > atIndex + 1 && dotIndex < input.Length - 1)
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

        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine($"{"ID",-4} | {"Nazwa atrakcji",-34} | {"Lokalizacja",-26} | {"Oferty",-6}");
        Console.WriteLine("--------------------------------------------------------------------------------");

        foreach (var attraction in attractions)
        {
            int offersCount = offerCountResolver(attraction.Id);
            Console.WriteLine(
                $"{attraction.Id,-4} | {Truncate(attraction.Name, 34),-34} | {Truncate(attraction.Location, 26),-26} | {offersCount,-6}");
        }

        Console.WriteLine("--------------------------------------------------------------------------------");
    }

    public static void DisplayAttractionDetails(Attraction attraction, IReadOnlyCollection<Offer> assignedOffers)
    {
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"ID atrakcji : {attraction.Id}");
        Console.WriteLine($"Nazwa       : {attraction.Name}");
        Console.WriteLine($"Lokalizacja : {attraction.Location}");
        Console.WriteLine($"Opis        : {(string.IsNullOrWhiteSpace(attraction.Description) ? "(brak opisu)" : attraction.Description)}");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Przypisane oferty ({assignedOffers.Count}):");

        if (assignedOffers.Count == 0)
        {
            Console.WriteLine("  (Brak ofert przypisanych do tej atrakcji)");
            return;
        }

        foreach (var offer in assignedOffers)
        {
            Console.WriteLine(
                $"  - [ID: {offer.Id}] {offer.Name} | Data: {offer.Date:yyyy-MM-dd} | Cena: {offer.Price:F2} zł | Status: {FormatOfferStatus(offer)} | Dodatki: {offer.Addons.Count} | Powiązane: {offer.RelatedOfferIds.Count}");
        }
    }

    public static void DisplayOffersList(IReadOnlyCollection<Offer> offers, Func<int, Attraction?> attractionResolver)
    {
        if (offers.Count == 0)
        {
            PrintInfo("Brak ofert spełniających kryteria.");
            return;
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");
        Console.WriteLine(
            $"{"ID",-4} | {"Nazwa oferty",-32} | {"Atrakcja",-24} | {"Data",-10} | {"Cena",-11} | {"Status",-25}");
        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");

        foreach (var offer in offers)
        {
            string attractionName = attractionResolver(offer.AttractionId)?.Name ?? "(nieznana)";
            string priceText = $"{offer.Price:F2} zł";

            Console.WriteLine(
                $"{offer.Id,-4} | {Truncate(offer.Name, 32),-32} | {Truncate(attractionName, 24),-24} | {offer.Date:yyyy-MM-dd} | {priceText,11} | {FormatOfferStatus(offer),-25}");
        }

        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");
    }

    public static void DisplayOfferDetails(
        Offer offer,
        Attraction? attraction,
        IReadOnlyCollection<Offer> relatedOffers)
    {
        Console.WriteLine("============================================================");
        Console.WriteLine($"SZCZEGÓŁY OFERTY [ID: {offer.Id}]");
        Console.WriteLine("============================================================");
        Console.WriteLine($"Nazwa          : {offer.Name}");
        Console.WriteLine($"Atrakcja       : {(attraction is not null ? $"{attraction.Name} (ID: {attraction.Id}, {attraction.Location})" : "(brak)")}");
        Console.WriteLine($"Opis           : {(string.IsNullOrWhiteSpace(offer.Description) ? "(brak opisu)" : offer.Description)}");
        Console.WriteLine($"Cena bazowa    : {offer.Price:F2} zł");
        Console.WriteLine($"Data realizacji: {offer.Date:yyyy-MM-dd}");
        Console.WriteLine($"Status         : {FormatOfferStatus(offer)}");
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
                Console.WriteLine(
                    $"  - [ID: {related.Id}] {related.Name} | Data: {related.Date:yyyy-MM-dd} | Cena: {related.Price:F2} zł | Status: {FormatOfferStatus(related)}");
            }
        }

        Console.WriteLine("============================================================");
    }

    public static void DisplayAddonsList(Offer offer, IReadOnlyCollection<Addon> addons)
    {
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
        }

        Console.WriteLine("--------------------------------------------------------------------------------");
    }

    public static void DisplayConfigurationSummary(
        OfferConfiguration config,
        Func<int, Attraction?> attractionResolver)
    {
        string attractionName = attractionResolver(config.BaseOffer.AttractionId)?.Name ?? "(nieznana atrakcja)";

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

            Console.WriteLine($"                  Podsuma pozycji: {item.ItemSubtotal:F2} zł");
        }

        Console.WriteLine("--------------------------------------------------------------------");
        Console.WriteLine($"Cena pakietu (1 osoba) : {order.PricePerPerson:F2} zł");
        Console.WriteLine($"Liczba biletów         : x {order.TicketsCount}");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"RAZEM DO ZAPŁATY       : {order.TotalPrice:F2} zł");
        Console.ResetColor();
        Console.WriteLine("====================================================================");
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
