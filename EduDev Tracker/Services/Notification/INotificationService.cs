using EduDev_Tracker.Data.Models;

namespace EduDev_Tracker.Services.Notification
{
    public interface INotificationService
    {
        Task RequestPermissionAsync();

        // Вызывается при каждом старте приложения — восстанавливает уведомления из БД
        Task RescheduleAllPendingAsync(int profileId);

        // Привычки — повторяющиеся еженедельные напоминания
        Task ScheduleHabitNotificationAsync(Habit habit, HabitSchedule schedule);
        Task CancelHabitNotificationAsync(int habitId);

        // Задачи — одноразовое напоминание
        Task ScheduleTaskReminderAsync(TaskItem task, DateTime fireAt);
        Task CancelTaskReminderAsync(int taskId);
    }
}
