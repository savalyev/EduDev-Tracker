using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Tasks
{
    public class TaskService: ITaskService
    {
        private readonly TaskRepository _repo;

        public TaskService(TaskRepository repo)
        {
            _repo = repo;
        }

        public Task SaveAsync(TaskItem task)
            => _repo.SaveAsync(task);

        public Task<List<TaskItem>> GetActiveAsync(int profileId)
            => _repo.GetActiveAsync(profileId);
        public Task<List<TaskItem>> GetByDateAsync(int profileId, DateTime date) 
            => _repo.GetByDateAsync(profileId, date);
        public Task<TaskItem> GetByIdWithChildrenAsync(int id)
            => _repo.GetByIdWithChildrenAsync(id);
        public Task CompleteAsync(int id)
            => _repo.CompleteAsync(id);
        public Task ArchiveAsync(int id, bool archived)
            => _repo.ArchiveAsync(id, archived);
        public Task DeleteAsync(int id)
            => _repo.DeleteAsync(id);
        public Task<TaskRecurrence> GetRecurrenceAsync(int id)
            => _repo.GetRecurrenceAsync(id);
        public Task SaveRecurrenceAsync(TaskRecurrence recurrence)
            => _repo.SaveRecurrenceAsync(recurrence);

        public Task<int> GetTasksClosedWeek(int profileId, DateTime from, DateTime to)
            => _repo.GetTasksClosedWeek(profileId, from, to);
    }
}
