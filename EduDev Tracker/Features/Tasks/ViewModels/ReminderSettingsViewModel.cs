using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notification;
using EduDev_Tracker.Services.Tasks;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class ReminderSettingsViewModel : BaseViewModel
    {
        private readonly ITaskService         _taskService;
        private readonly INotificationService _notificationService;
        private readonly INavigationService   _navigation;

        private int _taskId;

        [ObservableProperty] private string taskTitle        = "";
        [ObservableProperty] private DateTime reminderDate   = DateTime.Now.Date.AddDays(1);
        [ObservableProperty] private TimeSpan reminderTime   = TimeSpan.FromHours(9);
        [ObservableProperty] private bool   hasReminder      = false;
        [ObservableProperty] private string existingLabel    = "";

        public ReminderSettingsViewModel(
            ITaskService taskService,
            INotificationService notificationService,
            INavigationService navigation)
        {
            _taskService         = taskService;
            _notificationService = notificationService;
            _navigation          = navigation;
        }

        public async Task InitializeAsync(int taskId)
        {
            _taskId = taskId;

            var task = await _taskService.GetByIdWithChildrenAsync(taskId);
            if (task is null) return;

            TaskTitle = task.Title;

            if (task.ReminderAt.HasValue && task.ReminderAt.Value > DateTime.Now)
            {
                HasReminder    = true;
                ReminderDate   = task.ReminderAt.Value.Date;
                ReminderTime   = task.ReminderAt.Value.TimeOfDay;
                ExistingLabel  = task.ReminderAt.Value.ToString("d MMMM yyyy, HH:mm",
                                     new System.Globalization.CultureInfo("ru-RU"));
            }
            else
            {
                HasReminder   = false;
                ExistingLabel = "";
                // Предлагаем дату по умолчанию: срок задачи если есть, иначе завтра 09:00
                if (task.DueAt.HasValue && task.DueAt.Value > DateTime.Now)
                {
                    ReminderDate = task.DueAt.Value.Date;
                    ReminderTime = task.DueAt.Value.Hour > 0
                        ? task.DueAt.Value.TimeOfDay
                        : TimeSpan.FromHours(9);
                }
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var fireAt = ReminderDate.Date.Add(ReminderTime);
                if (fireAt <= DateTime.Now)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Время напоминания должно быть в будущем.",
                        "OK");
                    return;
                }

                var task = await _taskService.GetByIdWithChildrenAsync(_taskId);
                if (task is null) return;

                task.ReminderAt = fireAt;
                await _taskService.SaveAsync(task);
                await _notificationService.ScheduleTaskReminderAsync(task, fireAt);

                await _navigation.GoBackAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RemoveReminderAsync()
        {
            if (IsBusy) return;

            var task = await _taskService.GetByIdWithChildrenAsync(_taskId);
            if (task is null) return;

            task.ReminderAt = null;
            await _taskService.SaveAsync(task);
            await _notificationService.CancelTaskReminderAsync(_taskId);

            HasReminder   = false;
            ExistingLabel = "";

            await _navigation.GoBackAsync();
        }

        [RelayCommand]
        private async Task CancelAsync()
            => await _navigation.GoBackAsync();
    }
}
