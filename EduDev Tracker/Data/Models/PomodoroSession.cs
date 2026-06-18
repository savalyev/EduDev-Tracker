using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    public enum PomodoroPhase { Work, ShortBreak, LongBreak }

    [Table("pomodoro_sessions")]
    public class PomodoroSession
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [ForeignKey(typeof(PomodoroPreset))] public int? PresetId { get; set; }
        [ForeignKey(typeof(TaskItem))] public int? TaskId { get; set; }
        [ForeignKey(typeof(Project))] public int? ProjectId { get; set; }

        [Ignore] public PomodoroPhase Phase { get; set; }

        [Column("Phase")]
        public string PhaseString
        {
            get => Phase.ToString();
            set => Phase = value switch
            {
                nameof(PomodoroPhase.ShortBreak) => PomodoroPhase.ShortBreak,
                nameof(PomodoroPhase.LongBreak)  => PomodoroPhase.LongBreak,
                _                                => PomodoroPhase.Work,
            };
        }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public int PlannedMinutes { get; set; }
        public int? ActualMinutes { get; set; }
        public bool WasInterrupted { get; set; }
        public string? Notes { get; set; }
    }
}
