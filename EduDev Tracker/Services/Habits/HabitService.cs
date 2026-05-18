using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Habits
{
    public class HabitService: IHabitService
    {
        private readonly HabitRepository _repo;

        public HabitService(HabitRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Habit>> GetActiveAsync(int profileId)
            => _repo.GetActiveAsync(profileId);

        public Task<Habit?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<Habit> CreateAsync(int profileId, string title, HabitType type, string? description = null)
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
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.SaveWithChildrenAsync(habit);
            return habit;
        }

        public Task UpdateAsync(Habit habit)
            => _repo.SaveWithChildrenAsync(habit);

        public Task MarkDoneAsync(int habitId, DateTime? date = null, double value = 1, string? note = null)
            => _repo.LogAsync(habitId, date, value, note);

        public Task UndoDoneAsync(int habitId, DateTime date)
            => _repo.UnlogAsync(habitId, date);

        public Task ArchiveAsync(int habitId, bool archived = true)
            => _repo.ArchiveAsync(habitId, archived);
    }
}
