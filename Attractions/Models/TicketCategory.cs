namespace Attractions.Models;

public enum TicketCategory
{
    Adult,
    Child,
    YouthOrStudent,
    Senior,
    PersonWithDisability
}

public static class TicketDiscountPolicy
{
    public static decimal GetCategoryDiscountPercent(TicketCategory category)
    {
        return category switch
        {
            TicketCategory.Adult => 0m,
            TicketCategory.Child => 50m,
            TicketCategory.YouthOrStudent => 30m,
            TicketCategory.Senior => 25m,
            TicketCategory.PersonWithDisability => 50m,
            _ => 0m
        };
    }

    public static string GetCategoryDisplayName(TicketCategory category)
    {
        return category switch
        {
            TicketCategory.Adult => "Dorosły (normalny)",
            TicketCategory.Child => "Dziecko (do 12 lat)",
            TicketCategory.YouthOrStudent => "Młodzież / Student (13–26 lat)",
            TicketCategory.Senior => "Senior (65+ lat)",
            TicketCategory.PersonWithDisability => "Osoba z niepełnosprawnością",
            _ => category.ToString()
        };
    }

    /// <summary>
    /// Dodatkowy rabat grupowy naliczany od sumy biletów przy większej liczbie uczestników:
    /// - grupa 4–7 osób: 5% rabatu grupowego,
    /// - grupa 8 i więcej osób: 10% rabatu grupowego.
    /// </summary>
    public static decimal GetExtraGroupDiscountPercent(int totalParticipants)
    {
        if (totalParticipants >= 8)
        {
            return 10m;
        }

        if (totalParticipants >= 4)
        {
            return 5m;
        }

        return 0m;
    }

    public static TicketCategory ClassifyByAge(int age)
    {
        if (age <= 12)
        {
            return TicketCategory.Child;
        }

        if (age <= 26)
        {
            return TicketCategory.YouthOrStudent;
        }

        if (age >= 65)
        {
            return TicketCategory.Senior;
        }

        return TicketCategory.Adult;
    }
}
