using EduDev_Tracker.Data.Models.Joins;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    public enum TaskStatus { Open, InProgress, Done, Cancelled }
    public enum TaskPriority { Low = 0, Medium = 1, High = 2, Urgent = 3 }

    [Table("tasks")]
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [ForeignKey(typeof(Project))] public int? ProjectId { get; set; }

        [NotNull, MaxLength(300)] public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Category { get; set; }
        [Ignore] public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Column("Priority")]
        public string PriorityString
        {
            get => Priority.ToString();
            set => Priority = value switch
            {
                nameof(TaskPriority.Urgent) => TaskPriority.Urgent,
                nameof(TaskPriority.High)   => TaskPriority.High,
                nameof(TaskPriority.Low)    => TaskPriority.Low,
                _                           => TaskPriority.Medium,
            };
        }
        [Ignore] public TaskStatus Status { get; set; } = TaskStatus.Open;

        [Column("Status")]
        public string StatusString
        {
            get => Status.ToString();
            set => Status = value switch
            {
                nameof(TaskStatus.InProgress) => TaskStatus.InProgress,
                nameof(TaskStatus.Done)       => TaskStatus.Done,
                nameof(TaskStatus.Cancelled)  => TaskStatus.Cancelled,
                _                             => TaskStatus.Open,
            };
        }

        public DateTime? DueAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReminderAt { get; set; }

        public bool IsArchived { get; set; }
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Ignore]
        public TaskRecurrence? Recurrence { get; set; }

        [ManyToMany(typeof(TaskTag))]
        public List<Tag> Tags { get; set; } = new();
    }
}
