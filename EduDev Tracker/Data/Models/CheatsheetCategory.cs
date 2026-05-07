using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    [Table("cheatsheet_categories")]
    public class CheatsheetCategory
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Unique, NotNull, MaxLength(80)] public string Name { get; set; } = "";
        public string? Icon { get; set; }
        public bool IsBuiltin { get; set; }

    }
}
