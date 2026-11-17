using Attractions.DTOs;
using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class UserMenu
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;
    private readonly OfferExtensionService _offerExtensionService;
    private readonly OrderService _orderService;

    private OfferConfiguration? _activeConfiguration;

    public UserMenu(
        OfferService offerService,
        AttractionService attractionService,
        OfferExtensionService offerExtensionService,
        OrderService orderService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
        _offerExtensionService = offerExtensionService ?? throw new ArgumentNullException(nameof(offerExtensionService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public void Show()
    {
        while (true)
        {
            RefreshActiveConfiguration();

            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("            UŻYTKOWNIK");
            Console.WriteLine("====================================");

            if (_activeConfiguration is not null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[Koszyk / konfiguracja: #{_activeConfiguration.BaseOffer.Id} {_activeConfiguration.BaseOffer.Name} | " +
                    $"Dodatki: {_activeConfiguration.SelectedAddons.Count} | " +
                    $"Powiązane oferty: {_activeConfiguration.SelectedRelatedOffers.Count} | " +
                    $"Suma: {_activeConfiguration.TotalPrice:F2} zł]");
                Console.ResetColor();
            }

            Console.WriteLine("1. Wyświetl dostępne oferty");
            Console.WriteLine("2. Wyświetl szczegóły oferty");
            Console.WriteLine("3. Wyświetl dodatki do oferty");
            Console.WriteLine("4. Rozszerz ofertę o dodatki");
            Console.WriteLine("5. Wyświetl powiązane oferty");
            Console.WriteLine("6. Rozszerz ofertę o powiązane oferty");
            Console.WriteLine("7. Wybierz atrakcję i ofertę (przeglądanie wg atrakcji)");
            Console.WriteLine("8. Złóż zamówienie (pełny proces zakupu krok po kroku)");
            Console.WriteLine("9. Wyświetl złożone zamówienia");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowAvailableOffers();
                    break;
                case "2":
                    ShowOfferDetails();
                    break;
                case "3":
                    ShowOfferAddons();
                    break;
                case "4":
                    ExtendOfferWithAddons();
                    break;
                case "5":
                    ShowRelatedOffers();
                    break;
                case "6":
                    ExtendOfferWithRelatedOffers();
                    break;
                case "7":
                    BrowseAttractionsAndSelectOffer();
                    break;
                case "8":
                    RunFullPurchaseProcess();
                    break;
                case "9":
                    ShowPlacedOrders();
                    break;
                case "0":
                    return;
                default:
                    ConsoleHelper.PrintError("Nieprawidłowa opcja menu. Spróbuj ponownie.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }

    private void RefreshActiveConfiguration()
    {
        if (_activeConfiguration is null)
        {
            return;
        }

        var syncResult = _offerExtensionService.SynchronizeConfiguration(_activeConfiguration);
        if (!syncResult.IsSuccess)
        {
            _activeConfiguration = null;
        }
    }

    private Offer? SelectAvailableOffer(string header)
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader(header);

        var availableOffers = _offerService.GetAvailableOffersForUser();
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById);
        if (availableOffers.Count == 0)
        {
            return null;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty: ");
        var result = _offerService.GetAvailableOfferForUser(offerId);
        if (!result.IsSuccess || result.Data is null)
        {
            ConsoleHelper.PrintResult(result);
            return null;
        }

        return result.Data;
    }

    private void ShowAvailableOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DOSTĘPNE OFERTY");
        var availableOffers = _offerService.GetAvailableOffersForUser();
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void ShowOfferDetails()
    {
        var offer = SelectAvailableOffer("SZCZEGÓŁY DOSTĘPNEJ OFERTY");
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var attraction = _attractionService.GetAttractionById(offer.AttractionId);
        var relatedAvailableOffers = _offerService.GetRelatedOffers(offer.Id, onlyAvailableForUser: true);
        ConsoleHelper.DisplayOfferDetails(offer, attraction, relatedAvailableOffers);
        ConsoleHelper.Pause();
    }

    private void ShowOfferAddons()
    {
        var offer = SelectAvailableOffer("DODATKI DO OFERTY");
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var addons = offer.Addons.OrderBy(a => a.Id).ToList();
        ConsoleHelper.DisplayAddonsList(offer, addons);
        ConsoleHelper.Pause();
    }

    private void ShowRelatedOffers()
    {
        var offer = SelectAvailableOffer("DOSTĘPNE OFERTY POWIĄZANE");
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Dostępne oferty powiązane z ofertą \"{offer.Name}\" (ID: {offer.Id}):");
        var relatedOffers = _offerService.GetRelatedOffers(offer.Id, onlyAvailableForUser: true);
        ConsoleHelper.DisplayOffersList(relatedOffers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void BrowseAttractionsAndSelectOffer()
    {
        var selectedConfig = SelectOfferViaAttractionStep(
            "WYBÓR ATRAKCJI I OFERTY",
            "OFERTY WYBRANEJ ATRAKCJI");

        if (selectedConfig is null)
        {
            return;
        }

        _activeConfiguration = selectedConfig;

        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSuccess(
            $"Wybrano ofertę \"{selectedConfig.BaseOffer.Name}\" (ID: {selectedConfig.BaseOffer.Id}) jako aktywną konfigurację.");

        var attraction = _attractionService.GetAttractionById(selectedConfig.BaseOffer.AttractionId);
        var relatedOffers = _offerService.GetRelatedOffers(selectedConfig.BaseOffer.Id, onlyAvailableForUser: true);
        ConsoleHelper.DisplayOfferDetails(selectedConfig.BaseOffer, attraction, relatedOffers);

        Console.WriteLine();
        bool startWizardNow = ConsoleHelper.ReadConfirmation(
            "Czy chcesz od razu przejść przez dobór dodatków, ofert powiązanych i złożyć zamówienie?",
            defaultYes: true);

        if (startWizardNow)
        {
            RunBaseAddonsStep(selectedConfig, isWizardStep: true);
            RunRelatedOffersStep(selectedConfig, isWizardStep: true);
            RunCheckoutStep(selectedConfig);
        }
    }

    private void RunFullPurchaseProcess()
    {
        OfferConfiguration? config = null;
        bool jumpDirectlyToCheckout = false;

        if (_activeConfiguration is not null)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader("SKŁADANIE ZAMÓWIENIA – KREATOR ZAKUPU");
            Console.WriteLine(
                $"Posiadasz już wybraną ofertę w konfiguracji: [ID: {_activeConfiguration.BaseOffer.Id}] {_activeConfiguration.BaseOffer.Name} " +
                $"(aktualna suma: {_activeConfiguration.TotalPrice:F2} zł)");
            Console.WriteLine("1. Przejdź przez dobór dodatków, ofert powiązanych i podsumowanie tej oferty [Domyślnie - Enter]");
            Console.WriteLine("2. Przejdź od razu do podsumowania i złożenia zamówienia dla obecnej konfiguracji");
            Console.WriteLine("3. Rozpocznij nowy proces zakupu od wyboru atrakcji i oferty");
            Console.WriteLine("0. Powrót");

            while (true)
            {
                string mode = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");
                if (string.IsNullOrEmpty(mode) || mode == "1")
                {
                    config = _activeConfiguration;
                    break;
                }

                if (mode == "2")
                {
                    config = _activeConfiguration;
                    jumpDirectlyToCheckout = true;
                    break;
                }

                if (mode == "3")
                {
                    break;
                }

                if (mode == "0")
                {
                    return;
                }

                ConsoleHelper.PrintError("Wybierz 1, 2, 3 lub 0.");
            }
        }

        if (config is null)
        {
            config = SelectOfferViaAttractionStep(
                "KROK 1/5 – WYBÓR ATRAKCJI TURYSTYCZNEJ",
                "KROK 2/5 – WYBÓR OFERTY DLA WYBRANEJ ATRAKCJI");

            if (config is null)
            {
                return;
            }

            _activeConfiguration = config;
        }

        if (!jumpDirectlyToCheckout)
        {
            RunBaseAddonsStep(config, isWizardStep: true);
            RunRelatedOffersStep(config, isWizardStep: true);
        }

        RunCheckoutStep(config);
    }

    private OfferConfiguration? SelectOfferViaAttractionStep(string attractionStepHeader, string offerStepHeader)
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader(attractionStepHeader);

            var availableAttractions = _orderService.GetAttractionsWithAvailableOffers();
            if (availableAttractions.Count == 0)
            {
                ConsoleHelper.PrintInfo("Obecnie żadna atrakcja nie posiada dostępnych aktywnych ofert.");
                ConsoleHelper.Pause();
                return null;
            }

            ConsoleHelper.DisplayAttractionsList(
                availableAttractions,
                id => _orderService.GetAvailableOffersForAttraction(id).Count);

            int attractionId = ConsoleHelper.ReadInt("Podaj ID wybranej atrakcji (0 = anuluj): ");
            if (attractionId == 0)
            {
                return null;
            }

            var selectedAttraction = availableAttractions.FirstOrDefault(a => a.Id == attractionId);
            if (selectedAttraction is null)
            {
                ConsoleHelper.PrintError($"Atrakcja o ID {attractionId} nie istnieje lub nie posiada dostępnych ofert.");
                ConsoleHelper.Pause();
                continue;
            }

            while (true)
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.PrintSectionHeader(offerStepHeader);

                var offersForAttraction = _orderService.GetAvailableOffersForAttraction(selectedAttraction.Id);
                ConsoleHelper.DisplayAttractionDetails(selectedAttraction, offersForAttraction);
                Console.WriteLine();

                int offerId = ConsoleHelper.ReadInt("Podaj ID oferty, którą chcesz wybrać (0 = wróć do listy atrakcji): ");
                if (offerId == 0)
                {
                    break;
                }

                var chosenOffer = offersForAttraction.FirstOrDefault(o => o.Id == offerId);
                if (chosenOffer is null)
                {
                    ConsoleHelper.PrintError(
                        $"Oferta o ID {offerId} nie należy do atrakcji \"{selectedAttraction.Name}\" lub nie jest dostępna.");
                    ConsoleHelper.Pause();
                    continue;
                }

                var createResult = _offerExtensionService.CreateConfiguration(chosenOffer.Id);
                if (!createResult.IsSuccess || createResult.Data is null)
                {
                    ConsoleHelper.PrintResult(createResult);
                    ConsoleHelper.Pause();
                    continue;
                }

                return createResult.Data;
            }
        }
    }

    private OfferConfiguration? ResolveConfigurationForExtension(string header)
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader(header);

        if (_activeConfiguration is not null)
        {
            Console.WriteLine(
                $"Aktualnie rozszerzana oferta: [ID: {_activeConfiguration.BaseOffer.Id}] {_activeConfiguration.BaseOffer.Name} " +
                $"(aktualna cena konfiguracji: {_activeConfiguration.TotalPrice:F2} zł)");
            Console.WriteLine("1. Kontynuuj rozszerzanie tej oferty [Domyślnie - Enter]");
            Console.WriteLine("2. Wybierz inną ofertę z listy wszystkich dostępnych ofert");
            Console.WriteLine("3. Wybierz inną ofertę przeglądając po atrakcjach");
            Console.WriteLine("0. Powrót");

            while (true)
            {
                string mode = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");
                if (string.IsNullOrEmpty(mode) || mode == "1")
                {
                    return _activeConfiguration;
                }

                if (mode == "0")
                {
                    return null;
                }

                if (mode == "3")
                {
                    var byAttraction = SelectOfferViaAttractionStep(
                        "WYBÓR ATRAKCJI",
                        "WYBÓR OFERTY DLA ATRAKCJI");

                    if (byAttraction is not null)
                    {
                        _activeConfiguration = byAttraction;
                    }

                    return byAttraction;
                }

                if (mode == "2")
                {
                    Console.WriteLine();
                    break;
                }

                ConsoleHelper.PrintError("Wybierz 1, 2, 3 lub 0.");
            }
        }

        var availableOffers = _offerService.GetAvailableOffersForUser();
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById);
        if (availableOffers.Count == 0)
        {
            ConsoleHelper.Pause();
            return null;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty do rozszerzenia (0 = anuluj): ");
        if (offerId == 0)
        {
            return null;
        }

        var createResult = _offerExtensionService.CreateConfiguration(offerId);
        if (!createResult.IsSuccess || createResult.Data is null)
        {
            ConsoleHelper.PrintResult(createResult);
            ConsoleHelper.Pause();
            return null;
        }

        _activeConfiguration = createResult.Data;
        return _activeConfiguration;
    }

    private void ExtendOfferWithAddons()
    {
        var config = ResolveConfigurationForExtension("ROZSZERZANIE OFERTY O DODATKI");
        if (config is null)
        {
            return;
        }

        RunBaseAddonsStep(config, isWizardStep: false);

        ConsoleHelper.ClearScreen();
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById);
        ConsoleHelper.PrintInfo("Aby przejść do złożenia zamówienia dla tej konfiguracji, wybierz w menu opcję 8.");
        ConsoleHelper.Pause();
    }

    private void ExtendOfferWithRelatedOffers()
    {
        var config = ResolveConfigurationForExtension("ROZSZERZANIE OFERTY O POWIĄZANE OFERTY");
        if (config is null)
        {
            return;
        }

        RunRelatedOffersStep(config, isWizardStep: false);

        ConsoleHelper.ClearScreen();
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById);
        ConsoleHelper.PrintInfo("Aby przejść do złożenia zamówienia dla tej konfiguracji, wybierz w menu opcję 8.");
        ConsoleHelper.Pause();
    }

    private void RunBaseAddonsStep(OfferConfiguration config, bool isWizardStep)
    {
        string header = isWizardStep
            ? "KROK 3/5 – PROPONOWANE DODATKI DO WYBRANEJ OFERTY"
            : "WYBÓR DODATKÓW DO OFERTY GŁÓWNEJ";

        if (config.BaseOffer.Addons.Count == 0)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader(header);
            ConsoleHelper.PrintInfo($"Oferta \"{config.BaseOffer.Name}\" nie posiada dodatkowych opcji płatnych.");
            if (isWizardStep)
            {
                ConsoleHelper.Pause();
            }

            return;
        }

        string? lastFeedback = null;
        bool lastFeedbackSuccess = true;

        while (true)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader(header);
            Console.WriteLine(
                $"Wybrana oferta: [ID: {config.BaseOffer.Id}] {config.BaseOffer.Name} | Cena podstawowa: {config.BasePrice:F2} zł");
            Console.WriteLine("Proponowane dodatki:");
            Console.WriteLine("--------------------------------------------------------------------------------");

            foreach (var addon in config.BaseOffer.Addons.OrderBy(a => a.Id))
            {
                bool isSelected = config.SelectedAddons.Any(a => a.Id == addon.Id);
                string mark = isSelected ? "[X]" : "[ ]";
                string desc = string.IsNullOrWhiteSpace(addon.Description) ? string.Empty : $" – {addon.Description}";
                Console.WriteLine($"{mark} ID: {addon.Id,-3} | {addon.Name,-38} | {addon.Price,8:F2} zł{desc}");
            }

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(
                $"Wybrane dodatki główne: {config.BaseAddonsPrice:F2} zł | " +
                $"Powiązane oferty (z dodatkami): {config.SelectedRelatedOffers.Sum(r => r.Subtotal):F2} zł | " +
                $"SUMA PAKIETU: {config.TotalPrice:F2} zł");

            if (!string.IsNullOrEmpty(lastFeedback))
            {
                Console.WriteLine();
                if (lastFeedbackSuccess)
                {
                    ConsoleHelper.PrintSuccess(lastFeedback);
                }
                else
                {
                    ConsoleHelper.PrintError(lastFeedback);
                }
            }

            Console.WriteLine();
            string finishLabel = isWizardStep
                ? "'0' lub Enter = przejdź dalej do ofert powiązanych"
                : "'0' = zakończ i pokaż podsumowanie";

            string input = ConsoleHelper.ReadMenuChoice(
                $"Podaj ID dodatku, aby go zaznaczyć/odznaczyć ('C' = wyczyść dodatki główne, {finishLabel}): ");

            if (input == "0" || (isWizardStep && string.IsNullOrEmpty(input)))
            {
                return;
            }

            if (input.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                var clearResult = _offerExtensionService.ClearBaseAddons(config);
                lastFeedback = clearResult.Message;
                lastFeedbackSuccess = clearResult.IsSuccess;
                continue;
            }

            if (!int.TryParse(input, out int addonId))
            {
                lastFeedback = "Wprowadź poprawne ID dodatku, literę C lub 0.";
                lastFeedbackSuccess = false;
                continue;
            }

            var toggleResult = _offerExtensionService.ToggleBaseAddon(config, addonId);
            lastFeedback = toggleResult.Message;
            lastFeedbackSuccess = toggleResult.IsSuccess;
        }
    }

    private void RunRelatedOffersStep(OfferConfiguration config, bool isWizardStep)
    {
        string header = isWizardStep
            ? "KROK 4/5 – PROPONOWANE OFERTY POWIĄZANE"
            : "WYBÓR POWIĄZANYCH OFERT DO ROZSZERZENIA";

        var availableRelated = _offerService.GetRelatedOffers(config.BaseOffer.Id, onlyAvailableForUser: true);
        if (availableRelated.Count == 0)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader(header);
            ConsoleHelper.PrintInfo($"Oferta \"{config.BaseOffer.Name}\" nie posiada obecnie żadnych dostępnych ofert powiązanych.");
            if (isWizardStep)
            {
                ConsoleHelper.Pause();
            }

            return;
        }

        string? lastFeedback = null;
        bool lastFeedbackSuccess = true;

        while (true)
        {
            availableRelated = _offerService.GetRelatedOffers(config.BaseOffer.Id, onlyAvailableForUser: true);
            if (availableRelated.Count == 0)
            {
                return;
            }

            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader(header);
            Console.WriteLine(
                $"Oferta główna: [ID: {config.BaseOffer.Id}] {config.BaseOffer.Name} | Cena z dodatkami głównymi: {(config.BasePrice + config.BaseAddonsPrice):F2} zł");
            Console.WriteLine("Polecane powiązane oferty uzupełniające zwiedzanie:");
            Console.WriteLine("--------------------------------------------------------------------------------------------------------");

            foreach (var relatedOffer in availableRelated)
            {
                var selection = _offerExtensionService.GetSelectedRelatedOffer(config, relatedOffer.Id);
                string mark = selection is not null ? "[X]" : "[ ]";
                string attractionName = _attractionService.GetAttractionById(relatedOffer.AttractionId)?.Name ?? "(nieznana)";

                Console.WriteLine(
                    $"{mark} ID: {relatedOffer.Id,-3} | {relatedOffer.Name,-38} | {attractionName,-24} | {relatedOffer.Date:yyyy-MM-dd} | {relatedOffer.Price,8:F2} zł");

                if (selection is not null)
                {
                    Console.WriteLine(
                        $"        Dołączono -> wybrane dodatki powiązanej oferty: {selection.SelectedAddons.Count}/{relatedOffer.Addons.Count} | Podsuma: {selection.Subtotal:F2} zł");
                }
                else if (relatedOffer.Addons.Count > 0)
                {
                    Console.WriteLine($"        Dostępne dodatki w tej ofercie powiązanej: {relatedOffer.Addons.Count}");
                }
            }

            Console.WriteLine("--------------------------------------------------------------------------------------------------------");
            Console.WriteLine(
                $"Oferta główna z dodatkami: {(config.BasePrice + config.BaseAddonsPrice):F2} zł | " +
                $"Powiązane oferty z dodatkami: {config.SelectedRelatedOffers.Sum(r => r.Subtotal):F2} zł | " +
                $"SUMA PAKIETU: {config.TotalPrice:F2} zł");

            if (!string.IsNullOrEmpty(lastFeedback))
            {
                Console.WriteLine();
                if (lastFeedbackSuccess)
                {
                    ConsoleHelper.PrintSuccess(lastFeedback);
                }
                else
                {
                    ConsoleHelper.PrintError(lastFeedback);
                }
            }

            Console.WriteLine();
            string finishLabel = isWizardStep
                ? "'0' lub Enter = przejdź do podsumowania i zamówienia"
                : "'0' = zakończ i pokaż podsumowanie";

            string input = ConsoleHelper.ReadMenuChoice(
                $"Podaj ID powiązanej oferty, aby ją dołączyć / edytować ({finishLabel}): ");

            if (input == "0" || (isWizardStep && string.IsNullOrEmpty(input)))
            {
                return;
            }

            if (!int.TryParse(input, out int relatedOfferId))
            {
                lastFeedback = "Wprowadź poprawne ID oferty powiązanej lub 0.";
                lastFeedbackSuccess = false;
                continue;
            }

            var existingSelection = _offerExtensionService.GetSelectedRelatedOffer(config, relatedOfferId);
            if (existingSelection is null)
            {
                var addResult = _offerExtensionService.AddRelatedOffer(config, relatedOfferId);
                lastFeedback = addResult.Message;
                lastFeedbackSuccess = addResult.IsSuccess;

                if (addResult.IsSuccess && addResult.Data is not null && addResult.Data.Offer.Addons.Count > 0)
                {
                    ManageRelatedOfferAddons(config, addResult.Data);
                }
            }
            else
            {
                HandleExistingRelatedOfferAction(config, existingSelection, out lastFeedback, out lastFeedbackSuccess);
            }
        }
    }

    private void RunCheckoutStep(OfferConfiguration config)
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("KROK 5/5 – PODSUMOWANIE I FINALIZACJA ZAMÓWIENIA");
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById);

        Console.WriteLine();
        Console.WriteLine("Uzupełnij dane zamówienia (lub wpisz 0 przy potwierdzeniu, aby zachować koszyk bez zamawiania):");

        int ticketsCount = ConsoleHelper.ReadIntInRangeOrDefault("Liczba biletów / uczestników (1-100)", 1, 100, defaultValue: 1);
        decimal finalTotal = config.TotalPrice * ticketsCount;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"Kalkulacja końcowa: {config.TotalPrice:F2} zł (pakiet 1 os.) x {ticketsCount} = {finalTotal:F2} zł");
        Console.ResetColor();
        Console.WriteLine();

        string customerName = ConsoleHelper.ReadRequiredString("Imię i nazwisko zamawiającego: ", "Imię i nazwisko");
        string customerEmail = ConsoleHelper.ReadOptionalEmail("Adres e-mail do potwierdzenia (opcjonalnie, Enter = pomiń): ");
        Console.WriteLine();
        PaymentMethod paymentMethod = ConsoleHelper.ReadPaymentMethod();

        Console.WriteLine();
        bool confirmed = ConsoleHelper.ReadConfirmation(
            $"Czy potwierdzasz złożenie zamówienia na łączną kwotę {finalTotal:F2} zł ({ConsoleHelper.FormatPaymentMethod(paymentMethod)})?",
            defaultYes: true);

        if (!confirmed)
        {
            ConsoleHelper.PrintInfo("Złożenie zamówienia zostało wstrzymane – Twoja konfiguracja nadal pozostaje zapisana w sesji.");
            ConsoleHelper.Pause();
            return;
        }

        var orderResult = _orderService.PlaceOrder(config, customerName, customerEmail, ticketsCount, paymentMethod);
        if (!orderResult.IsSuccess || orderResult.Data is null)
        {
            Console.WriteLine();
            ConsoleHelper.PrintResult(orderResult);
            ConsoleHelper.Pause();
            return;
        }

        _activeConfiguration = null;

        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintResult(orderResult);
        Console.WriteLine();
        ConsoleHelper.DisplayOrderDetails(orderResult.Data);
        ConsoleHelper.Pause();
    }

    private void ShowPlacedOrders()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader("MOJE ZŁOŻONE ZAMÓWIENIA");

            var orders = _orderService.GetAllOrders();
            ConsoleHelper.DisplayOrdersList(orders);

            if (orders.Count == 0)
            {
                ConsoleHelper.Pause();
                return;
            }

            Console.WriteLine();
            int orderId = ConsoleHelper.ReadInt("Podaj ID zamówienia, aby wyświetlić pełne potwierdzenie (0 = powrót): ");
            if (orderId == 0)
            {
                return;
            }

            var order = _orderService.GetOrderById(orderId);
            if (order is null)
            {
                ConsoleHelper.PrintError($"Zamówienie o ID {orderId} nie istnieje.");
                ConsoleHelper.Pause();
                continue;
            }

            ConsoleHelper.ClearScreen();
            ConsoleHelper.DisplayOrderDetails(order);
            ConsoleHelper.Pause();
        }
    }

    private void HandleExistingRelatedOfferAction(
        OfferConfiguration config,
        SelectedRelatedOffer selection,
        out string? feedback,
        out bool feedbackSuccess)
    {
        if (selection.Offer.Addons.Count == 0)
        {
            var removeResult = _offerExtensionService.RemoveRelatedOffer(config, selection.Offer.Id);
            feedback = removeResult.Message;
            feedbackSuccess = removeResult.IsSuccess;
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Oferta powiązana \"{selection.Offer.Name}\" jest już dołączona:");
        Console.WriteLine("  1. Dobierz / odznacz dodatki tej oferty powiązanej");
        Console.WriteLine("  2. Usuń tę ofertę powiązaną z konfiguracji");
        Console.WriteLine("  0. Anuluj");

        string action = ConsoleHelper.ReadMenuChoice("Wybierz akcję: ");
        switch (action)
        {
            case "1":
                ManageRelatedOfferAddons(config, selection);
                feedback = $"Zaktualizowano dodatki dla powiązanej oferty \"{selection.Offer.Name}\".";
                feedbackSuccess = true;
                break;
            case "2":
                var removeResult = _offerExtensionService.RemoveRelatedOffer(config, selection.Offer.Id);
                feedback = removeResult.Message;
                feedbackSuccess = removeResult.IsSuccess;
                break;
            default:
                feedback = null;
                feedbackSuccess = true;
                break;
        }
    }

    private void ManageRelatedOfferAddons(OfferConfiguration config, SelectedRelatedOffer relatedSelection)
    {
        string? feedback = null;
        bool feedbackSuccess = true;

        while (true)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader("DODATKI DO OFERTY POWIĄZANEJ");
            Console.WriteLine(
                $"Oferta powiązana: [ID: {relatedSelection.Offer.Id}] {relatedSelection.Offer.Name} | Cena bazowa: {relatedSelection.BasePrice:F2} zł");
            Console.WriteLine("--------------------------------------------------------------------------------");

            foreach (var addon in relatedSelection.Offer.Addons.OrderBy(a => a.Id))
            {
                bool isSelected = relatedSelection.SelectedAddons.Any(a => a.Id == addon.Id);
                string mark = isSelected ? "[X]" : "[ ]";
                string desc = string.IsNullOrWhiteSpace(addon.Description) ? string.Empty : $" – {addon.Description}";
                Console.WriteLine($"{mark} ID: {addon.Id,-3} | {addon.Name,-38} | {addon.Price,8:F2} zł{desc}");
            }

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(
                $"Podsuma tej oferty powiązanej: {relatedSelection.Subtotal:F2} zł | Łączna cena całej konfiguracji: {config.TotalPrice:F2} zł");

            if (!string.IsNullOrEmpty(feedback))
            {
                Console.WriteLine();
                if (feedbackSuccess)
                {
                    ConsoleHelper.PrintSuccess(feedback);
                }
                else
                {
                    ConsoleHelper.PrintError(feedback);
                }
            }

            Console.WriteLine();
            string input = ConsoleHelper.ReadMenuChoice(
                "Podaj ID dodatku oferty powiązanej, aby go zaznaczyć/odznaczyć ('0' lub Enter = zatwierdź i wróć): ");

            if (input == "0" || string.IsNullOrEmpty(input))
            {
                return;
            }

            if (!int.TryParse(input, out int addonId))
            {
                feedback = "Wprowadź poprawne ID dodatku lub 0.";
                feedbackSuccess = false;
                continue;
            }

            var toggleResult = _offerExtensionService.ToggleRelatedOfferAddon(config, relatedSelection.Offer.Id, addonId);
            feedback = toggleResult.Message;
            feedbackSuccess = toggleResult.IsSuccess;
        }
    }
}
