using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Habits
{
    public interface IHabitService
    {
        Task<List<Habit>> GetActiveAsync(int profileId);
        Task<Habit?> GetByIdAsync(int id);
        Task<Habit> CreateAsync(int profileId, string title, HabitType type, string? description = null);
        Task UpdateAsync(Habit habit);
        Task MarkDoneAsync(int habitId, DateTime? date = null, double value = 1, string? note = null);
        Task UndoDoneAsync(int habitId, DateTime date);
        Task ArchiveAsync(int habitId, bool archived = true);
    }
}
}
