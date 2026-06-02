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
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        [Ignore] public TaskStatus Status { get; set; } = TaskStatus.Open;

        [Column("Status")]
        public string StatusString
        {
            get => Status.ToString();
            set => Status = Enum.Parse<TaskStatus>(value);
        }

        public DateTime? DueAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public bool IsArchived { get; set; }
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [OneToOne(CascadeOperations = CascadeOperation.All)]
        public TaskRecurrence? Recurrence { get; set; }

        [ManyToMany(typeof(TaskTag))]
        public List<Tag> Tags { get; set; } = new();
    }
}
