using Attractions.Models;
using Attractions.Services;

namespace Attractions.Menus;

public class AddonMenu
{
    private readonly AddonService _addonService;
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;

    public AddonMenu(
        AddonService addonService,
        OfferService offerService,
        AttractionService attractionService)
    {
        _addonService = addonService ?? throw new ArgumentNullException(nameof(addonService));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
    }

    public void Show()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("       ZARZĄDZANIE DODATKAMI");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Wyświetl dodatki wybranej oferty");
            Console.WriteLine("2. Dodaj dodatek do oferty");
            Console.WriteLine("3. Edytuj dodatek oferty");
            Console.WriteLine("4. Usuń dodatek z oferty");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowAddonsForOffer();
                    break;
                case "2":
                    AddAddonToOffer();
                    break;
                case "3":
                    EditAddonInOffer();
                    break;
                case "4":
                    RemoveAddonFromOffer();
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

    private Offer? PromptAndFindOffer()
    {
        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            return null;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty: ");
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {offerId} nie istnieje.");
            return null;
        }

        return offer;
    }

    private void ShowAddonsForOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DODATKI KONKRETNEJ OFERTY");

        var offer = PromptAndFindOffer();
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var addonsResult = _addonService.GetAddonsForOffer(offer.Id);
        if (!addonsResult.IsSuccess || addonsResult.Data is null)
        {
            ConsoleHelper.PrintResult(addonsResult);
        }
        else
        {
            ConsoleHelper.DisplayAddonsList(offer, addonsResult.Data);
        }

        ConsoleHelper.Pause();
    }

    private void AddAddonToOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("DODAWANIE DODATKU DO OFERTY");

        var offer = PromptAndFindOffer();
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            ConsoleHelper.PrintError("Nie można dodawać dodatków do wycofanej oferty.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.PrintInfo($"Dodawanie dodatku do oferty: \"{offer.Name}\" (ID: {offer.Id})");

        string name = ConsoleHelper.ReadRequiredString("Nazwa dodatku: ", "Nazwa dodatku");
        string description = ConsoleHelper.ReadOptionalString("Opis dodatku (opcjonalnie): ");
        decimal price = ConsoleHelper.ReadNonNegativeDecimal("Cena dodatku (zł): ", "Cena dodatku");

        var result = _addonService.AddAddon(offer.Id, name, description, price);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void EditAddonInOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("EDYCJA DODATKU W OFERCIE");

        var offer = PromptAndFindOffer();
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        if (offer.Status == OfferStatus.Withdrawn)
        {
            ConsoleHelper.PrintError("Nie można edytować dodatków wycofanej oferty.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var addons = offer.Addons.OrderBy(a => a.Id).ToList();
        ConsoleHelper.DisplayAddonsList(offer, addons);
        if (addons.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int addonId = ConsoleHelper.ReadInt("Podaj ID dodatku do edycji: ");
        var existingAddon = offer.Addons.FirstOrDefault(a => a.Id == addonId);
        if (existingAddon is null)
        {
            ConsoleHelper.PrintError($"Dodatek o ID {addonId} nie istnieje w ofercie \"{offer.Name}\".");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.PrintInfo("Wprowadź nowe wartości lub naciśnij Enter, aby zachować obecne.");

        string newName = ConsoleHelper.ReadStringOrKeepCurrent(
            "Nazwa dodatku",
            existingAddon.Name,
            required: true,
            "Nazwa dodatku");
        string newDescription = ConsoleHelper.ReadStringOrKeepCurrent(
            "Opis dodatku",
            existingAddon.Description,
            required: false,
            "Opis dodatku",
            allowClearWithDash: true);
        decimal newPrice = ConsoleHelper.ReadNonNegativeDecimalOrKeepCurrent(
            "Cena dodatku",
            existingAddon.Price,
            "Cena dodatku");

        var result = _addonService.UpdateAddon(offer.Id, addonId, newName, newDescription, newPrice);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void RemoveAddonFromOffer()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("USUWANIE DODATKU Z OFERTY");

        var offer = PromptAndFindOffer();
        if (offer is null)
        {
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        var addons = offer.Addons.OrderBy(a => a.Id).ToList();
        ConsoleHelper.DisplayAddonsList(offer, addons);
        if (addons.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int addonId = ConsoleHelper.ReadInt("Podaj ID dodatku do usunięcia: ");
        var result = _addonService.RemoveAddon(offer.Id, addonId);

        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }
}
