using EduDev_Tracker.Data.Models;
using SQLiteNetExtensionsAsync.Extensions;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class HabitRepository: BaseRepository<Habit>
    {
        public HabitRepository(DatabaseService db) : base(db) { }

        public Task<List<Habit>> GetActiveAsync(int profileId)
        {
            return Connection.Table<Habit>()
                .Where(h => h.ProfileId == profileId && !h.IsArchived)
                .OrderBy(h => h.SortOrder)
                .ToListAsync();
        }

        public Task<List<Habit>> GetArchivedAsync(int profileId)
            => Connection.Table<Habit>()
                .Where(h => h.ProfileId == profileId && h.IsArchived)
                .OrderByDescending(h => h.UpdatedAt)
                .ToListAsync();

        public Task<Habit> GetByIdWithChildrenAsync(int id)
            => Connection.GetWithChildrenAsync<Habit>(id, recursive: true);

        public Task<HabitSchedule> GetScheduleAsync(int habitId)
        {
            return Connection.Table<HabitSchedule>().FirstOrDefaultAsync(h => h.HabitId == habitId);
        }

        public async Task<int> LogAsync(int habitId, DateTime? date = null, double value = 1, string? note = null)
        {
            var dateKey = (date ?? DateTime.Now.Date).ToString("yyyy-MM-dd");

            await Connection.ExecuteAsync(@"
                INSERT INTO habit_logs (HabitId, LogDate, Value, Note, CompletedAt)
                VALUES (?, ?, ?, ?, ?)
                ON CONFLICT(HabitId, LogDate) DO UPDATE SET
                Value = excluded.Value,
                Note = excluded.Note,
                CompletedAt = excluded.CompletedAt;", habitId, dateKey, value, note, DateTime.UtcNow);

            return 1;
        }

        public async Task ArchiveAsync(int habitId, bool archived = true)
        {
            await Connection.ExecuteAsync(
                "UPDATE habits SET IsArchived = ?, UpdatedAt = ? WHERE Id = ?",
                archived ? 1 : 0, DateTime.UtcNow, habitId);
        }

        public async Task FreezeAsync(int habitId, bool freeze = true)
        {
            await Connection.ExecuteAsync(
                "UPDATE habits SET IsFrozen = ?, UpdatedAt = ? WHERE Id = ?",
                freeze ? 1 : 0, DateTime.UtcNow, habitId);
        }

        public Task<int> UnlogAsync(int habitId, DateTime date)
        {
            var dateKey = date.Date.ToString("yyyy-MM-dd");
            return Connection.ExecuteAsync(
                "DELETE FROM habit_logs WHERE HabitId = ? AND LogDate = ?",
                habitId, dateKey);
        }

        public async Task SaveWithChildrenAsync(Habit habit)
        {
            habit.UpdatedAt = DateTime.UtcNow;

            if(habit.Id == 0)
            {
                habit.CreatedAt = DateTime.UtcNow;
                await Connection.InsertWithChildrenAsync(habit, recursive:  true);
            }
            else
            {
                await Connection.UpdateAsync(habit);
            }
        }

        public async Task<bool> IsCompletedTodayAsync(int habitId)
        {
            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var cnt = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM habit_logs WHERE HabitId = ? AND LogDate = ?",
                habitId, today);
            return cnt > 0;
        }

        public async Task<bool> IsCompletedTodayAsync(int habitId, DateTime date)
        {
            var logDate = date.ToString("yyyy-MM-dd");
            var cnt = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM habit_logs WHERE HabitId = ? AND LogDate = ?",
                habitId, logDate);
            return cnt > 0;
        }

        public Task DeleteAsync(int habitId)
            => Connection.ExecuteAsync("DELETE FROM habits WHERE Id = ?", habitId);

        public async Task<int> GetCurrentStreakAsync(int habitId)
        {
            var schedule = await GetScheduleAsync(habitId);
            if (schedule == null) return 0;

            var logs = await Connection.QueryAsync<HabitLog>(
                "SELECT DISTINCT LogDate FROM habit_logs WHERE HabitId = ? ORDER BY LogDate DESC",
                habitId);

            if (logs == null || logs.Count == 0) return 0;

            var completedDates = logs
                .Select(l => DateTime.Parse(l.LogDate).Date)
                .ToHashSet();

            int streak = 0;
            var checkDate = DateTime.Today;

            bool todayScheduled = IsDateScheduled(checkDate, schedule.DayMask);
            bool todayCompleted = completedDates.Contains(checkDate);

            if (todayScheduled && !todayCompleted)
                checkDate = checkDate.AddDays(-1);

            while (true)
            {
                bool scheduled = IsDateScheduled(checkDate, schedule.DayMask);

                if (!scheduled)
                {
                    checkDate = checkDate.AddDays(-1);

                    if ((DateTime.Today - checkDate).TotalDays > 365) break;
                    continue;
                }

                if (!completedDates.Contains(checkDate))
                    break;

                streak++;
                checkDate = checkDate.AddDays(-1);

                if ((DateTime.Today - checkDate).TotalDays > 365) break;
            }

            return streak;
        }

        private bool IsDateScheduled(DateTime date, int dayMask)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => (dayMask & 1) != 0,
                DayOfWeek.Tuesday => (dayMask & 2) != 0,
                DayOfWeek.Wednesday => (dayMask & 4) != 0,
                DayOfWeek.Thursday => (dayMask & 8) != 0,
                DayOfWeek.Friday => (dayMask & 16) != 0,
                DayOfWeek.Saturday => (dayMask & 32) != 0,
                DayOfWeek.Sunday => (dayMask & 64) != 0,
                _ => false
            };
        }

        public async Task<double> GetTodayProgressAsync(int habitId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            var result = await Connection.ExecuteScalarAsync<double>(
                "SELECT COALESCE(SUM(Value), 0) FROM habit_logs WHERE HabitId = ? AND LogDate = ?",
                habitId, today);

            return result;
        }


    }
}
