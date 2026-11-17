using Attractions.Services;

namespace Attractions.Menus;

public class OrganizerMenu
{
    private readonly AttractionService _attractionService;
    private readonly OfferService _offerService;
    private readonly OrderService _orderService;
    private readonly AttractionMenu _attractionMenu;
    private readonly OfferMenu _offerMenu;
    private readonly AddonMenu _addonMenu;
    private readonly RelatedOfferMenu _relatedOfferMenu;

    public OrganizerMenu(
        AttractionService attractionService,
        OfferService offerService,
        OrderService orderService,
        AttractionMenu attractionMenu,
        OfferMenu offerMenu,
        AddonMenu addonMenu,
        RelatedOfferMenu relatedOfferMenu)
    {
        _attractionService = attractionService ?? throw new ArgumentNullException(nameof(attractionService));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _attractionMenu = attractionMenu ?? throw new ArgumentNullException(nameof(attractionMenu));
        _offerMenu = offerMenu ?? throw new ArgumentNullException(nameof(offerMenu));
        _addonMenu = addonMenu ?? throw new ArgumentNullException(nameof(addonMenu));
        _relatedOfferMenu = relatedOfferMenu ?? throw new ArgumentNullException(nameof(relatedOfferMenu));
    }

    public void Show()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("          ORGANIZATOR");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Zarządzanie atrakcjami");
            Console.WriteLine("2. Zarządzanie ofertami");
            Console.WriteLine("3. Zarządzanie dodatkami");
            Console.WriteLine("4. Zarządzanie powiązaniami ofert");
            Console.WriteLine("5. Wyświetl oferty zaplanowane");
            Console.WriteLine("6. Wyświetl oferty na dzisiaj");
            Console.WriteLine("7. Wyświetl oferty wycofane");
            Console.WriteLine("8. Wyświetl złożone zamówienia klientów");
            Console.WriteLine("0. Powrót");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    _attractionMenu.Show();
                    break;
                case "2":
                    _offerMenu.Show();
                    break;
                case "3":
                    _addonMenu.Show();
                    break;
                case "4":
                    _relatedOfferMenu.Show();
                    break;
                case "5":
                    ShowPlannedOffers();
                    break;
                case "6":
                    ShowTodayOffers();
                    break;
                case "7":
                    ShowWithdrawnOffers();
                    break;
                case "8":
                    ShowCustomerOrders();
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

    private void ShowPlannedOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("OFERTY ZAPLANOWANE (PRZYSZŁE)");
        var offers = _offerService.GetPlannedOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void ShowTodayOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader($"OFERTY NA DZISIAJ ({DateTime.Today:yyyy-MM-dd})");
        var offers = _offerService.GetTodayOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void ShowWithdrawnOffers()
    {
        ConsoleHelper.ClearScreen();
        ConsoleHelper.PrintSectionHeader("OFERTY WYCOFANE");
        var offers = _offerService.GetWithdrawnOffers();
        ConsoleHelper.DisplayOffersList(offers, _attractionService.GetAttractionById);
        ConsoleHelper.Pause();
    }

    private void ShowCustomerOrders()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.PrintSectionHeader("ZŁOŻONE ZAMÓWIENIA KLIENTÓW");

            var orders = _orderService.GetAllOrders();
            ConsoleHelper.DisplayOrdersList(orders);
            if (orders.Count == 0)
            {
                ConsoleHelper.Pause();
                return;
            }

            Console.WriteLine();
            int orderId = ConsoleHelper.ReadInt("Podaj ID zamówienia, aby wyświetlić szczegóły (0 = powrót): ");
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
}
