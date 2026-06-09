using EduDev_Tracker.Data.Models.Joins;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("notes")]
    public class Note
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [ForeignKey(typeof(NoteCategory))] public int? CategoryId { get; set; }
        [NotNull, MaxLength(300)] public string Title { get; set; } = "";
        [NotNull] public string Content { get; set; } = "";
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<NoteAttachment> Attachments { get; set; } = new();

        [ManyToMany(typeof(NoteTag))]
        public List<Tag> Tags { get; set; } = new();

        [OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
        public List<NoteVersion> Versions { get; set; } = new();
    }
}
