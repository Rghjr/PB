using Attractions.Data;
using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class FeatureMenu
{
    private readonly FeatureService _featureService;

    public FeatureMenu(FeatureService featureService)
    {
        _featureService = featureService ?? throw new ArgumentNullException(nameof(featureService));
    }

    public void Show()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("==========================================");
            Console.WriteLine("   SŁOWNIK CECH I WYMAGAŃ (Features.json)");
            Console.WriteLine("==========================================");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Plik słownika: {JsonFeatureStore.GetFeaturesFilePath()}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("1. Wyświetl wszystkie cechy w słowniku");
            Console.WriteLine("2. Dodaj nową cechę do słownika JSON");
            Console.WriteLine("3. Edytuj istniejącą cechę");
            Console.WriteLine("4. Usuń cechę ze słownika");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowAllFeatures();
                    break;
                case "2":
                    AddNewFeature();
                    break;
                case "3":
                    EditExistingFeature();
                    break;
                case "4":
                    DeleteFeature();
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

    private void ShowAllFeatures()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("SŁOWNIK CECH (Features.json)");
        var features = _featureService.GetAllFeatures();
        ConsoleHelper.DisplayFeaturesDictionary(features, id => _featureService.GetFeatureUsage(id).TotalCount);
        ConsoleHelper.Pause();
    }

    private void AddNewFeature()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DODAWANIE NOWEJ CECHY DO SŁOWNIKA JSON");

        string name = ConsoleHelper.ReadRequiredString("Nazwa nowej cechy: ", "Nazwa cechy");
        string description = ConsoleHelper.ReadOptionalString("Opis cechy (opcjonalnie): ");

        Console.WriteLine();
        Console.WriteLine("Wybierz typ wartości cechy:");
        Console.WriteLine("  1. Boolean (Tak / Nie)");
        Console.WriteLine("  2. Number (Wartość liczbowa, np. wiek, wzrost, waga)");
        Console.WriteLine("  3. OptionList (Lista dozwolonych wartości, np. Niski, Średni, Wysoki)");
        Console.WriteLine("  4. Text (Wartość tekstowa, np. język, wyposażenie)");

        FeatureValueType selectedType;
        while (true)
        {
            string typeChoice = ConsoleHelper.ReadMenuChoice("Typ cechy (1-4): ");
            if (typeChoice == "1")
            {
                selectedType = FeatureValueType.Boolean;
                break;
            }

            if (typeChoice == "2")
            {
                selectedType = FeatureValueType.Number;
                break;
            }

            if (typeChoice == "3")
            {
                selectedType = FeatureValueType.OptionList;
                break;
            }

            if (typeChoice == "4")
            {
                selectedType = FeatureValueType.Text;
                break;
            }

            ConsoleHelper.PrintError("Wybierz 1, 2, 3 lub 4.");
        }

        string? unit = null;
        List<string>? allowedValues = null;

        if (selectedType == FeatureValueType.Number)
        {
            unit = ConsoleHelper.ReadOptionalString("Jednostka (np. lat, cm, kg – Enter = brak): ");
        }
        else if (selectedType == FeatureValueType.OptionList)
        {
            while (true)
            {
                string rawOptions = ConsoleHelper.ReadRequiredString(
                    "Podaj dozwolone wartości oddzielone przecinkiem (od najmniej do najbardziej wymagającej, np. Niski, Średni, Wysoki): ",
                    "Lista wartości");

                allowedValues = rawOptions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (allowedValues.Count >= 2)
                {
                    break;
                }

                ConsoleHelper.PrintError("Podaj co najmniej dwie różne wartości oddzielone przecinkiem.");
            }
        }

        FeatureComparisonRule rule = PromptComparisonRuleForType(selectedType, currentRule: null);

        var result = _featureService.AddFeature(name, selectedType, description, rule, unit, allowedValues);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        if (result.IsSuccess)
        {
            ConsoleHelper.PrintInfo("Nowa cecha jest od razu dostępna przy tworzeniu/edycji atrakcji, ofert, dodatków oraz w profilu uczestnika.");
        }

        ConsoleHelper.Pause();
    }

    private void EditExistingFeature()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("EDYCJA CECHY W SŁOWNIKU JSON");

        var features = _featureService.GetAllFeatures();
        ConsoleHelper.DisplayFeaturesDictionary(features, id => _featureService.GetFeatureUsage(id).TotalCount);
        if (features.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int id = ConsoleHelper.ReadInt("Podaj ID cechy do edycji (0 = anuluj): ");
        if (id == 0)
        {
            return;
        }

        var existing = _featureService.GetFeatureById(id);
        if (existing is null)
        {
            ConsoleHelper.PrintError($"Cecha o ID {id} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.PrintInfo($"Edytujesz cechę ID {existing.Id} (Typ: {ConsoleHelper.FormatFeatureType(existing.Type)}). Naciśnij Enter, aby zachować obecną wartość.");

        string newName = ConsoleHelper.ReadStringOrKeepCurrent("Nazwa cechy", existing.Name, required: true, "Nazwa cechy");
        string newDesc = ConsoleHelper.ReadStringOrKeepCurrent(
            "Opis cechy",
            existing.Description,
            required: false,
            "Opis cechy",
            allowClearWithDash: true);

        string? newUnit = existing.Unit;
        if (existing.Type == FeatureValueType.Number)
        {
            newUnit = ConsoleHelper.ReadStringOrKeepCurrent(
                "Jednostka",
                existing.Unit ?? string.Empty,
                required: false,
                "Jednostka",
                allowClearWithDash: true);
        }

        List<string>? newAllowedValues = existing.AllowedValues;
        if (existing.Type == FeatureValueType.OptionList)
        {
            string currentJoined = existing.AllowedValues is { Count: > 0 }
                ? string.Join(", ", existing.AllowedValues)
                : string.Empty;

            while (true)
            {
                string raw = ConsoleHelper.ReadStringOrKeepCurrent(
                    "Dozwolone wartości (po przecinku)",
                    currentJoined,
                    required: true,
                    "Dozwolone wartości");

                var parsed = raw
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (parsed.Count >= 2)
                {
                    newAllowedValues = parsed;
                    break;
                }

                ConsoleHelper.PrintError("Lista musi zawierać co najmniej 2 wartości oddzielone przecinkiem.");
            }
        }

        FeatureComparisonRule newRule = PromptComparisonRuleForType(existing.Type, existing.EffectiveRule);

        var updateResult = _featureService.UpdateFeature(id, newName, newDesc, newRule, newUnit, newAllowedValues);
        Console.WriteLine();
        ConsoleHelper.PrintResult(updateResult);
        ConsoleHelper.Pause();
    }

    private void DeleteFeature()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("USUWANIE CECHY ZE SŁOWNIKA JSON");

        var features = _featureService.GetAllFeatures();
        ConsoleHelper.DisplayFeaturesDictionary(features, id => _featureService.GetFeatureUsage(id).TotalCount);
        if (features.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int id = ConsoleHelper.ReadInt("Podaj ID cechy do usunięcia (0 = anuluj): ");
        if (id == 0)
        {
            return;
        }

        var existing = _featureService.GetFeatureById(id);
        if (existing is null)
        {
            ConsoleHelper.PrintError($"Cecha o ID {id} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        var usage = _featureService.GetFeatureUsage(id);
        if (!usage.IsUsed)
        {
            bool confirmSimple = ConsoleHelper.ReadConfirmation(
                $"Czy na pewno chcesz usunąć nieużywaną cechę \"{existing.Name}\" (ID: {id}) ze słownika Features.json?");

            if (!confirmSimple)
            {
                ConsoleHelper.PrintInfo("Usuwanie cechy zostało anulowane.");
                ConsoleHelper.Pause();
                return;
            }

            var simpleDelete = _featureService.DeleteFeature(id, removeFromAssignedObjects: false);
            Console.WriteLine();
            ConsoleHelper.PrintResult(simpleDelete);
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.PrintWarning(
            $"Cecha \"{existing.Name}\" jest aktualnie przypisana do {usage.TotalCount} obiektów " +
            $"(Atrakcje: {usage.Attractions.Count}, Oferty: {usage.Offers.Count}, Dodatki: {usage.Addons.Count})!");

        if (usage.Attractions.Count > 0)
        {
            Console.WriteLine($"  Atrakcje: {string.Join(", ", usage.Attractions.Take(5).Select(a => $"#{a.Id} {a.Name}"))}" +
                              (usage.Attractions.Count > 5 ? $" ... (+{usage.Attractions.Count - 5})" : string.Empty));
        }

        if (usage.Offers.Count > 0)
        {
            Console.WriteLine($"  Oferty  : {string.Join(", ", usage.Offers.Take(5).Select(o => $"#{o.Id} {o.Name}"))}" +
                              (usage.Offers.Count > 5 ? $" ... (+{usage.Offers.Count - 5})" : string.Empty));
        }

        if (usage.Addons.Count > 0)
        {
            Console.WriteLine($"  Dodatki : {string.Join(", ", usage.Addons.Take(5).Select(x => $"#{x.Addon.Id} {x.Addon.Name}"))}" +
                              (usage.Addons.Count > 5 ? $" ... (+{usage.Addons.Count - 5})" : string.Empty));
        }

        Console.WriteLine();
        bool forceConfirm = ConsoleHelper.ReadConfirmation(
            "Czy mimo to chcesz usunąć tę cechę ze słownika ORAZ odpiąć ją ze wszystkich powyższych obiektów?",
            defaultYes: false);

        if (!forceConfirm)
        {
            ConsoleHelper.PrintInfo("Operacja usunięcia cechy została bezpiecznie anulowana.");
            ConsoleHelper.Pause();
            return;
        }

        var cascadeDelete = _featureService.DeleteFeature(id, removeFromAssignedObjects: true);
        Console.WriteLine();
        ConsoleHelper.PrintResult(cascadeDelete);
        ConsoleHelper.Pause();
    }

    private static FeatureComparisonRule PromptComparisonRuleForType(
        FeatureValueType type,
        FeatureComparisonRule? currentRule)
    {
        Console.WriteLine();
        switch (type)
        {
            case FeatureValueType.Boolean:
            {
                var def = currentRule ?? FeatureComparisonRule.FacilityAvailable;
                Console.WriteLine($"Sposób weryfikacji cechy logicznej {(currentRule.HasValue ? $"[aktualnie: {ConsoleHelper.FormatComparisonRule(def)}]" : string.Empty)}:");
                Console.WriteLine("  1. Udogodnienie / dostępność obiektu (np. dostępność dla wózków, winda, przyjazne dzieciom)");
                Console.WriteLine("  2. Warunek wymagany od uczestnika / grupy (np. wymagana obecność opiekuna, wymagane obuwie)");
                while (true)
                {
                    string c = ConsoleHelper.ReadMenuChoice("Wybierz regułę (1-2, Enter = domyślna/obecna): ");
                    if (string.IsNullOrEmpty(c)) return def;
                    if (c == "1") return FeatureComparisonRule.FacilityAvailable;
                    if (c == "2") return FeatureComparisonRule.ConditionRequired;
                    ConsoleHelper.PrintError("Wybierz 1 lub 2.");
                }
            }

            case FeatureValueType.Number:
            {
                var def = currentRule ?? FeatureComparisonRule.MinThreshold;
                Console.WriteLine($"Sposób weryfikacji cechy liczbowej {(currentRule.HasValue ? $"[aktualnie: {ConsoleHelper.FormatComparisonRule(def)}]" : string.Empty)}:");
                Console.WriteLine("  1. Próg minimalny – uczestnik musi mieć >= wartość cechy (np. minimalny wiek, minimalny wzrost)");
                Console.WriteLine("  2. Próg maksymalny – uczestnik musi mieć <= wartość cechy (np. maksymalny wiek, maksymalna waga)");
                while (true)
                {
                    string c = ConsoleHelper.ReadMenuChoice("Wybierz regułę (1-2, Enter = domyślna/obecna): ");
                    if (string.IsNullOrEmpty(c)) return def;
                    if (c == "1") return FeatureComparisonRule.MinThreshold;
                    if (c == "2") return FeatureComparisonRule.MaxThreshold;
                    ConsoleHelper.PrintError("Wybierz 1 lub 2.");
                }
            }

            case FeatureValueType.OptionList:
            {
                var def = currentRule ?? FeatureComparisonRule.MaxLevelIndex;
                Console.WriteLine($"Sposób weryfikacji listy wartości {(currentRule.HasValue ? $"[aktualnie: {ConsoleHelper.FormatComparisonRule(def)}]" : string.Empty)}:");
                Console.WriteLine("  1. Uporządkowane poziomy trudności/intensywności (uczestnik określa maksymalny akceptowalny poziom)");
                Console.WriteLine("  2. Dopasowanie wybranej opcji (np. kategoria lub wariant)");
                while (true)
                {
                    string c = ConsoleHelper.ReadMenuChoice("Wybierz regułę (1-2, Enter = domyślna/obecna): ");
                    if (string.IsNullOrEmpty(c)) return def;
                    if (c == "1") return FeatureComparisonRule.MaxLevelIndex;
                    if (c == "2") return FeatureComparisonRule.ExactOrContainsMatch;
                    ConsoleHelper.PrintError("Wybierz 1 lub 2.");
                }
            }

            case FeatureValueType.Text:
            default:
                return FeatureComparisonRule.ExactOrContainsMatch;
        }
    }
}
