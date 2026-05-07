using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("reminders")]
    public class Reminder
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [NotNull] public string EntityType { get; set; } = "";
        [Indexed] public int EntityId { get; set; }
        [Indexed] public DateTime FireAt { get; set; }
        [NotNull] public string Title { get; set; } = "";
        public string? Body { get; set; }
        public bool IsFired { get; set; }
        public int? PlatformId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
