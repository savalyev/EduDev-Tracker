using EduDev_Tracker.Data.Models.Joins;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("note_versions")]
    public class NoteVersion
    {
        [PrimaryKey][AutoIncrement] public int Id { get; set; }

        [ForeignKey(typeof(Note)), Indexed] public int NoteId { get; set; }

        [NotNull] public string Content { get; set; } = "";

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        [Ignore] public string VersionLabel { get; set; } = "";
    }
}
