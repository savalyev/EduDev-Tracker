using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models.Joins
{
    [Table("cheatsheet_tags")]
    public class CheatsheetTag
    {
        [ForeignKey(typeof(Cheatsheet))] 
        public int CheatsheetId { get; set; }

        [ForeignKey(typeof(Tag))] 
        public int TagId { get; set; }
    }
}
