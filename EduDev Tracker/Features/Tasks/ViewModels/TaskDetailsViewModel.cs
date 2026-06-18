using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Tasks.Views;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class TaskDetailsViewModel: BaseViewModel
    {
        private readonly ITaskService _taskService;
        private readonly INavigationService _navigation;
        private TaskItem _task = null;

        public TaskDetailsViewModel(ITaskService taskService,
            INavigationService navigation)
        {
            _taskService = taskService;
            _navigation = navigation;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditing))]
        [NotifyPropertyChangedFor(nameof(EditButtonText))]
        private bool isEditing;

        public bool IsNotEditing => !IsEditing;
        public string EditButtonText => IsEditing ? "Отмена" : "Редактировать";

        [RelayCommand]
        private async Task ToggleEdit()
        {
            if (IsEditing) await LoadFromTask(_task); // откат изменений
            IsEditing = !IsEditing;
        }

        [ObservableProperty] private string taskTitle = string.Empty;
        [ObservableProperty] private string description = string.Empty;
        [ObservableProperty] private string category = string.Empty;
        [ObservableProperty] private string createdAtText = string.Empty;
        [ObservableProperty] private string deadlineText = string.Empty;
        [ObservableProperty] private string deadlineTextColor = "#99FFFFFF";
        [ObservableProperty] private DateTime dueDate = DateTime.Today;
        [ObservableProperty] private TimeSpan dueTime = TimeSpan.Zero;
        [ObservableProperty] private string currentStatus = "Open";
        [ObservableProperty] private string currentPriority = "Medium";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArchiveButtonText))]
        private bool isArchived;

        public string ArchiveButtonText => IsArchived ? "Разархивировать" : "Архивировать";

        private RecurrenceHelper Recurrence { get; } = new();

        public bool HasRecurrence => Recurrence.IsRecurring;
        public string RecurrenceSummary => Recurrence.RecurrenceSummary;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNextDueText))]
        private string nextDueText = string.Empty;

        public bool HasNextDueText => !string.IsNullOrEmpty(NextDueText);


        [RelayCommand]
        private Task Init() => Task.CompletedTask;

        public async Task InitializeAsync(int taskId)
        {
            IsBusy = true;
            try
            {
                _task = await _taskService.GetByIdWithChildrenAsync(taskId);
                if (_task == null) return; 
                LoadFromTask(_task);
            }
            finally { IsBusy = false; }
        }

        private async Task LoadFromTask(TaskItem? task)
        {
            if (task is null) return;
            TaskTitle = task.Title;
            Description = task.Description ?? string.Empty;
            Category = task.Category ?? string.Empty;
            CurrentStatus = task.Status.ToString();
            CurrentPriority = task.Priority.ToString();
            IsArchived = task.IsArchived;
            CreatedAtText = $"Создана {task.CreatedAt:d MMMM yyyy}";

            if (task.DueAt.HasValue)
            {
                DueDate = task.DueAt.Value.Date;
                DueTime = task.DueAt.Value.TimeOfDay;
                var isOverdue = task.DueAt < DateTime.Now && task.Status != TaskStatus.Done;
                DeadlineText = isOverdue
                    ? $"{task.DueAt:d MMMM, HH:mm} (просрочено)"
                    : task.DueAt.Value.ToString("d MMMM yyyy, HH:mm");
                DeadlineTextColor = isOverdue ? "#FF6B6B" : "#CCFFFFFF";
            }
            else
            {
                DeadlineText = "Без срока";
                DeadlineTextColor = "#99FFFFFF";
            }

            var rec = await _taskService.GetRecurrenceAsync(task.Id);
            Recurrence.LoadFromRecurrence(rec);
            NextDueText = rec?.NextDue != null
                ? $"Следующее: {rec.NextDue:d MMMM yyyy}"
                : string.Empty;
        }

        [RelayCommand]
        private async Task OpenReminder()
        {
            if (_task is null) return;
            await _navigation.GoToAsync($"{nameof(ReminderSettingsPage)}?taskId={_task.Id}");
        }

        [RelayCommand]
        private async Task ChangeStatus(string status)
        {
            if (_task is null) return;
            CurrentStatus = status;
            _task.Status = status switch
            {
                "InProgress" => TaskStatus.InProgress,
                "Done"       => TaskStatus.Done,
                "Cancelled"  => TaskStatus.Cancelled,
                _            => TaskStatus.Open,
            };
            _task.UpdatedAt = DateTime.UtcNow;
            if (_task.Status == TaskStatus.Done) _task.CompletedAt = DateTime.UtcNow;

            await _taskService.SaveAsync(_task);
        }

        [RelayCommand]
        private async Task ChangePriority(string priority)
        {
            if (_task is null) return;
            CurrentPriority = priority;
            _task.Priority = priority switch
            {
                "Urgent" => TaskPriority.Urgent,
                "High"   => TaskPriority.High,
                "Low"    => TaskPriority.Low,
                _        => TaskPriority.Medium,
            };
            _task.UpdatedAt = DateTime.UtcNow;
            await _taskService.SaveAsync(_task);
        }

        [RelayCommand]
        private async Task Save()
        {
            if (_task is null || IsBusy) return;
            IsBusy = true;
            try
            {
                _task.Title = TaskTitle.Trim();
                _task.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                _task.Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim();
                _task.Priority = CurrentPriority switch
                {
                    "Urgent" => TaskPriority.Urgent,
                    "High"   => TaskPriority.High,
                    "Low"    => TaskPriority.Low,
                    _        => TaskPriority.Medium,
                };
                _task.Status = CurrentStatus switch
                {
                    "InProgress" => TaskStatus.InProgress,
                    "Done"       => TaskStatus.Done,
                    "Cancelled"  => TaskStatus.Cancelled,
                    _            => TaskStatus.Open,
                };
                _task.DueAt = DueDate.Date + DueTime;
                _task.UpdatedAt = DateTime.UtcNow;

                await _taskService.SaveAsync(_task);
                IsEditing = false;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task Archive()
        {
            if (_task is null) return;
            IsArchived = !IsArchived;
            await _taskService.ArchiveAsync(_task.Id, isArchived);
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (_task is null) return;
            bool confirmation = await Application.Current.MainPage.DisplayAlertAsync("Подтвердите свои действия",
                "Вы уверены в удалении задачи?", "Да", "Нет");
            if (confirmation)
            {
                await _taskService.DeleteAsync(_task.Id);
            }
            await CloseAsync();
        }

        [RelayCommand]
        private async Task Close() => await CloseAsync();

        private async Task CloseAsync()
        {
            await _navigation.GoBackAsync();
        }
    }
}
