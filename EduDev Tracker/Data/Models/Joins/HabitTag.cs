using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models.Joins
{
    [Table("habit_tags")]
    public class HabitTag
    {
        [ForeignKey(typeof(Habit))]
        public int HabitId { get; set; }
        [ForeignKey(typeof(Tag))]
        public int TagId { get; set; }
    }
}
