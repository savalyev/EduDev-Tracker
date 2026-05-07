using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("tags")]
    public class Tag
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }

        [ForeignKey(typeof(Profile)), Indexed]
        public int ProfileId { get; set; }

        [NotNull, MaxLength(64)] 
        public string Name { get; set; } = "";
        public string? Color { get; set; }
    }
}
