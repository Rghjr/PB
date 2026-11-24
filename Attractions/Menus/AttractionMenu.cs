using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class AttractionMenu
{
    private readonly AttractionService _attractionService;
    private readonly FeatureService _featureService;

    public AttractionMenu(AttractionService attractionService, FeatureService featureService)
    {
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
        _featureService = featureService ?? throw new ArgumentNullException(nameof(featureService));
    }

    public void Show()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("      ZARZĄDZANIE ATRAKCJAMI");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Wyświetl wszystkie atrakcje");
            Console.WriteLine("2. Wyświetl szczegóły atrakcji");
            Console.WriteLine("3. Dodaj atrakcję");
            Console.WriteLine("4. Edytuj atrakcję");
            Console.WriteLine("5. Usuń atrakcję");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowAllAttractions();
                    break;
                case "2":
                    ShowAttractionDetails();
                    break;
                case "3":
                    AddAttraction();
                    break;
                case "4":
                    EditAttraction();
                    break;
                case "5":
                    DeleteAttraction();
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

    private void ShowAllAttractions()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("WSZYSTKIE ATRAKCJE");
        var attractions = _attractionService.GetAllAttractions();
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);
        ConsoleHelper.Pause();
    }

    private void ShowAttractionDetails()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("SZCZEGÓŁY ATRAKCJI");

        var attractions = _attractionService.GetAllAttractions();
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);
        if (attractions.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int id = ConsoleHelper.ReadInt("Podaj ID atrakcji: ");
        var attraction = _attractionService.GetAttractionById(id);
        if (attraction is null)
        {
            ConsoleHelper.PrintError($"Atrakcja o ID {id} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var offers = _attractionService.GetOffersForAttraction(attraction.Id);
        ConsoleHelper.DisplayAttractionDetails(attraction, offers);
        ConsoleHelper.Pause();
    }

    private void AddAttraction()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DODAWANIE NOWEJ ATRAKCJI");

        string name = ConsoleHelper.ReadRequiredString("Nazwa atrakcji: ", "Nazwa atrakcji");
        string location = ConsoleHelper.ReadRequiredString("Lokalizacja atrakcji: ", "Lokalizacja atrakcji");
        string description = ConsoleHelper.ReadOptionalString("Opis atrakcji (opcjonalnie): ");
        int maxParticipants = ConsoleHelper.ReadIntInRangeOrDefault(
            "Maksymalna liczba osób jednocześnie na atrakcji (1-500)",
            1,
            500,
            defaultValue: 30);

        List<FeatureAssignment>? features = null;
        var availableDefs = _featureService.GetAllFeatures();
        if (availableDefs.Count > 0 &&
            ConsoleHelper.ReadConfirmation("Czy chcesz teraz przypisać cechy ogólne do tej atrakcji?", defaultYes: false))
        {
            features = ConsoleHelper.EditFeatureAssignmentsInteractive(
                $"CECHY OGÓLNE ATRAKCJI: {name}",
                availableDefs,
                Enumerable.Empty<FeatureAssignment>());
        }

        var result = _attractionService.AddAttraction(name, description, location, features, maxParticipants);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void EditAttraction()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("EDYCJA ATRAKCJI");

        var attractions = _attractionService.GetAllAttractions();
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);
        if (attractions.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int id = ConsoleHelper.ReadInt("Podaj ID atrakcji do edycji: ");
        var existing = _attractionService.GetAttractionById(id);
        if (existing is null)
        {
            ConsoleHelper.PrintError($"Nie można edytować atrakcji – atrakcja o ID {id} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.PrintInfo("Wprowadź nowe dane lub naciśnij Enter, aby zachować aktualną wartość.");

        string newName = ConsoleHelper.ReadStringOrKeepCurrent("Nazwa atrakcji", existing.Name, required: true, "Nazwa atrakcji");
        string newLocation = ConsoleHelper.ReadStringOrKeepCurrent("Lokalizacja", existing.Location, required: true, "Lokalizacja");
        string newDescription = ConsoleHelper.ReadStringOrKeepCurrent(
            "Opis",
            existing.Description,
            required: false,
            "Opis",
            allowClearWithDash: true);
        int newMaxParticipants = ConsoleHelper.ReadIntOrKeepCurrent(
            "Maksymalna liczba osób na atrakcji",
            existing.MaxParticipants);

        List<FeatureAssignment>? updatedFeatures = null;
        var availableDefs = _featureService.GetAllFeatures();
        if (availableDefs.Count > 0 &&
            ConsoleHelper.ReadConfirmation(
                $"Czy chcesz edytować cechy ogólne tej atrakcji (aktualnie przypisanych: {existing.Features.Count})?",
                defaultYes: false))
        {
            updatedFeatures = ConsoleHelper.EditFeatureAssignmentsInteractive(
                $"EDYCJA CECH ATRAKCJI: {newName}",
                availableDefs,
                existing.Features);
        }

        var result = _attractionService.UpdateAttraction(
            id,
            newName,
            newDescription,
            newLocation,
            updatedFeatures,
            newMaxParticipants);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void DeleteAttraction()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("USUWANIE ATRAKCJI");

        var attractions = _attractionService.GetAllAttractions();
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);
        if (attractions.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int id = ConsoleHelper.ReadInt("Podaj ID atrakcji do usunięcia: ");
        var existing = _attractionService.GetAttractionById(id);
        if (existing is null)
        {
            ConsoleHelper.PrintError($"Nie można usunąć atrakcji – atrakcja o ID {id} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        int offersCount = _attractionService.GetOffersForAttraction(id).Count;
        bool confirmed = ConsoleHelper.ReadConfirmation(
            $"Czy na pewno chcesz usunąć atrakcję \"{existing.Name}\" wraz z przypisanymi ofertami ({offersCount})?");

        if (!confirmed)
        {
            ConsoleHelper.PrintInfo("Operacja usuwania atrakcji została anulowana.");
            ConsoleHelper.Pause();
            return;
        }

        var result = _attractionService.DeleteAttraction(id);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }
}
