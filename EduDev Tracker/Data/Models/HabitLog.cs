using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("habit_logs")]
    public class HabitLog
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }

        [ForeignKey(typeof(Habit)), Indexed]
        public int HabitId { get; set; }
        [Indexed, NotNull] public string LogDate { get; set; } = "";
        public double Value { get; set; } = 1;
        public string? Note { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
