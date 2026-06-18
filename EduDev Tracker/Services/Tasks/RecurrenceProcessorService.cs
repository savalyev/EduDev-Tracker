using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Tasks
{
    public class RecurrenceProcessorService
    {
        private readonly TaskRepository _taskRepo;

        public RecurrenceProcessorService(TaskRepository taskRepo)
        {
            _taskRepo = taskRepo;
        }

        public async Task ProcessAsync(int profileId)
        {
            var today = DateTime.Today;

            var tasks = await _taskRepo.GetActiveAsync(profileId);

            foreach (var task in tasks)
            {
                var rec = await _taskRepo.GetRecurrenceAsync(task.Id);
                if (rec == null) continue;

                var nextDue = rec.NextDue ?? RecurrenceCalculator.CalcFirstDue(rec, task.DueAt?.Date ?? today);
                if (nextDue.Date > today) continue;

                if (rec.EndDate.HasValue && today > rec.EndDate.Value) continue;

                await SpawnNextTaskAsync(task, rec, nextDue);

                rec.NextDue = RecurrenceCalculator.CalcNext(rec, nextDue);
                await _taskRepo.SaveRecurrenceAsync(rec);
            }
        }

        private async Task SpawnNextTaskAsync(TaskItem original, TaskRecurrence rec, DateTime dueAt)
        {
            var newTask = new TaskItem
            {
                ProfileId = original.ProfileId,
                ProjectId = original.ProjectId,
                Title = original.Title,
                Description = original.Description,
                Category = original.Category,
                Priority = original.Priority,
                Status = Data.Models.TaskStatus.Open,
                DueAt = dueAt,
                SortOrder = original.SortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _taskRepo.SaveAsync(newTask);
            // Порождённый экземпляр — просто задача без recurrence.
            // Только шаблон (original) управляет расписанием через своё TaskRecurrence.
        }
    }
}
