using EduDev_Tracker.Data.Models.Joins;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("cheatsheets")]
    public class Cheatsheet
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile))] public int? ProfileId { get; set; }
        [ForeignKey(typeof(CheatsheetCategory)), Indexed] public int CategoryId { get; set; }
        [NotNull, MaxLength(200)] public string Title { get; set; } = "";
        [NotNull] public string Content { get; set; } = "";
        public bool IsBuiltin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ManyToMany(typeof(CheatsheetTag))]
        public List<Tag> Tags { get; set; } = new();
    }
}
