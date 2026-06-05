using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notification;
using EduDev_Tracker.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class AddTaskViewModel: BaseViewModel
    {
        private readonly ITaskService _taskService;
        private readonly INavigationService _navigation;
        private readonly INotificationService _notification;
        private int _profileId;

        public AddTaskViewModel(ITaskService taskService, INavigationService navigationService, INotificationService notification)
        {
            _taskService = taskService;
            _navigation = navigationService;
            _notification = notification;

            Recurrence.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
        }

        [ObservableProperty] private string taskTitle = string.Empty;
        [ObservableProperty] private string description = string.Empty;
        [ObservableProperty] private string category = string.Empty;
        [ObservableProperty] private string selectedPriority = "Medium";
        [ObservableProperty] private DateTime dueDate = DateTime.Today;
        [ObservableProperty] private TimeSpan dueTime = new TimeSpan(23, 59, 0);

        public DateTime Today => DateTime.Today;

        public RecurrenceHelper Recurrence { get; } = new();
        public bool IsRecurring { get => Recurrence.IsRecurring; set => Recurrence.IsRecurring = value; }
        public string SelectedRuleType { get => Recurrence.SelectedRuleType; set => Recurrence.SelectedRuleType = value; }
        public double IntervalN { get => Recurrence.IntervalN; set => Recurrence.IntervalN = value; }
        public double DayOfMonth { get => Recurrence.DayOfMonth; set => Recurrence.DayOfMonth = value; }
        public int DaysMask { get => Recurrence.DaysMask; set => Recurrence.DaysMask = value; }
        public DateTime RecurrenceEndDate { get => Recurrence.RecurrenceEndDate; set => Recurrence.RecurrenceEndDate = value; }
        public bool IsWeekly => Recurrence.IsWeekly;
        public bool IsMonthly => Recurrence.IsMonthly;
        public string RecurrenceSummary => Recurrence.RecurrenceSummary;
        public string RecurringHint => Recurrence.RecurringHint;
        public string IntervalLabel => Recurrence.IntervalLabel;
        public string MonColor => Recurrence.MonColor; public string MonBorder => Recurrence.MonBorder;
        public string TueColor => Recurrence.TueColor; public string TueBorder => Recurrence.TueBorder;
        public string WedColor => Recurrence.WedColor; public string WedBorder => Recurrence.WedBorder;
        public string ThuColor => Recurrence.ThuColor; public string ThuBorder => Recurrence.ThuBorder;
        public string FriColor => Recurrence.FriColor; public string FriBorder => Recurrence.FriBorder;
        public string SatColor => Recurrence.SatColor; public string SatBorder => Recurrence.SatBorder;
        public string SunColor => Recurrence.SunColor; public string SunBorder => Recurrence.SunBorder;

        [RelayCommand] private void SelectPriority(string p) => SelectedPriority = p;
        [RelayCommand] private void SelectRuleType(string t) => Recurrence.SelectedRuleType = t;
        [RelayCommand] private void ToggleDay(string bit) => Recurrence.ToggleDayCommand.Execute(bit);

        [RelayCommand]
        private Task Init() => Task.CompletedTask;

        public override Task InitializeAsync(IDictionary<string, object>? query = null)
        {
            if (query?.TryGetValue("profileId", out var pid) == true)
                _profileId = Convert.ToInt32(pid);
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!await Validate())
            {
                return;
            }
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var dueAt = DueDate.Date + DueTime;

                var task = new TaskItem
                {
                    ProfileId = _profileId,
                    Title = TaskTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                    Priority = Enum.Parse<TaskPriority>(SelectedPriority),
                    Status = Data.Models.TaskStatus.Open,
                    DueAt = dueAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };


                await Shell.Current.DisplayAlertAsync("Успех!", "Задача создана!", "ОК");
                await _taskService.SaveAsync(task);

                if (Recurrence.IsRecurring)
                {
                    try
                    {
                        var rec = Recurrence.BuildRecurrence(task.Id);
                        rec.NextDue = RecurrenceCalculator.CalcFirstDue(rec, DueDate);
                        await _taskService.SaveRecurrenceAsync(rec);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CalcFirstDue] {ex}");
                        throw;
                    }
                }
                await CloseAsync();
            }
            finally { IsBusy = false; }
        }

        private async Task<bool> Validate()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Название задачи обязательно", "ОК");
                valid = false;
            }
            else if(TaskTitle.Length < 3)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Название задачи должно быть больше 3 символов", "ОК");
                valid = false;
            }

            if(DueDate.Date + DueTime < DateTime.Now)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Дедлайн не может существовать в прошлом", "ОК");
                valid = false;
            }

            return valid;
        }

        [RelayCommand]
        private async Task Close() => await CloseAsync();

        private async Task CloseAsync()
        {
            await _navigation.PopModalAsync();
        }

    }
}
