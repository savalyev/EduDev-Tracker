using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Tasks
{
    public static class RecurrenceCalculator
    {
        public static DateTime CalcFirstDue(TaskRecurrence rec, DateTime from)
        {
            return CalcNext(rec, from);
        }
        public static DateTime CalcNext(TaskRecurrence rec, DateTime from)
        {
            return rec.RuleType switch
            {
                "daily" => from.AddDays(rec.IntervalN),
                "weekly" => CalcNextWeekly(rec, from),
                "monthly" => CalcNextMonthly(rec, from),
                _ => from.AddDays(1)
            };
        }

        private static DateTime CalcNextWeekly(TaskRecurrence rec, DateTime from)
        {
            if (rec.DaysMask == null || rec.DaysMask == 0)
                return from.AddDays(7 * rec.IntervalN);

            // Ищем следующий выбранный день в пределах текущей недели (до следующего понедельника)
            for (int i = 1; i <= 6; i++)
            {
                var candidate = from.AddDays(i);
                int bit = DayOfWeekToBit(candidate.DayOfWeek);
                if ((rec.DaysMask & bit) != 0) return candidate;
                if (candidate.DayOfWeek == DayOfWeek.Monday) break;
            }

            // В текущей неделе дней нет — прыгаем в следующий цикл (+IntervalN недель от начала текущей)
            int daysSinceMonday = ((int)from.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var thisMonday = from.Date.AddDays(-daysSinceMonday);
            var nextCycleMonday = thisMonday.AddDays(7 * rec.IntervalN);

            for (int i = 0; i < 7; i++)
            {
                var candidate = nextCycleMonday.AddDays(i);
                int bit = DayOfWeekToBit(candidate.DayOfWeek);
                if ((rec.DaysMask & bit) != 0) return candidate;
            }

            return nextCycleMonday;
        }

        private static DateTime CalcNextMonthly(TaskRecurrence rec, DateTime from)
        {
            int day = rec.DayOfMonth ?? 1;
            var next = new DateTime(from.Year, from.Month, 1)
                .AddMonths(rec.IntervalN);

            int maxDay = DateTime.DaysInMonth(next.Year, next.Month);
            return new DateTime(next.Year, next.Month, Math.Min(day, maxDay));
        }

        private static int DayOfWeekToBit(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 8,
            DayOfWeek.Friday => 16,
            DayOfWeek.Saturday => 32,
            DayOfWeek.Sunday => 64,
            _ => 0
        };
    }
}
