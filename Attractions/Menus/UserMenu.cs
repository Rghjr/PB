using System.Globalization;
using Attractions.DTOs;
using Attractions.Helpers;
using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class UserMenu
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;
    private readonly OfferExtensionService _offerExtensionService;
    private readonly OrderService _orderService;
    private readonly FeatureService _featureService;

    private OfferConfiguration? _activeConfiguration;
    private ParticipantProfile? _participantProfile;

    public UserMenu(
        OfferService offerService,
        AttractionService attractionService,
        OfferExtensionService offerExtensionService,
        OrderService orderService,
        FeatureService featureService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
        _offerExtensionService = offerExtensionService ?? throw new ArgumentNullException(nameof(offerExtensionService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _featureService = featureService ?? throw new ArgumentNullException(nameof(featureService));
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

            if (_participantProfile is not null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Profil uczestnika: {_participantProfile.GetShortSummary(_featureService.GetAllFeatures())}]");
                Console.ResetColor();
            }

            if (_activeConfiguration is not null)
            {
                string compatTag = string.Empty;
                if (_participantProfile is not null)
                {
                    bool ok = _activeConfiguration.ValidateAgainst(_participantProfile).IsCompatible;
                    compatTag = ok ? " | ZGODNY Z PROFILEM" : " | NIEZGODNY Z PROFILEM (!)";
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[Koszyk / konfiguracja: #{_activeConfiguration.BaseOffer.Id} {_activeConfiguration.BaseOffer.Name} | " +
                    $"Dodatki: {_activeConfiguration.SelectedAddons.Count} | " +
                    $"Powiązane oferty: {_activeConfiguration.SelectedRelatedOffers.Count} | " +
                    $"Suma: {_activeConfiguration.TotalPrice:F2} zł{compatTag}]");
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
            Console.WriteLine("10. Ustaw / edytuj profil uczestnika (wiek, dostępność, wymagania)");
            Console.WriteLine("11. Filtruj dostępne oferty według profilu uczestnika");
            Console.WriteLine("12. Sprawdź kompatybilność atrakcji, oferty lub pakietu z profilem");
            Console.WriteLine("13. Wyświetl oferty grupowe (np. tylko dla grup 4-osobowych)");
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
                case "10":
                    ManageParticipantProfile();
                    break;
                case "11":
                    FilterOffersByProfile();
                    break;
                case "12":
                    RunCompatibilityDiagnosticMenu();
                    break;
                case "13":
                    ShowGroupOffersMenu();
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
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById, _participantProfile);
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
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById, _participantProfile);
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
        ConsoleHelper.DisplayOfferDetails(offer, attraction, relatedAvailableOffers, _participantProfile);
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
        ConsoleHelper.DisplayOffersList(relatedOffers, _attractionService.GetAttractionById, _participantProfile);
        ConsoleHelper.Pause();
    }

    private void ManageParticipantProfile()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("PROFIL UCZESTNIKA / GRUPY UCZESTNIKÓW");

        var allFeatures = _featureService.GetAllFeatures();

        if (_participantProfile is not null)
        {
            Console.WriteLine($"Aktualny profil: {_participantProfile.GetShortSummary(allFeatures)}");
            Console.WriteLine("1. Zdefiniuj profil na nowo / zaktualizuj dane [Domyślnie - Enter]");
            Console.WriteLine("2. Wyczyść aktywny profil uczestnika");
            Console.WriteLine("0. Powrót");

            string mode = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");
            if (mode == "0")
            {
                return;
            }

            if (mode == "2")
            {
                _participantProfile = null;
                ConsoleHelper.PrintInfo("Wyczyszczono profil uczestnika w bieżącej sesji.");
                ConsoleHelper.Pause();
                return;
            }
        }

        _participantProfile = BuildParticipantProfileInteractive(allFeatures);
        Console.WriteLine();
        ConsoleHelper.PrintSuccess($"Zapisano profil uczestnika: {_participantProfile.GetShortSummary(allFeatures)}");
        ConsoleHelper.PrintInfo(
            $"Wyliczony podział biletów i ulg grupy: {_participantProfile.ToGroupBreakdown().FormatCompactSummary()}");

        if (_activeConfiguration is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Weryfikacja aktualnego koszyka względem nowego profilu:");
            ConsoleHelper.DisplayCompatibilityReport(_activeConfiguration.ValidateAgainst(_participantProfile), includeMetRequirements: false);
        }

        ConsoleHelper.Pause();
    }

    private static ParticipantProfile BuildParticipantProfileInteractive(IReadOnlyCollection<FeatureDefinition> allFeatures)
    {
        List<int> participantAges;

        Console.WriteLine();
        Console.WriteLine("Podaj wiek uczestnika (np. 25) lub wiek wszystkich osób w grupie po przecinku (np. 36, 34, 8):");
        while (true)
        {
            string rawAges = ConsoleHelper.ReadRequiredString("Wiek uczestnika / grupy: ", "Wiek");
            var tokens = rawAges.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var parsedAges = new List<int>();
            bool allValid = tokens.Length > 0;
            foreach (string token in tokens)
            {
                if (!int.TryParse(token, out int age) || age < 0 || age > 120)
                {
                    allValid = false;
                    break;
                }

                parsedAges.Add(age);
            }

            if (allValid)
            {
                participantAges = parsedAges;
                break;
            }

            ConsoleHelper.PrintError("Podaj poprawny wiek (0-120), np. '14' dla jednej osoby lub '38, 35, 9' dla grupy.");
        }

        decimal? minParticipantHeightCm = null;
        while (true)
        {
            string rawHeight = ConsoleHelper.ReadOptionalString(
                "Wzrost najniższego uczestnika w cm (np. 135, Enter = pomiń): ");
            if (string.IsNullOrWhiteSpace(rawHeight))
            {
                break;
            }

            if (decimal.TryParse(rawHeight.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal heightCm) &&
                heightCm >= 40 && heightCm <= 260)
            {
                minParticipantHeightCm = heightCm;
                break;
            }

            ConsoleHelper.PrintError("Podaj poprawny wzrost w centymetrach (40-260) lub naciśnij Enter.");
        }

        bool requiresWheelchairAccess = ConsoleHelper.ReadConfirmation(
            "Czy wymagany jest dostęp dla wózka inwalidzkiego?",
            defaultYes: false);

        int defaultDisabled = requiresWheelchairAccess ? 1 : 0;
        int disabledParticipantsCount = ConsoleHelper.ReadIntInRangeOrDefault(
            $"Liczba osób z orzeczeniem o niepełnosprawności uprawnionych do ulgi -50% (0-{participantAges.Count})",
            0,
            participantAges.Count,
            defaultValue: defaultDisabled);

        var physicalFeature = allFeatures.FirstOrDefault(f => f.ProfileBindingKey == "physical_demand");
        var physicalOptions = physicalFeature?.AllowedValues is { Count: > 0 }
            ? physicalFeature.AllowedValues
            : new List<string> { "Niski", "Średni", "Wysoki" };

        Console.WriteLine("Maksymalny akceptowalny wysiłek fizyczny na trasie:");
        for (int i = 0; i < physicalOptions.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {physicalOptions[i]}");
        }
        Console.WriteLine("  0 / Enter. Bez ograniczeń");

        string? maxPhysicalDemand = null;
        while (true)
        {
            string effortChoice = ConsoleHelper.ReadMenuChoice($"Wybierz poziom (0-{physicalOptions.Count}): ");
            if (string.IsNullOrEmpty(effortChoice) || effortChoice == "0")
            {
                break;
            }

            if (int.TryParse(effortChoice, out int idx) && idx >= 1 && idx <= physicalOptions.Count)
            {
                maxPhysicalDemand = physicalOptions[idx - 1];
                break;
            }

            ConsoleHelper.PrintError($"Wybierz liczbę od 0 do {physicalOptions.Count} lub naciśnij Enter.");
        }

        bool hasMinor = participantAges.Any(a => a < 18);
        bool hasAdult = participantAges.Any(a => a >= 18);
        bool? explicitAdultGuardianPresent = null;

        if (hasMinor && !hasAdult)
        {
            explicitAdultGuardianPresent = ConsoleHelper.ReadConfirmation(
                "Wszyscy wskazani uczestnicy są niepełnoletni (<18 lat). Czy towarzyszy im pełnoletni opiekun?",
                defaultYes: false);
        }

        bool requiresChildFriendly = ConsoleHelper.ReadConfirmation(
            "Czy wymagasz ofert przyjaznych dla dzieci?",
            defaultYes: participantAges.Min() < 10);

        bool requiresStrollerAccess = ConsoleHelper.ReadConfirmation(
            "Czy wymagasz możliwości wejścia z wózkiem dziecięcym?",
            defaultYes: false);

        bool requiresElevator = ConsoleHelper.ReadConfirmation(
            "Czy wymagasz dostępności windy?",
            defaultYes: false);

        var requiredFacilities = new HashSet<int>();
        var confirmedConditions = new HashSet<int>();
        var customNumbers = new Dictionary<int, List<decimal>>();
        var customTexts = new Dictionary<int, string>();

        var dynamicExtraFeatures = allFeatures
            .Where(f => string.IsNullOrEmpty(f.ProfileBindingKey))
            .OrderBy(f => f.Id)
            .ToList();

        if (dynamicExtraFeatures.Count > 0 &&
            ConsoleHelper.ReadConfirmation(
                $"W słowniku Features.json znajdują się dodatkowe kryteria/cechy ({dynamicExtraFeatures.Count}, np. \"{dynamicExtraFeatures[0].Name}\"). Czy chcesz określić dla nich preferencje/dane?",
                defaultYes: false))
        {
            CollectDynamicFeaturesForProfile(
                dynamicExtraFeatures,
                requiredFacilities,
                confirmedConditions,
                customNumbers,
                customTexts);
        }

        return new ParticipantProfile(
            participantAges: participantAges,
            minParticipantHeightCm: minParticipantHeightCm,
            requiresWheelchairAccess: requiresWheelchairAccess,
            disabledParticipantsCount: disabledParticipantsCount,
            maxPhysicalDemand: maxPhysicalDemand,
            explicitAdultGuardianPresent: explicitAdultGuardianPresent,
            requiresChildFriendly: requiresChildFriendly,
            requiresStrollerAccess: requiresStrollerAccess,
            requiresElevator: requiresElevator,
            requiredFacilityFeatureIds: requiredFacilities,
            confirmedConditionFeatureIds: confirmedConditions,
            customNumericValues: customNumbers,
            customOptionOrTextValues: customTexts);
    }

    private static void CollectDynamicFeaturesForProfile(
        IReadOnlyCollection<FeatureDefinition> extraFeatures,
        HashSet<int> requiredFacilities,
        HashSet<int> confirmedConditions,
        Dictionary<int, List<decimal>> customNumbers,
        Dictionary<int, string> customTexts)
    {
        foreach (var feature in extraFeatures)
        {
            Console.WriteLine();
            Console.WriteLine($"Cecha słownikowa [ID: {feature.Id}]: {feature.Name} ({ConsoleHelper.FormatFeatureType(feature.Type)})");
            if (!string.IsNullOrWhiteSpace(feature.Description))
            {
                Console.WriteLine($"  Opis: {feature.Description}");
            }

            switch (feature.EffectiveRule)
            {
                case FeatureComparisonRule.FacilityAvailable:
                    if (ConsoleHelper.ReadConfirmation($"  Czy wymagasz udogodnienia \"{feature.Name}\"?", defaultYes: false))
                    {
                        requiredFacilities.Add(feature.Id);
                    }
                    break;

                case FeatureComparisonRule.ConditionRequired:
                    if (ConsoleHelper.ReadConfirmation($"  Czy uczestnik/grupa spełnia warunek \"{feature.Name}\"?", defaultYes: true))
                    {
                        confirmedConditions.Add(feature.Id);
                    }
                    break;

                case FeatureComparisonRule.MinThreshold:
                case FeatureComparisonRule.MaxThreshold:
                {
                    string unitHint = string.IsNullOrWhiteSpace(feature.Unit) ? string.Empty : $" ({feature.Unit})";
                    while (true)
                    {
                        string raw = ConsoleHelper.ReadOptionalString(
                            $"  Podaj wartość uczestnika (lub wartości grupy po przecinku){unitHint} [Enter = pomiń]: ");
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            break;
                        }

                        var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var nums = new List<decimal>();
                        bool ok = parts.Length > 0;
                        foreach (string p in parts)
                        {
                            if (!DomainValueHelper.TryParseDecimalInvariant(p, out decimal val))
                            {
                                ok = false;
                                break;
                            }
                            nums.Add(val);
                        }

                        if (ok)
                        {
                            customNumbers[feature.Id] = nums;
                            break;
                        }

                        ConsoleHelper.PrintError("Wprowadź poprawne liczby (np. 75 lub 65, 92) lub naciśnij Enter.");
                    }
                    break;
                }

                case FeatureComparisonRule.MaxLevelIndex:
                case FeatureComparisonRule.ExactOrContainsMatch:
                {
                    if (feature.Type == FeatureValueType.OptionList && feature.AllowedValues is { Count: > 0 })
                    {
                        for (int i = 0; i < feature.AllowedValues.Count; i++)
                        {
                            Console.WriteLine($"    {i + 1}. {feature.AllowedValues[i]}");
                        }

                        string raw = ConsoleHelper.ReadOptionalString(
                            $"  Wybierz maksymalny akceptowalny / oczekiwany wariant (1-{feature.AllowedValues.Count}, Enter = pomiń): ");
                        if (!string.IsNullOrWhiteSpace(raw) &&
                            feature.TryNormalizeValue(raw, out string norm, out _))
                        {
                            customTexts[feature.Id] = norm;
                        }
                    }
                    else
                    {
                        string rawText = ConsoleHelper.ReadOptionalString(
                            $"  Podaj oczekiwaną wartość / frazę dla \"{feature.Name}\" (Enter = dowolna): ");
                        string cleanedText = DomainValueHelper.CleanText(rawText);
                        if (cleanedText.Length > 0)
                        {
                            customTexts[feature.Id] = cleanedText;
                        }
                    }
                    break;
                }
            }
        }
    }

    private bool EnsureParticipantProfileConfigured()
    {
        if (_participantProfile is not null)
        {
            return true;
        }

        ConsoleHelper.PrintInfo("Nie skonfigurowano jeszcze profilu uczestnika w tej sesji.");
        if (!ConsoleHelper.ReadConfirmation("Czy chcesz teraz utworzyć profil uczestnika?", defaultYes: true))
        {
            return false;
        }

        _participantProfile = BuildParticipantProfileInteractive(_featureService.GetAllFeatures());
        Console.WriteLine();
        ConsoleHelper.PrintSuccess($"Utworzono profil: {_participantProfile.GetShortSummary(_featureService.GetAllFeatures())}");
        return true;
    }

    private void FilterOffersByProfile()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("FILTROWANIE OFERT WEDŁUG PROFILU UCZESTNIKA");

        if (!EnsureParticipantProfileConfigured() || _participantProfile is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        var evaluated = _offerService.EvaluateAvailableOffersForProfile(_participantProfile);
        var compatibleOffers = evaluated.Where(x => x.Report.IsCompatible).Select(x => x.Offer).ToList();
        var rejectedOffers = evaluated.Where(x => !x.Report.IsCompatible).ToList();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Profil: {_participantProfile.GetShortSummary(_featureService.GetAllFeatures())}");
        Console.ResetColor();
        Console.WriteLine($"Dopasowane oferty: {compatibleOffers.Count} z {evaluated.Count} dostępnych (odrzucone przez wymagania: {rejectedOffers.Count}).");
        Console.WriteLine();

        ConsoleHelper.DisplayOffersList(compatibleOffers, _attractionService.GetAttractionById, _participantProfile);

        if (rejectedOffers.Count > 0 &&
            ConsoleHelper.ReadConfirmation(
                $"Czy chcesz wyświetlić powody odrzucenia pozostałych ofert ({rejectedOffers.Count})?",
                defaultYes: false))
        {
            Console.WriteLine();
            ConsoleHelper.PrintSectionHeader("OFERTY NIESPEŁNIAJĄCE WYMAGAŃ PROFILU");
            foreach (var (offer, report) in rejectedOffers)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ID: {offer.Id}] {offer.Name} ({offer.Price:F2} zł):");
                Console.ResetColor();
                foreach (string reason in report.IncompatibilityReasons)
                {
                    Console.WriteLine($"   - {reason}");
                }
            }
        }

        ConsoleHelper.Pause();
    }

    private void ShowGroupOffersMenu()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("OFERTY GRUPOWE (NP. TYLKO DLA GRUP 4-OSOBOWYCH)");

        Console.WriteLine("Wybierz zakres przeglądania ofert grupowych:");
        Console.WriteLine("  1. Wszystkie dostępne oferty tylko dla grup [Domyślnie - Enter]");
        Console.WriteLine("  2. Oferty przeznaczone ściśle dla grup 4-osobowych (TYLKO 4 os.)");
        Console.WriteLine("  3. Oferty grupowe pasujące do wybranej liczby osób");
        Console.WriteLine("  0. Powrót");

        string subChoice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");
        if (subChoice == "0")
        {
            return;
        }

        List<Offer> groupOffers;
        if (subChoice == "2")
        {
            groupOffers = _offerService
                .GetGroupOnlyOffers(onlyAvailableForUser: true)
                .Where(o => o.MinParticipants == 4 && o.IsExactGroupSizeOffer(_attractionService.GetAttractionById(o.AttractionId)))
                .ToList();
        }
        else if (subChoice == "3")
        {
            int targetSize = ConsoleHelper.ReadIntInRangeOrDefault("Podaj liczebność grupy (2-60)", 2, 60, defaultValue: 4);
            groupOffers = _offerService.GetGroupOnlyOffers(onlyAvailableForUser: true, groupSizeFilter: targetSize);
        }
        else
        {
            groupOffers = _offerService.GetGroupOnlyOffers(onlyAvailableForUser: true);
        }

        Console.WriteLine();
        ConsoleHelper.DisplayOffersList(groupOffers, _attractionService.GetAttractionById, _participantProfile);
        if (groupOffers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty grupowej, aby zobaczyć szczegóły i symulację zniżek (0 = powrót): ");
        if (offerId == 0)
        {
            return;
        }

        var selected = groupOffers.FirstOrDefault(o => o.Id == offerId);
        if (selected is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {offerId} nie znajduje się na powyższej liście ofert grupowych.");
            ConsoleHelper.Pause();
            return;
        }

        var attr = _attractionService.GetAttractionById(selected.AttractionId);
        var rel = _offerService.GetRelatedOffers(selected.Id, onlyAvailableForUser: true);

        ConsoleHelper.ClearScreen();
        ConsoleHelper.DisplayOfferDetails(selected, attr, rel, _participantProfile);

        int remainingSpots = _orderService.GetRemainingSpotsForOffer(selected);
        ConsoleHelper.PrintInfo(
            $"Dostępne wolne miejsca dla tej oferty: {remainingSpots} / {selected.GetEffectiveMaxParticipants(attr)} os.");

        if (ConsoleHelper.ReadConfirmation(
                "Czy chcesz wybrać tę ofertę grupową i przejść do konfiguracji oraz zakupu ze zniżkami?",
                defaultYes: true))
        {
            var cfgRes = _offerExtensionService.CreateConfiguration(selected.Id);
            if (cfgRes.IsSuccess && cfgRes.Data is not null)
            {
                _activeConfiguration = cfgRes.Data;
                RunBaseAddonsStep(_activeConfiguration, isWizardStep: true);
                RunRelatedOffersStep(_activeConfiguration, isWizardStep: true);
                RunCheckoutStep(_activeConfiguration);
                return;
            }
        }

        ConsoleHelper.Pause();
    }

    private void RunCompatibilityDiagnosticMenu()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("WERYFIKACJA KOMPATYBILNOŚCI Z PROFILEM UCZESTNIKA");

        if (!EnsureParticipantProfileConfigured() || _participantProfile is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Co chcesz zweryfikować względem swojego profilu?");
        Console.WriteLine("  1. Konkretną ofertę");
        Console.WriteLine("  2. Ogólną dostępność atrakcji turystycznej");
        Console.WriteLine("  3. Aktualnie skonfigurowany pakiet w koszyku");
        Console.WriteLine("  0. Powrót");

        string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");
        switch (choice)
        {
            case "1":
            {
                var offer = SelectAvailableOffer("WYBÓR OFERTY DO WERYFIKACJI KOMPATYBILNOŚCI");
                if (offer is not null)
                {
                    Console.WriteLine();
                    var report = offer.ValidateAgainst(_participantProfile);
                    ConsoleHelper.DisplayCompatibilityReport(report, includeMetRequirements: true);
                }
                ConsoleHelper.Pause();
                break;
            }

            case "2":
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.PrintSectionHeader("WERYFIKACJA OGÓLNEJ DOSTĘPNOŚCI ATRAKCJI");
                var attractions = _attractionService.GetAllAttractions();
                ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);
                int attrId = ConsoleHelper.ReadInt("Podaj ID atrakcji: ");
                var attraction = _attractionService.GetAttractionById(attrId);
                if (attraction is null)
                {
                    ConsoleHelper.PrintError($"Atrakcja o ID {attrId} nie istnieje.");
                }
                else
                {
                    Console.WriteLine();
                    var report = attraction.ValidateAgainst(_participantProfile);
                    ConsoleHelper.DisplayCompatibilityReport(report, includeMetRequirements: true);
                }
                ConsoleHelper.Pause();
                break;
            }

            case "3":
            {
                Console.WriteLine();
                if (_activeConfiguration is null)
                {
                    ConsoleHelper.PrintInfo("Nie masz obecnie aktywnej konfiguracji pakietu w koszyku.");
                }
                else
                {
                    var report = _activeConfiguration.ValidateAgainst(_participantProfile);
                    ConsoleHelper.DisplayCompatibilityReport(report, includeMetRequirements: true);
                }
                ConsoleHelper.Pause();
                break;
            }
        }
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
        ConsoleHelper.DisplayOfferDetails(selectedConfig.BaseOffer, attraction, relatedOffers, _participantProfile);

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
                ConsoleHelper.DisplayAttractionDetails(selectedAttraction, offersForAttraction, _participantProfile);
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

                if (_participantProfile is not null)
                {
                    var offerCheck = chosenOffer.ValidateAgainst(_participantProfile, selectedAttraction);
                    if (!offerCheck.IsCompatible)
                    {
                        Console.WriteLine();
                        ConsoleHelper.PrintWarning("Wybrana oferta nie spełnia wymagań Twojego profilu uczestnika (chyba że dobierzesz dodatek zapewniający udogodnienie):");
                        ConsoleHelper.DisplayCompatibilityReport(offerCheck, includeMetRequirements: false);
                        if (!ConsoleHelper.ReadConfirmation("Czy mimo to chcesz rozpocząć konfigurację tej oferty?", defaultYes: false))
                        {
                            continue;
                        }
                    }
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
        ConsoleHelper.DisplayOffersList(availableOffers, _attractionService.GetAttractionById, _participantProfile);
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
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById, _participantProfile);
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
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById, _participantProfile);
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

        var defById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
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
                ConsoleHelper.PrintAddonModifiersIfAny(addon, defById);
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

            bool isCurrentlySelected = config.SelectedAddons.Any(a => a.Id == addonId);
            if (!isCurrentlySelected && _participantProfile is not null && config.BaseOffer.Addons.Any(a => a.Id == addonId))
            {
                var (_, newIssues, resolvedPrevious) =
                    _offerExtensionService.PreviewBaseAddonImpact(config, addonId, _participantProfile);

                if (newIssues.Count > 0)
                {
                    Console.WriteLine();
                    ConsoleHelper.PrintWarning("Wybrany dodatek wprowadza wymagania niezgodne z Twoim profilem uczestnika:");
                    foreach (var issue in newIssues)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  - {issue.FormattedMessage}");
                        Console.ResetColor();
                    }

                    if (!ConsoleHelper.ReadConfirmation(
                            "Czy mimo to chcesz dodać ten dodatek do roboczej konfiguracji (finalizacja zamówienia będzie zablokowana)?",
                            defaultYes: false))
                    {
                        lastFeedback = "Pominięto dodanie niekompatybilnego dodatku.";
                        lastFeedbackSuccess = true;
                        continue;
                    }
                }
                else if (resolvedPrevious)
                {
                    var toggleOk = _offerExtensionService.ToggleBaseAddon(config, addonId);
                    lastFeedback = $"{toggleOk.Message} [Udogodnienie dopasowało ofertę do Twojego profilu!]";
                    lastFeedbackSuccess = toggleOk.IsSuccess;
                    continue;
                }
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
                var relAttraction = _attractionService.GetAttractionById(relatedOffer.AttractionId);
                string attractionName = relAttraction?.Name ?? "(nieznana)";

                string compatBadge = string.Empty;
                if (_participantProfile is not null)
                {
                    bool ok = relatedOffer.ValidateAgainst(_participantProfile, relAttraction).IsCompatible;
                    compatBadge = ok ? " | [ZGODNA]" : " | [NIEZGODNA (!)]";
                }

                Console.WriteLine(
                    $"{mark} ID: {relatedOffer.Id,-3} | {relatedOffer.Name,-38} | {attractionName,-24} | {relatedOffer.Date:yyyy-MM-dd} | {relatedOffer.Price,8:F2} zł{compatBadge}");

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
                if (_participantProfile is not null && availableRelated.Any(r => r.Id == relatedOfferId))
                {
                    var (_, newIssues) =
                        _offerExtensionService.PreviewRelatedOfferImpact(config, relatedOfferId, _participantProfile);

                    if (newIssues.Count > 0)
                    {
                        Console.WriteLine();
                        ConsoleHelper.PrintWarning("Dołączenie tej oferty powiązanej spowoduje brak kompatybilności pakietu z Twoim profilem:");
                        foreach (var issue in newIssues)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  - {issue.FormattedMessage}");
                            Console.ResetColor();
                        }

                        if (!ConsoleHelper.ReadConfirmation(
                                "Czy mimo to chcesz dołączyć tę ofertę powiązaną (np. aby dobrać do niej dodatek z udogodnieniem)?",
                                defaultYes: false))
                        {
                            lastFeedback = "Pominięto niekompatybilną ofertę powiązaną.";
                            lastFeedbackSuccess = true;
                            continue;
                        }
                    }
                }

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
        ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById, _participantProfile);

        var (pkgMin, pkgMax, minSrc, maxSrc) = config.GetPackageGroupSizeBounds(_attractionService.GetAttractionById);
        if (pkgMin > pkgMax)
        {
            Console.WriteLine();
            ConsoleHelper.PrintError(
                $"Nie można złożyć zamówienia – wybrane w pakiecie oferty mają sprzeczne wymagania wielkości grupy ({minSrc}: min. {pkgMin} os. vs {maxSrc}: maks. {pkgMax} os.).");
            ConsoleHelper.Pause();
            return;
        }

        var (minRemainingSpots, limitingOfferName) = _orderService.GetPackageRemainingSpots(config);
        if (minRemainingSpots < pkgMin)
        {
            Console.WriteLine();
            ConsoleHelper.PrintError(
                $"Brak wystarczającej liczby wolnych miejsc do realizacji pakietu (wymagane min. {pkgMin} os., pozostało wolnych miejsc: {minRemainingSpots} – limit w: \"{limitingOfferName}\").");
            ConsoleHelper.Pause();
            return;
        }

        if (_participantProfile is null)
        {
            Console.WriteLine();
            if (ConsoleHelper.ReadConfirmation(
                    "Czy chcesz określić pełny profil uczestników (wiek, dostępność) przed podziałem biletów?",
                    defaultYes: false))
            {
                _participantProfile = BuildParticipantProfileInteractive(_featureService.GetAllFeatures());
                ConsoleHelper.ClearScreen();
                ConsoleHelper.PrintSectionHeader("KROK 5/5 – PODSUMOWANIE I FINALIZACJA ZAMÓWIENIA");
                ConsoleHelper.DisplayConfigurationSummary(config, _attractionService.GetAttractionById, _participantProfile);
            }
        }

        if (_participantProfile is not null)
        {
            var packageReport = config.ValidateAgainst(_participantProfile);
            if (!packageReport.IsCompatible)
            {
                Console.WriteLine();
                ConsoleHelper.PrintError(
                    "Złożenie zamówienia jest ZABLOKOWANE – skonfigurowany pakiet nie spełnia wymagań profilu / wielkości grupy.");
                ConsoleHelper.PrintInfo(
                    "Usuń niekompatybilny element pakietu (opcja 4 / 6), dobierz odpowiednie udogodnienie lub dostosuj skład grupy w profilu (opcja 10).");
                ConsoleHelper.Pause();
                return;
            }
        }

        Console.WriteLine();
        ConsoleHelper.PrintInfo(
            $"Dostępna pula miejsc dla pakietu: maks. {minRemainingSpots} os. (wymagana wielkość grupy: {(pkgMin == pkgMax ? $"dokładnie {pkgMin} os." : $"{pkgMin}–{minRemainingSpots} os.")}).");

        var groupBreakdown = ConsoleHelper.PromptGroupBreakdownInteractive(
            minAllowed: pkgMin,
            maxAllowed: Math.Min(100, minRemainingSpots),
            activeProfile: _participantProfile);

        var priceBreakdown = config.CalculatePriceBreakdown(groupBreakdown);
        Console.WriteLine();
        ConsoleHelper.DisplayPackagePriceBreakdown(priceBreakdown);
        Console.WriteLine();

        string customerName = ConsoleHelper.ReadRequiredString("Imię i nazwisko zamawiającego / opiekuna grupy: ", "Imię i nazwisko");
        string customerEmail = ConsoleHelper.ReadOptionalEmail("Adres e-mail do potwierdzenia (opcjonalnie, Enter = pomiń): ");
        Console.WriteLine();
        PaymentMethod paymentMethod = ConsoleHelper.ReadPaymentMethod();

        Console.WriteLine();
        bool confirmed = ConsoleHelper.ReadConfirmation(
            $"Czy potwierdzasz złożenie zamówienia dla {groupBreakdown.TotalCount} os. na kwotę {priceBreakdown.FinalTotalPrice:F2} zł ({ConsoleHelper.FormatPaymentMethod(paymentMethod)})?",
            defaultYes: true);

        if (!confirmed)
        {
            ConsoleHelper.PrintInfo("Złożenie zamówienia zostało wstrzymane – Twoja konfiguracja nadal pozostaje zapisana w sesji.");
            ConsoleHelper.Pause();
            return;
        }

        var orderResult = _orderService.PlaceOrder(
            config,
            customerName,
            customerEmail,
            groupBreakdown,
            paymentMethod,
            _participantProfile);

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
        var defById = _featureService.GetAllFeatures().ToDictionary(f => f.Id);
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
                ConsoleHelper.PrintAddonModifiersIfAny(addon, defById);
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

            bool alreadySelected = relatedSelection.SelectedAddons.Any(a => a.Id == addonId);
            if (!alreadySelected && _participantProfile is not null && relatedSelection.Offer.Addons.Any(a => a.Id == addonId))
            {
                var (_, newIssues, resolvedPrevious) = _offerExtensionService.PreviewRelatedOfferAddonImpact(
                    config,
                    relatedSelection.Offer.Id,
                    addonId,
                    _participantProfile);

                if (newIssues.Count > 0)
                {
                    Console.WriteLine();
                    ConsoleHelper.PrintWarning("Ten dodatek oferty powiązanej wprowadza niezgodność z Twoim profilem uczestnika:");
                    foreach (var issue in newIssues)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  - {issue.FormattedMessage}");
                        Console.ResetColor();
                    }

                    if (!ConsoleHelper.ReadConfirmation("Czy mimo to chcesz go zaznaczyć?", defaultYes: false))
                    {
                        feedback = "Pominięto niekompatybilny dodatek.";
                        feedbackSuccess = true;
                        continue;
                    }
                }
                else if (resolvedPrevious)
                {
                    var toggleAcc = _offerExtensionService.ToggleRelatedOfferAddon(config, relatedSelection.Offer.Id, addonId);
                    feedback = $"{toggleAcc.Message} [Udogodnienie dopasowało ofertę powiązaną do Twojego profilu!]";
                    feedbackSuccess = toggleAcc.IsSuccess;
                    continue;
                }
            }

            var toggleResult = _offerExtensionService.ToggleRelatedOfferAddon(config, relatedSelection.Offer.Id, addonId);
            feedback = toggleResult.Message;
            feedbackSuccess = toggleResult.IsSuccess;
        }
    }
}
