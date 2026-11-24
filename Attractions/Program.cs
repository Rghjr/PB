using System.Text;
using System.Text.Json.Serialization;
using Attractions.Data;
using Attractions.Menus;
using Attractions.Services;

namespace Attractions;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        DataSeeder.Seed();

        if (args.Any(a => string.Equals(a, "--console", StringComparison.OrdinalIgnoreCase)))
        {
            RunConsoleMode();
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<FeatureService>();
        builder.Services.AddSingleton<AttractionService>();
        builder.Services.AddSingleton<OfferService>();
        builder.Services.AddSingleton<AddonService>();
        builder.Services.AddSingleton<OfferExtensionService>();
        builder.Services.AddSingleton<OrderService>();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
        });

        var app = builder.Build();

        app.UseCors();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static void RunConsoleMode()
    {
        if (!string.IsNullOrEmpty(JsonFeatureStore.LastWarningMessage))
        {
            ConsoleHelper.PrintWarning(JsonFeatureStore.LastWarningMessage);
        }

        var featureService = new FeatureService();
        var attractionService = new AttractionService();
        var offerService = new OfferService(attractionService);
        var addonService = new AddonService(offerService);
        var offerExtensionService = new OfferExtensionService(offerService);
        var orderService = new OrderService(offerService, attractionService, offerExtensionService);

        var featureMenu = new FeatureMenu(featureService);
        var attractionMenu = new AttractionMenu(attractionService, featureService);
        var offerMenu = new OfferMenu(offerService, attractionService, featureService);
        var addonMenu = new AddonMenu(addonService, offerService, attractionService, featureService);
        var relatedOfferMenu = new RelatedOfferMenu(offerService, attractionService);

        var organizerMenu = new OrganizerMenu(
            attractionService,
            offerService,
            orderService,
            attractionMenu,
            offerMenu,
            addonMenu,
            relatedOfferMenu,
            featureMenu);

        var userMenu = new UserMenu(
            offerService,
            attractionService,
            offerExtensionService,
            orderService,
            featureService);

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
