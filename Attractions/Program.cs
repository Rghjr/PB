using System.Text;
using Attractions.Data;
using Attractions.Menus;
using Attractions.Services;

namespace Attractions;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        DataSeeder.Seed();

        var attractionService = new AttractionService();
        var offerService = new OfferService(attractionService);
        var addonService = new AddonService(offerService);
        var offerExtensionService = new OfferExtensionService(offerService);
        var orderService = new OrderService(offerService, attractionService, offerExtensionService);

        var attractionMenu = new AttractionMenu(attractionService);
        var offerMenu = new OfferMenu(offerService, attractionService);
        var addonMenu = new AddonMenu(addonService, offerService, attractionService);
        var relatedOfferMenu = new RelatedOfferMenu(offerService, attractionService);

        var organizerMenu = new OrganizerMenu(
            attractionService,
            offerService,
            orderService,
            attractionMenu,
            offerMenu,
            addonMenu,
            relatedOfferMenu);

        var userMenu = new UserMenu(
            offerService,
            attractionService,
            offerExtensionService,
            orderService);

        var mainMenu = new MainMenu(organizerMenu, userMenu);

        try
        {
            mainMenu.Run();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            ConsoleHelper.PrintInfo("Zakończono działanie aplikacji.");
        }
    }
}
