using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
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
        [NotifyPropertyChangedFor(nameof(IsEditingRecurrence))]
        [NotifyPropertyChangedFor(nameof(EditButtonText))]
        private bool isEditing;

        public bool IsNotEditing => !IsEditing;
        public bool IsEditingRecurrence => IsEditing && IsRecurring;
        public string EditButtonText => IsEditing ? "Отмена" : "Редактировать";

        [RelayCommand]
        private void ToggleEdit()
        {
            if (IsEditing) LoadFromTask(_task); // откат изменений
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

        public RecurrenceHelper Recurrence { get; } = new();

        public bool IsRecurring { get => Recurrence.IsRecurring; set => Recurrence.IsRecurring = value; }
        public bool HasRecurrence => Recurrence.IsRecurring && !IsEditing;
        public string RecurrenceSummary => Recurrence.RecurrenceSummary;
        public string SelectedRuleType { get => Recurrence.SelectedRuleType; set => Recurrence.SelectedRuleType = value; }
        public double IntervalN { get => Recurrence.IntervalN; set => Recurrence.IntervalN = value; }
        public double DayOfMonth { get => Recurrence.DayOfMonth; set => Recurrence.DayOfMonth = value; }
        public bool IsWeekly => Recurrence.IsWeekly;
        public bool IsMonthly => Recurrence.IsMonthly;
        public string IntervalLabel => Recurrence.IntervalLabel;
        public string MonColor => Recurrence.MonColor; public string MonBorder => Recurrence.MonBorder;
        public string TueColor => Recurrence.TueColor; public string TueBorder => Recurrence.TueBorder;
        public string WedColor => Recurrence.WedColor; public string WedBorder => Recurrence.WedBorder;
        public string ThuColor => Recurrence.ThuColor; public string ThuBorder => Recurrence.ThuBorder;
        public string FriColor => Recurrence.FriColor; public string FriBorder => Recurrence.FriBorder;
        public string SatColor => Recurrence.SatColor; public string SatBorder => Recurrence.SatBorder;
        public string SunColor => Recurrence.SunColor; public string SunBorder => Recurrence.SunBorder;

        [ObservableProperty] private string nextDueText = string.Empty;

        [RelayCommand] private void SelectRuleType(string t) => Recurrence.SelectedRuleType = t;
        [RelayCommand] private void ToggleDay(string bit) => Recurrence.ToggleDayCommand.Execute(bit);


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

            Recurrence.LoadFromRecurrence(await _taskService.GetRecurrenceAsync(task.Id));
            if (task.Recurrence?.NextDue != null)
                NextDueText = $"Следующее: {task.Recurrence.NextDue:d MMMM yyyy}";
        }

        [RelayCommand]
        private async Task ChangeStatus(string status)
        {
            if (_task is null) return;
            CurrentStatus = status;
            _task.Status = Enum.Parse<TaskStatus>(status);
            _task.UpdatedAt = DateTime.UtcNow;
            if (_task.Status == TaskStatus.Done) _task.CompletedAt = DateTime.UtcNow;

            await _taskService.SaveAsync(_task);
        }

        [RelayCommand]
        private async Task ChangePriority(string priority)
        {
            if (_task is null) return;
            CurrentPriority = priority;
            _task.Priority = Enum.Parse<TaskPriority>(priority);
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
                _task.Priority = Enum.Parse<TaskPriority>(CurrentPriority);
                _task.Status = Enum.Parse<TaskStatus>(CurrentStatus);
                _task.DueAt = DueDate.Date + DueTime;
                _task.Recurrence = Recurrence.BuildRecurrence(_task.Id);
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
