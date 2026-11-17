using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class OfferMenu
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;

    public OfferMenu(OfferService offerService, AttractionService attractionService)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
    }

    public void Show()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("        ZARZĄDZANIE OFERTAMI");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Wyświetl wszystkie oferty");
            Console.WriteLine("2. Wyświetl szczegóły oferty");
            Console.WriteLine("3. Dodaj ofertę do atrakcji");
            Console.WriteLine("4. Edytuj ofertę");
            Console.WriteLine("5. Wycofaj ofertę");
            Console.WriteLine("6. Usuń ofertę");
            Console.WriteLine("7. Wyświetl oferty zaplanowane");
            Console.WriteLine("8. Wyświetl oferty na dzisiaj");
            Console.WriteLine("9. Wyświetl oferty wycofane");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowOffersList("WSZYSTKIE OFERTY", _offerService.GetAllOffers());
                    break;
                case "2":
                    ShowOfferDetails();
                    break;
                case "3":
                    AddOffer();
                    break;
                case "4":
                    EditOffer();
                    break;
                case "5":
                    WithdrawOffer();
                    break;
                case "6":
                    DeleteOffer();
                    break;
                case "7":
                    ShowOffersList("OFERTY ZAPLANOWANE (PRZYSZŁE)", _offerService.GetPlannedOffers());
                    break;
                case "8":
                    ShowOffersList($"OFERTY NA DZISIAJ ({DateTime.Today:yyyy-MM-dd})", _offerService.GetTodayOffers());
                    break;
                case "9":
                    ShowOffersList("OFERTY WYCOFANE", _offerService.GetWithdrawnOffers());
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

    private void ShowOffersList(string header, List<Offer> offers)
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader(header);
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void ShowOfferDetails()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("SZCZEGÓŁY OFERTY");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty: ");
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {offerId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var attraction = _attractionService.GetAttractionById(offer.AttractionId);
        var relatedOffers = _offerService.GetRelatedOffers(offer.Id, onlyAvailableForUser: false);
        ConsoleHelper.DisplayOfferDetails(offer, attraction, relatedOffers);
        ConsoleHelper.Pause();
    }

    private void AddOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DODAWANIE NOWEJ OFERTY");

        var attractions = _attractionService.GetAllAttractions();
        if (attractions.Count == 0)
        {
            ConsoleHelper.PrintError("Brak atrakcji w systemie. Najpierw dodaj atrakcję, aby przypisać do niej ofertę.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine("Dostępne atrakcje:");
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);

        int attractionId = ConsoleHelper.ReadInt("Podaj ID atrakcji dla nowej oferty: ");
        if (_attractionService.GetAttractionById(attractionId) is null)
        {
            ConsoleHelper.PrintError($"Oferta musi być przypisana do istniejącej atrakcji – brak atrakcji o ID {attractionId}.");
            ConsoleHelper.Pause();
            return;
        }

        string name = ConsoleHelper.ReadRequiredString("Nazwa oferty: ", "Nazwa oferty");
        string description = ConsoleHelper.ReadOptionalString("Opis oferty (opcjonalnie): ");
        decimal price = ConsoleHelper.ReadNonNegativeDecimal("Cena podstawowa oferty (zł): ", "Cena oferty");
        DateTime date = ConsoleHelper.ReadDate("Data oferty (RRRR-MM-DD): ");

        var result = _offerService.AddOffer(attractionId, name, description, price, date);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void EditOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("EDYCJA OFERTY");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty do edycji: ");
        var existing = _offerService.GetOfferById(offerId);
        if (existing is null)
        {
            ConsoleHelper.PrintError($"Nie można edytować oferty – oferta o ID {offerId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Dostępne atrakcje:");
        var attractions = _attractionService.GetAllAttractions();
        ConsoleHelper.DisplayAttractionsList(attractions, id => _attractionService.GetOffersForAttraction(id).Count);

        int newAttractionId = ConsoleHelper.ReadIntOrKeepCurrent("ID atrakcji", existing.AttractionId);
        if (_attractionService.GetAttractionById(newAttractionId) is null)
        {
            ConsoleHelper.PrintError($"Nie istnieje atrakcja o ID {newAttractionId}. Zmiany nie zostały zapisane.");
            ConsoleHelper.Pause();
            return;
        }

        string newName = ConsoleHelper.ReadStringOrKeepCurrent("Nazwa oferty", existing.Name, required: true, "Nazwa oferty");
        string newDescription = ConsoleHelper.ReadStringOrKeepCurrent(
            "Opis oferty",
            existing.Description,
            required: false,
            "Opis oferty",
            allowClearWithDash: true);
        decimal newPrice = ConsoleHelper.ReadNonNegativeDecimalOrKeepCurrent("Cena podstawowa", existing.Price, "Cena oferty");
        DateTime newDate = ConsoleHelper.ReadDateOrKeepCurrent("Data oferty (RRRR-MM-DD)", existing.Date);
        OfferStatus newStatus = ConsoleHelper.ReadOfferStatusOrKeepCurrent(existing.Status);

        var result = _offerService.UpdateOffer(
            offerId,
            newAttractionId,
            newName,
            newDescription,
            newPrice,
            newDate,
            newStatus);

        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void WithdrawOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("WYCOFYWANIE OFERTY");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty do wycofania: ");
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {offerId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            ConsoleHelper.PrintError($"Oferta \"{offer.Name}\" (ID: {offer.Id}) jest już wycofana.");
            ConsoleHelper.Pause();
            return;
        }

        bool confirmed = ConsoleHelper.ReadConfirmation(
            $"Czy na pewno chcesz wycofać ofertę \"{offer.Name}\" (powiązania z innymi ofertami: {offer.RelatedOfferIds.Count})?");

        if (!confirmed)
        {
            ConsoleHelper.PrintInfo("Wycofywanie oferty zostało anulowane.");
            ConsoleHelper.Pause();
            return;
        }

        var result = _offerService.WithdrawOffer(offerId);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void DeleteOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("USUWANIE OFERTY");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty do usunięcia: ");
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            ConsoleHelper.PrintError($"Nie można usunąć oferty – oferta o ID {offerId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        bool confirmed = ConsoleHelper.ReadConfirmation(
            $"Czy na pewno chcesz trwale usunąć ofertę \"{offer.Name}\" wraz z jej dodatkami ({offer.Addons.Count}) i powiązaniami ({offer.RelatedOfferIds.Count})?");

        if (!confirmed)
        {
            ConsoleHelper.PrintInfo("Operacja usuwania oferty została anulowana.");
            ConsoleHelper.Pause();
            return;
        }

        var result = _offerService.DeleteOffer(offerId);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }
}
