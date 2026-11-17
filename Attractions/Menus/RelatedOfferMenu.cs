using Attractions.Services;

namespace Attractions.Menus;

public class RelatedOfferMenu
{
    private readonly OfferService _offerService;
    private readonly AttractionService _attractionService;

    public RelatedOfferMenu(OfferService offerService, AttractionService attractionService)
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
            Console.WriteLine("   ZARZĄDZANIE POWIĄZANIAMI OFERT");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Wyświetl powiązane oferty");
            Console.WriteLine("2. Połącz dwie oferty ze sobą");
            Console.WriteLine("3. Usuń powiązanie między ofertami");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    ShowRelatedOffers();
                    break;
                case "2":
                    LinkTwoOffers();
                    break;
                case "3":
                    UnlinkTwoOffers();
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

    private void ShowRelatedOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("POWIĄZANE OFERTY");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int offerId = ConsoleHelper.ReadInt("Podaj ID oferty, której powiązania chcesz zobaczyć: ");
        var offer = _offerService.GetOfferById(offerId);
        if (offer is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {offerId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Powiązane oferty dla: [ID: {offer.Id}] {offer.Name}");
        var related = _offerService.GetRelatedOffers(offer.Id, onlyAvailableForUser: false);
        ConsoleHelper.DisplayOffersList(related, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void LinkTwoOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("ŁĄCZENIE OFERT (DWUKIERUNKOWO)");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count < 2)
        {
            ConsoleHelper.PrintInfo("W systemie muszą istnieć co najmniej dwie oferty, aby utworzyć powiązanie.");
            ConsoleHelper.Pause();
            return;
        }

        int firstOfferId = ConsoleHelper.ReadInt("Podaj ID pierwszej oferty: ");
        int secondOfferId = ConsoleHelper.ReadInt("Podaj ID drugiej oferty: ");

        var result = _offerService.LinkOffers(firstOfferId, secondOfferId);
        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }

    private void UnlinkTwoOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("USUWANIE POWIĄZANIA MIĘDZY OFERTAMI");

        var offers = _offerService.GetAllOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        if (offers.Count == 0)
        {
            ConsoleHelper.Pause();
            return;
        }

        int firstOfferId = ConsoleHelper.ReadInt("Podaj ID pierwszej oferty: ");
        var firstOffer = _offerService.GetOfferById(firstOfferId);
        if (firstOffer is null)
        {
            ConsoleHelper.PrintError($"Oferta o ID {firstOfferId} nie istnieje.");
            ConsoleHelper.Pause();
            return;
        }

        var relatedOffers = _offerService.GetRelatedOffers(firstOfferId, onlyAvailableForUser: false);
        if (relatedOffers.Count == 0)
        {
            ConsoleHelper.PrintInfo($"Oferta \"{firstOffer.Name}\" (ID: {firstOffer.Id}) nie posiada żadnych powiązanych ofert.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Oferty aktualnie powiązane z \"{firstOffer.Name}\":");
        ConsoleHelper.DisplayOffersList(relatedOffers, _attractionService.GetAttractionById);

        int secondOfferId = ConsoleHelper.ReadInt("Podaj ID powiązanej oferty do odłączenia: ");
        var result = _offerService.UnlinkOffers(firstOfferId, secondOfferId);

        Console.WriteLine();
        ConsoleHelper.PrintResult(result);
        ConsoleHelper.Pause();
    }
}
