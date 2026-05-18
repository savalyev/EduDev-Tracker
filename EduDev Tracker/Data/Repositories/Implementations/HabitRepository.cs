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

        public async Task<int> LogAsync(int habitId, DateTime? date = null, double value = 1, string? note = null)
        {
            var d = (date ?? DateTime.Now).Date;
            var dateKey = d.ToString("yyyy-MM-dd");

            await Connection.ExecuteAsync(@"
INSERT INTO habit_logs (HabitId, LogDate, Value, Note, CompletedAt
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
    }
}
