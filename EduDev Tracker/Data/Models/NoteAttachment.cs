using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("note_attachments")]
    public class NoteAttachment
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Note)), Indexed] public int NoteId { get; set; }
        [NotNull] public string FilePath { get; set; } = "";
        [NotNull] public string FileName { get; set; } = "";
        public string? MimeType { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
