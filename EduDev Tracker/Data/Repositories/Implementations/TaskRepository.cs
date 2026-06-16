using EduDev_Tracker.Data.Models;
using SQLiteNetExtensionsAsync.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class TaskRepository: BaseRepository<TaskItem>
    {
        public TaskRepository(DatabaseService db) : base(db) { }

        public async Task<List<TaskItem>> GetActiveAsync(int profileId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[GetActiveAsync] Start");

                var result = await Connection.Table<TaskItem>()
                    .Where(h => h.ProfileId == profileId && !h.IsArchived)
                    .OrderBy(h => h.DueAt)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[GetActiveAsync] Got {result.Count} items");

                foreach (var t in result)
                    System.Diagnostics.Debug.WriteLine($"[GetActiveAsync] Task Id={t.Id} Title={t.Title} DueAt={t.DueAt}");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetActiveAsync] CRASH: {ex}");
                throw;
            }
        }

        public Task<List<TaskItem>> GetByDateAsync(int profileId, DateTime date)
        {
            var start = date.Date.ToString("o");
            var end = date.Date.AddDays(1).ToString("o");

            return Connection.QueryAsync<TaskItem>(
                "SELECT * FROM tasks WHERE ProfileId = ? AND DueAt >= ? AND DueAt < ?" +
                " AND IsArchived = 0",
                profileId, start, end);
        }

        public Task<TaskItem> GetByIdWithChildrenAsync(int id)
        {
            return Connection.GetWithChildrenAsync<TaskItem>(id, recursive: true);
        }

        public async Task CompleteAsync(int id)
        {
            await Connection.ExecuteAsync(
                "UPDATE tasks SET Status = ?, UpdatedAt = ? WHERE Id = ?",
                TaskStatus.Done.ToString(), DateTime.UtcNow, id);
        }

        public async Task ArchiveAsync(int id, bool archived = true)
        {
            await Connection.ExecuteAsync(
                "UPDATE tasks SET IsArchived = ?, UpdatedAt = ? WHERE Id = ?",
                archived ? 1 : 0, DateTime.UtcNow, id);
        }

        public async Task DeleteAsync(int id)
        {
            await Connection.ExecuteAsync(
                "DELETE FROM tasks WHERE Id = ?",
                id);
        }

        public Task<TaskRecurrence> GetRecurrenceAsync(int id)
        {
            return Connection.Table<TaskRecurrence>().FirstOrDefaultAsync(h => h.TaskId == id);
        }

        public Task SaveRecurrenceAsync(TaskRecurrence recurrence)
        {
            return Connection.InsertOrReplaceAsync(recurrence);
        }

        public async Task<int> GetTasksClosedWeek(int profileId, DateTime from, DateTime to)
        {
            var result = await Connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM tasks WHERE ProfileId=? AND Status='Done' " +
                "AND CompletedAt BETWEEN ? AND ?",
                profileId, from, to);

            return result;
        }
    }
}
