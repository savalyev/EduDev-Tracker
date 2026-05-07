using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("note_categories")]
    public class NoteCategory
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [NotNull, MaxLength(100)] public string Name { get; set; } = "";
        public string? Color { get; set; }
    }
}
