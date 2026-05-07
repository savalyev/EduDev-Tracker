using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data.Models
{

    public enum HabitType { Binary, Quentitative, Time, Negative}
    public enum HabitPeriod {Day, Week, Month }

    [Table("habits")]
    public class Habit
    {

        [PrimaryKey, AutoIncrement] public int Id { get; set; }

        [ForeignKey(typeof(Profile)), Indexed]
        public int ProfileId { get; set; }
        [NotNull, MaxLength(200)] public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }

        [Ignore] public HabitType Type { get; set; } = HabitType.Binary;

        [Column("Type")]
        public string TypeString
        {
            get => Type.ToString();
            set => Type = Enum.Parse<HabitType>(value);
        }
        public double? TargetValue { get; set; }
        public string? TargetUnit { get; set; }
        [Ignore] public HabitPeriod TargetPeriod { get; set; } = HabitPeriod.Day;

        [Column("TargetPeriod")]
        public string TargetPerionString
        {
            get => TargetPeriod.ToString();
            set => TargetPeriod = Enum.Parse<HabitPeriod>(value);
        }
        public bool IsArchived { get; set; }
        public bool IsFrozen { get; set; }
        public DateTime FronzenUntil { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<HabitLog> Logs { get; set; } = new();

        [ManyToMany(typeof(HabitLog))]
        public List<Tag> Tags { get; set; } = new();




    }
}
