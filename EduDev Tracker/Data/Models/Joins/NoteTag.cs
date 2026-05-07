using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models.Joins
{
    [Table("note_tags")]
    public class NoteTag
    {
        [ForeignKey(typeof(Note))]
        public int NoteId { get; set; }

        [ForeignKey(typeof(Tag))]
        public int TagId { get; set; }
    }
}
