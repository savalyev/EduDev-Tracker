using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Habits
{
    public interface IHabitService
    {
        Task<List<Habit>> GetActiveAsync(int profileId);
        Task<HabitSchedule> GetScheduleAsync(int habitId);
        Task<Habit?> GetByIdAsync(int id);
        Task<Habit> CreateAsync(int profileId, string title, HabitType type, HabitSchedule schedule, double targetValue, string targetUnit, string? description = null);
        Task UpdateAsync(Habit habit);
        Task LogAsync(int habitId, DateTime? date = null, double value = 1, string? note = null);
        Task UnlogAsync(int habitId, DateTime date);
        Task ArchiveAsync(int habitId, bool archived = true);
        Task<bool> IsCompletedTodayAsync(int habitId);
    }
}
