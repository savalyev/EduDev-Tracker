using System;
using System.Collections.Generic;
using System.Text;
using SQLiteNetExtensions.Attributes;
using SQLite;

namespace EduDev_Tracker.Data.Models
{
    [Table("profiles")]
    public class Profile
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [NotNull, MaxLength(100)] public string Name { get; set; } = "";
        [Indexed(Unique = false)] public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? AvatarPath { get; set; }
        public string? AvatarUrl { get; set; }
        public string? OAuthProvider { get; set; }
        public string? OAuthId { get; set; }
        public bool IsLocal { get; set; } = true;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
        public List<Habit> Habits { get; set; } = new();
        [OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
        public List<TaskItem> Tasks { get; set; } = new();
        [OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
        public List<Note> Notes { get; set; } = new();

    }
}
