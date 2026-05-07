using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models.Joins
{
    [Table("task_tags")]
    public class TaskTag
    {
        [ForeignKey(typeof(TaskItem))]
        public int TaskId { get; set; }

        [ForeignKey(typeof(Tag))]
        public int TagId { get; set; }
    }
}
