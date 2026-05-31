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
                await Connection.UpdateWithChildrenAsync(habit);
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

        public async Task<int> GetStreakAsync(int habitId)
        {
            var logs = await Connection.QueryAsync<HabitLog>(
                "SELECT LogDate FROM habit_logs WHERE HabitId = ? ORDER BY LogDate DESC",
                habitId);

            if (logs.Count == 0) return 0;

            int streak = 0;
            var expected = DateTime.Today;

            foreach (var log in logs)
            {
                if (!DateTime.TryParse(log.LogDate, out var logDate)) break;

                if (logDate.Date == expected.Date)
                {
                    streak++;
                    expected = expected.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }


    }
}
