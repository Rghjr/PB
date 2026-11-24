namespace Attractions.Models;

public class ParticipantGroupBreakdown
{
    public int AdultsCount { get; }
    public int ChildrenCount { get; }
    public int YouthOrStudentsCount { get; }
    public int SeniorsCount { get; }
    public int DisabledCount { get; }

    public ParticipantGroupBreakdown(
        int adultsCount,
        int childrenCount = 0,
        int youthOrStudentsCount = 0,
        int seniorsCount = 0,
        int disabledCount = 0)
    {
        AdultsCount = Math.Max(0, adultsCount);
        ChildrenCount = Math.Max(0, childrenCount);
        YouthOrStudentsCount = Math.Max(0, youthOrStudentsCount);
        SeniorsCount = Math.Max(0, seniorsCount);
        DisabledCount = Math.Max(0, disabledCount);
    }

    public int TotalCount =>
        AdultsCount +
        ChildrenCount +
        YouthOrStudentsCount +
        SeniorsCount +
        DisabledCount;

    public bool HasAdultsOrSeniors => AdultsCount > 0 || SeniorsCount > 0 || DisabledCount > 0;

    public static ParticipantGroupBreakdown SingleAdult()
    {
        return new ParticipantGroupBreakdown(adultsCount: 1);
    }

    public static ParticipantGroupBreakdown FromAdultsCount(int count)
    {
        return new ParticipantGroupBreakdown(adultsCount: Math.Max(1, count));
    }

    public static ParticipantGroupBreakdown FromParticipantProfile(ParticipantProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.ParticipantAges.Count == 0)
        {
            return profile.RequiresWheelchairAccess
                ? new ParticipantGroupBreakdown(adultsCount: 0, disabledCount: 1)
                : SingleAdult();
        }

        int adults = 0;
        int children = 0;
        int youth = 0;
        int seniors = 0;
        int disabled = 0;

        foreach (int age in profile.ParticipantAges)
        {
            switch (TicketDiscountPolicy.ClassifyByAge(age))
            {
                case TicketCategory.Child:
                    children++;
                    break;
                case TicketCategory.YouthOrStudent:
                    youth++;
                    break;
                case TicketCategory.Senior:
                    seniors++;
                    break;
                default:
                    adults++;
                    break;
            }
        }

        int total = adults + children + youth + seniors;
        int desiredDisabled = profile.DisabledParticipantsCount > 0
            ? Math.Min(profile.DisabledParticipantsCount, total)
            : (profile.RequiresWheelchairAccess ? 1 : 0);

        for (int i = 0; i < desiredDisabled; i++)
        {
            if (adults > 0)
            {
                adults--;
                disabled++;
            }
            else if (seniors > 0)
            {
                seniors--;
                disabled++;
            }
            else if (youth > 0)
            {
                youth--;
                disabled++;
            }
            else if (children > 0)
            {
                children--;
                disabled++;
            }
        }

        return new ParticipantGroupBreakdown(adults, children, youth, seniors, disabled);
    }

    public IReadOnlyList<(TicketCategory Category, int Count)> GetNonZeroCategories()
    {
        var list = new List<(TicketCategory, int)>();
        if (AdultsCount > 0) list.Add((TicketCategory.Adult, AdultsCount));
        if (ChildrenCount > 0) list.Add((TicketCategory.Child, ChildrenCount));
        if (YouthOrStudentsCount > 0) list.Add((TicketCategory.YouthOrStudent, YouthOrStudentsCount));
        if (SeniorsCount > 0) list.Add((TicketCategory.Senior, SeniorsCount));
        if (DisabledCount > 0) list.Add((TicketCategory.PersonWithDisability, DisabledCount));
        return list;
    }

    public string FormatCompactSummary()
    {
        var parts = GetNonZeroCategories()
            .Select(x =>
            {
                decimal disc = TicketDiscountPolicy.GetCategoryDiscountPercent(x.Category);
                string discTag = disc > 0 ? $" (-{disc:0}%)" : string.Empty;
                return $"{TicketDiscountPolicy.GetCategoryDisplayName(x.Category)}: {x.Count}{discTag}";
            })
            .ToList();

        return parts.Count > 0
            ? $"{TotalCount} os. [{string.Join(", ", parts)}]"
            : "0 os.";
    }
}
