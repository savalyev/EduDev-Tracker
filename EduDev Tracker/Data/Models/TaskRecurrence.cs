using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("task_recurrences")]
    public class TaskRecurrence
    {
        [PrimaryKey] public int TaskId { get; set; }   // 1:1 с задачей
        public string RuleType { get; set; } = "daily";
        public int IntervalN { get; set; } = 1;
        public int? DaysMask { get; set; }
        public int? DayOfMonth { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextDue { get; set; }
    }
}
