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
    }
}
