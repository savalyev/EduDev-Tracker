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

        public Task<List<TaskItem>> GetActiveAsync(int profileId)
        {
            return Connection.Table<TaskItem>()
                .Where(h => h.ProfileId == profileId && !h.IsArchived)
                .OrderBy(h => h.DueAt)
                .ToListAsync();
        }

        public Task<List<TaskItem>> GetByDateAsync(int profileId, DateTime date)
        {
            var start = date.Date.ToString("o");
            var end = date.Date.AddDays(1).ToString("o");

            return Connection.QueryAsync<TaskItem>(
                "SELECT * FROM tasks WHERE ProfileId = ? AND DueAt >= ? AND DueAt < ?" +
                "AND IsArchived = 0",
                profileId, start, end);
        }

        public Task<TaskItem> GetByIdWithChildrenAsync(int id)
        {
            return Connection.GetWithChildrenAsync<TaskItem>(id, recursive: true);
        }

        public async Task SaveWithChildrenAsync(TaskItem task)
        {
            task.UpdatedAt = DateTime.UtcNow;

            if (task.Id == 0)
            {
                task.CreatedAt = DateTime.UtcNow;
                await Connection.InsertWithChildrenAsync(task, recursive: true);
            }
            else
            {
                await Connection.UpdateWithChildrenAsync(task);
            }
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
    }
}
