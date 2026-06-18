using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace EduDev_Tracker.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly ReminderRepository _reminderRepo;
        private readonly HabitRepository    _habitRepo;
        private readonly TaskRepository     _taskRepo;

        // Задачи используют диапазон ID 1_000_000+, чтобы не пересекаться с привычками
        private static int TaskNotificationId(int taskId) => 1_000_000 + taskId;

        public NotificationService(
            ReminderRepository reminderRepo,
            HabitRepository habitRepo,
            TaskRepository taskRepo)
        {
            _reminderRepo = reminderRepo;
            _habitRepo    = habitRepo;
            _taskRepo     = taskRepo;
        }

        public async Task RequestPermissionAsync()
            => await LocalNotificationCenter.Current.RequestNotificationPermission();

        // ── Привычки ─────────────────────────────────────────────────────────

        public async Task ScheduleHabitNotificationAsync(Habit habit, HabitSchedule schedule)
        {
            await CancelHabitNotificationAsync(habit.Id);

            if (string.IsNullOrEmpty(schedule.TimeOfDay)) return;
            if (!TimeSpan.TryParse(schedule.TimeOfDay, out var reminderTime)) return;

            var activeDays = GetActiveDays(schedule.DayMask);
            if (activeDays.Count == 0) return;

            foreach (var dayOfWeek in activeDays)
            {
                int notificationId = habit.Id * 10 + (int)dayOfWeek;
                var nextTrigger    = GetNextOccurrence(dayOfWeek, reminderTime);

                var notification = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title          = habit.Title,
                    Description    = string.IsNullOrEmpty(habit.Description)
                                         ? "Пора выполнить привычку!"
                                         : habit.Description,
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = nextTrigger,
                        RepeatType = NotificationRepeat.Weekly,
                    }
                };

                await LocalNotificationCenter.Current.Show(notification);
            }

            // Сохранить факт планирования в БД (первая дата как ориентир)
            var firstDay     = activeDays[0];
            var firstTrigger = GetNextOccurrence(firstDay, reminderTime);

            await _reminderRepo.DeleteByEntityAsync("Habit", habit.Id);
            await _reminderRepo.SaveAsync(new Reminder
            {
                ProfileId  = habit.ProfileId,
                EntityType = "Habit",
                EntityId   = habit.Id,
                FireAt     = firstTrigger,
                Title      = habit.Title,
                Body       = schedule.TimeOfDay,
                CreatedAt  = DateTime.UtcNow
            });
        }

        public async Task CancelHabitNotificationAsync(int habitId)
        {
            for (int day = 0; day < 7; day++)
                LocalNotificationCenter.Current.Cancel(habitId * 10 + day);

            await _reminderRepo.DeleteByEntityAsync("Habit", habitId);
        }

        // ── Задачи ───────────────────────────────────────────────────────────

        public async Task ScheduleTaskReminderAsync(TaskItem task, DateTime fireAt)
        {
            await CancelTaskReminderAsync(task.Id);

            if (fireAt <= DateTime.Now) return;

            var notification = new NotificationRequest
            {
                NotificationId = TaskNotificationId(task.Id),
                Title          = task.Title,
                Description    = string.IsNullOrEmpty(task.Description)
                                     ? "Не забудьте выполнить задачу!"
                                     : task.Description,
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = fireAt,
                    RepeatType = NotificationRepeat.No,
                }
            };

            await LocalNotificationCenter.Current.Show(notification);

            await _reminderRepo.SaveAsync(new Reminder
            {
                ProfileId  = task.ProfileId,
                EntityType = "Task",
                EntityId   = task.Id,
                FireAt     = fireAt,
                Title      = task.Title,
                PlatformId = TaskNotificationId(task.Id),
                CreatedAt  = DateTime.UtcNow
            });
        }

        public async Task CancelTaskReminderAsync(int taskId)
        {
            LocalNotificationCenter.Current.Cancel(TaskNotificationId(taskId));
            await _reminderRepo.DeleteByEntityAsync("Task", taskId);
        }

        // ── Восстановление при старте ─────────────────────────────────────────

        public async Task RescheduleAllPendingAsync(int profileId)
        {
            var reminders = await _reminderRepo.GetAllByProfileAsync(profileId);
            if (reminders.Count == 0) return;

            foreach (var reminder in reminders)
            {
                try
                {
                    if (reminder.EntityType == "Habit")
                    {
                        var habit    = await _habitRepo.GetByIdAsync(reminder.EntityId);
                        var schedule = await _habitRepo.GetScheduleAsync(reminder.EntityId);

                        if (habit is null || habit.IsArchived || habit.IsFrozen) continue;
                        if (schedule is null || string.IsNullOrEmpty(schedule.TimeOfDay)) continue;
                        if (!TimeSpan.TryParse(schedule.TimeOfDay, out var t)) continue;

                        await ReRegisterHabitAsync(habit, schedule, t);
                    }
                    else if (reminder.EntityType == "Task")
                    {
                        var task = await _taskRepo.GetByIdAsync(reminder.EntityId);
                        if (task is null || task.ReminderAt is null) continue;
                        if (task.ReminderAt.Value <= DateTime.Now) continue;
                        if (task.Status == EduDev_Tracker.Data.Models.TaskStatus.Done || task.IsArchived) continue;

                        await ReRegisterTaskAsync(task, task.ReminderAt.Value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notify] Reschedule error {reminder.EntityType}/{reminder.EntityId}: {ex.Message}");
                }
            }
        }

        // Регистрирует уведомления в ОС без изменения БД
        private async Task ReRegisterHabitAsync(Habit habit, HabitSchedule schedule, TimeSpan reminderTime)
        {
            var activeDays = GetActiveDays(schedule.DayMask);
            foreach (var day in activeDays)
            {
                await LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    NotificationId = habit.Id * 10 + (int)day,
                    Title          = habit.Title,
                    Description    = string.IsNullOrEmpty(habit.Description)
                                         ? "Пора выполнить привычку!" : habit.Description,
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = GetNextOccurrence(day, reminderTime),
                        RepeatType = NotificationRepeat.Weekly,
                    }
                });
            }
        }

        private async Task ReRegisterTaskAsync(TaskItem task, DateTime fireAt)
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = TaskNotificationId(task.Id),
                Title          = task.Title,
                Description    = string.IsNullOrEmpty(task.Description)
                                     ? "Не забудьте выполнить задачу!" : task.Description,
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = fireAt,
                    RepeatType = NotificationRepeat.No,
                }
            });
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<DayOfWeek> GetActiveDays(int mask)
        {
            var days = new List<DayOfWeek>();
            if ((mask &  1) != 0) days.Add(DayOfWeek.Monday);
            if ((mask &  2) != 0) days.Add(DayOfWeek.Tuesday);
            if ((mask &  4) != 0) days.Add(DayOfWeek.Wednesday);
            if ((mask &  8) != 0) days.Add(DayOfWeek.Thursday);
            if ((mask & 16) != 0) days.Add(DayOfWeek.Friday);
            if ((mask & 32) != 0) days.Add(DayOfWeek.Saturday);
            if ((mask & 64) != 0) days.Add(DayOfWeek.Sunday);
            return days;
        }

        private static DateTime GetNextOccurrence(DayOfWeek targetDay, TimeSpan time)
        {
            var now        = DateTime.Now;
            int daysUntil  = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;
            if (daysUntil == 0 && now.TimeOfDay >= time) daysUntil = 7;
            return now.Date.AddDays(daysUntil).Add(time);
        }
    }
}
