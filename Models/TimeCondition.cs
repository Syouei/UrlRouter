namespace UrlRouter.Models;

public class TimeCondition
{
    public DayOfWeek[] Days { get; set; } = Array.Empty<DayOfWeek>();

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public bool IsEmpty => Days.Length == 0 && StartTime == null && EndTime == null;

    public string Summary
    {
        get
        {
            if (IsEmpty) return "Always";

            var parts = new List<string>();

            if (Days.Length > 0)
            {
                var dayNames = Days.Length == 7
                    ? "Every day"
                    : string.Join(", ", Days.Select(d => d switch
                    {
                        DayOfWeek.Monday => "Mon",
                        DayOfWeek.Tuesday => "Tue",
                        DayOfWeek.Wednesday => "Wed",
                        DayOfWeek.Thursday => "Thu",
                        DayOfWeek.Friday => "Fri",
                        DayOfWeek.Saturday => "Sat",
                        DayOfWeek.Sunday => "Sun",
                        _ => d.ToString()
                    }));
                parts.Add(dayNames);
            }

            if (StartTime != null && EndTime != null)
                parts.Add($"{StartTime:HH:mm}-{EndTime:HH:mm}");
            else if (StartTime != null)
                parts.Add($"from {StartTime:HH:mm}");
            else if (EndTime != null)
                parts.Add($"until {EndTime:HH:mm}");

            return string.Join(", ", parts);
        }
    }
}
