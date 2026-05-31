using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("habit_schedules")]
    public class HabitSchedule
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }

        [ForeignKey(typeof(Habit)), Indexed]
        public int HabitId { get; set; }
        public int DayMask { get; set; } = 0b1111111;
        public string? TimeOfDay { get; set; }
        public int ReminderOffsetMinutes { get; set; } = 0;

        public string GetFormattedDays()
        {
            var days = new List<string>();
            if ((DayMask & 0b0000001) != 0) days.Add("Пн");
            if ((DayMask & 0b0000010) != 0) days.Add("Вт");
            if ((DayMask & 0b0000100) != 0) days.Add("Ср");
            if ((DayMask & 0b0001000) != 0) days.Add("Чт");
            if ((DayMask & 0b0010000) != 0) days.Add("Пт");
            if ((DayMask & 0b0100000) != 0) days.Add("Сб");
            if ((DayMask & 0b1000000) != 0) days.Add("Вс");
            return days.Any() ? string.Join(", ", days) : "—";
        }
    }
}
