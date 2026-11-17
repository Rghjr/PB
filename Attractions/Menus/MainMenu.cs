namespace Attractions.Menus;

public class MainMenu
{
    private readonly OrganizerMenu _organizerMenu;
    private readonly UserMenu _userMenu;

    public MainMenu(OrganizerMenu organizerMenu, UserMenu userMenu)
    {
        _organizerMenu = organizerMenu ?? throw new ArgumentNullException(nameof(organizerMenu));
        _userMenu = userMenu ?? throw new ArgumentNullException(nameof(userMenu));
    }

    public void Run()
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();
            Console.WriteLine("====================================");
            Console.WriteLine("       ATTRACTIONS MANAGEMENT");
            Console.WriteLine("====================================");
            Console.WriteLine("1. Organizator");
            Console.WriteLine("2. Użytkownik");
            Console.WriteLine("0. Zakończ program");

            string choice = ConsoleHelper.ReadMenuChoice("Wybierz opcję: ");

            switch (choice)
            {
                case "1":
                    _organizerMenu.Show();
                    break;
                case "2":
                    _userMenu.Show();
                    break;
                case "0":
                    Console.WriteLine();
                    ConsoleHelper.PrintInfo("Zamykanie aplikacji Attractions. Do zobaczenia!");
                    return;
                default:
                    ConsoleHelper.PrintError("Nieprawidłowa opcja. Wybierz 1, 2 lub 0.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }
}
