using UrlRouter.Models;

namespace UrlRouter.Engine;

internal static class TimeConditionEvaluator
{
    public static bool Matches(DateTime now, TimeCondition? cond)
    {
        if (cond == null || cond.IsEmpty)
            return true;

        if (cond.Days.Length > 0 && !cond.Days.Contains(now.DayOfWeek))
            return false;

        var t = TimeOnly.FromDateTime(now);

        if (cond.StartTime != null && cond.EndTime != null)
        {
            var start = cond.StartTime.Value;
            var end = cond.EndTime.Value;

            if (start <= end)
            {
                // Normal range: 09:00-18:00
                if (t < start || t > end) return false;
            }
            else
            {
                // Overnight range: 22:00-06:00
                // Match if t >= start OR t <= end
                if (t < start && t > end) return false;
            }
        }
        else if (cond.StartTime != null && t < cond.StartTime.Value)
            return false;
        else if (cond.EndTime != null && t > cond.EndTime.Value)
            return false;

        return true;
    }
}
