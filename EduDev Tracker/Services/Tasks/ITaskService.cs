using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Tasks
{
    public interface ITaskService
    {
        public Task<List<TaskItem>> GetActiveAsync(int profileId);
        public Task<List<TaskItem>> GetByDateAsync(int profileId, DateTime date);
        public Task<TaskItem> GetByIdWithChildrenAsync(int id);
        public Task CompleteAsync(int id);
        public Task ArchiveAsync(int id, bool archived = true);
        public Task DeleteAsync(int id);
        public Task<TaskRecurrence> GetRecurrenceAsync(int id);
        public Task SaveAsync(TaskItem task);
        public Task SaveRecurrenceAsync(TaskRecurrence recurrence);
        public Task DeleteRecurrenceAsync(int taskId);
        public Task<int> GetTasksClosedWeek(int profileId, DateTime from, DateTime to);
        Task<int> GetClosedCountByDayAsync(int profileId, DateTime date);
    }
}
