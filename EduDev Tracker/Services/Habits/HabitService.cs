using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Habits
{
    public class HabitService : IHabitService
    {
        private readonly HabitRepository _repo;

        public HabitService(HabitRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Habit>> GetActiveAsync(int profileId)
            => _repo.GetActiveAsync(profileId);

        public Task<HabitSchedule> GetScheduleAsync(int habitId)
            => _repo.GetScheduleAsync(habitId);

        public Task<Habit?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<Habit> CreateAsync(
            int profileId,
            string title,
            HabitType type,
            HabitSchedule schedule,
            double targetValue,
            string targetUnit,
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("Название привычки обязательно!");
            }

            var habit = new Habit
            {
                ProfileId = profileId,
                Title = title.Trim(),
                Type = type,
                Description = description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TargetValue = targetValue,
                TargetUnit = targetUnit,
                Schedules = schedule
            };

            await _repo.SaveWithChildrenAsync(habit);
            return habit;
        }

        public Task UpdateAsync(Habit habit)
            => _repo.SaveWithChildrenAsync(habit);

        public Task LogAsync(int habitId, DateTime? date = null, double value = 1, string? note = null)
            => _repo.LogAsync(habitId, date, value, note);

        public Task UnlogAsync(int habitId, DateTime date)
            => _repo.UnlogAsync(habitId, date);

        public Task ArchiveAsync(int habitId, bool archived = true)
            => _repo.ArchiveAsync(habitId, archived);
        public Task<bool> IsCompletedTodayAsync(int habitId)
            => _repo.IsCompletedTodayAsync(habitId);
        public Task<bool> IsCompletedTodayAsync(int habitId, DateTime date)
            => _repo.IsCompletedTodayAsync(habitId, date);
        public Task DeleteAsync(int habitId)
            => _repo.DeleteAsync(habitId);
        public Task<int> GetStreakAsync(int habitId)
            => _repo.GetStreakAsync(habitId);
        public Task<Habit> GetByIdWithChildrenAsync(int id)
            => _repo.GetByIdWithChildrenAsync(id);
        public Task<List<Habit>> GetArchivedAsync(int profileId)
            => _repo.GetArchivedAsync(profileId);
    }
}
