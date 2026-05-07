using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("pomodoro_presets")]
    public class PomodoroPreset
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [NotNull, MaxLength(80)] public string Name { get; set; } = "";
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMin { get; set; } = 5;
        public int LongBreakMin { get; set; } = 15;
        public int CyclesToLong { get; set; } = 4;
        public bool IsDefault { get; set; }
    }
}
