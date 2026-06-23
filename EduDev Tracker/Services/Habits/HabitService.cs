using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            string? description = null,
            string? icon = null)
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
                Icon = icon ?? "default_icon.png",
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

        public Task FreezeAsync(int habitId, bool freeze = true) 
            => _repo.FreezeAsync(habitId, freeze);
        public Task<bool> IsCompletedTodayAsync(int habitId)
            => _repo.IsCompletedTodayAsync(habitId);
        public Task<bool> IsCompletedTodayAsync(int habitId, DateTime date)
            => _repo.IsCompletedTodayAsync(habitId, date);
        public Task DeleteAsync(int habitId)
            => _repo.DeleteAsync(habitId);
        public Task<int> GetCurrentStreakAsync(int habitId)
            => _repo.GetCurrentStreakAsync(habitId);
        public Task<double> GetTodayProgressAsync(int habitId)
            => _repo.GetTodayProgressAsync(habitId);
        public Task<Habit> GetByIdWithChildrenAsync(int id)
            => _repo.GetByIdWithChildrenAsync(id);
        public Task<List<Habit>> GetArchivedAsync(int profileId)
            => _repo.GetArchivedAsync(profileId);
        public Task<int> GetHabitsCompletedWeek(int profileId, DateTime from, DateTime to)
            => _repo.GetHabitsCompletedWeek(profileId, from, to);
        public Task<int> GetCompletedCountByDayAsync(int profileId, DateTime date)
            => _repo.GetCompletedCountByDayAsync(profileId, date);

        public async Task<List<(Habit habit, bool isCompleted)>> GetHabitsWithStatusForDateAsync(int profileId, DateTime date)
        {
            var allHabits = await _repo.GetActiveAsync(profileId);
            var dayMask = DayOfWeekToMask(date.DayOfWeek);
            var result = new List<(Habit, bool)>();

            foreach (var habit in allHabits)
            {
                var schedule = await _repo.GetScheduleAsync(habit.Id);
                if (schedule == null || (schedule.DayMask & dayMask) == 0)
                    continue;

                var isCompleted = await _repo.IsCompletedTodayAsync(habit.Id, date);
                result.Add((habit, isCompleted));
            }

            return result;
        }

        private static int DayOfWeekToMask(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday    => 1,
            DayOfWeek.Tuesday   => 2,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday  => 8,
            DayOfWeek.Friday    => 16,
            DayOfWeek.Saturday  => 32,
            DayOfWeek.Sunday    => 64,
            _ => 0
        };
    }
}
