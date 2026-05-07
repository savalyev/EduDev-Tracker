using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{
    public enum ConversionType { Numeral, Color, Time, JsonXml, Url}

    [Table("conversion_history")]
    public class ConversionHistory
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(Profile)), Indexed] public int ProfileId { get; set; }
        [Ignore] public ConversionType Type { get; set; }

        [Column("Type")]
        public string TypeString
        {
            get => Type.ToString();
            set => Type = Enum.Parse<ConversionType>(value);
        }
        [NotNull] public string InputText { get; set; } = "";
        [NotNull] public string OutputText { get; set; } = "";
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
