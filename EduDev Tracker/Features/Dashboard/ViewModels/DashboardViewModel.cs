using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Analytics.Models;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Features.Pomodoro.ViewModels;
using EduDev_Tracker.Features.Tasks.Views;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Pomodoro;
using EduDev_Tracker.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Features.Dashboard.ViewModels
{
    public partial class DashboardViewModel: BaseViewModel
    {
        [ObservableProperty] private string greetingText = "";
        [ObservableProperty] private string dateText = "";

        [ObservableProperty] private double weekProgress = 0;
        [ObservableProperty] private string weekProgressText = "0%";
        [ObservableProperty] private int habitsCompletedWeek = 0;
        [ObservableProperty] private int habitsTargetWeek = 0;
        [ObservableProperty] private int tasksClosedWeek = 0;
        [ObservableProperty] private int pomodoroWeek = 0;

        [ObservableProperty] private int totalHabits = 0;
        [ObservableProperty] private int completedToday = 0;
        [ObservableProperty] private string habitsSubtitle = "Нет активных привычек";
        [ObservableProperty] private bool hasHabits = false;

        [ObservableProperty] private int totalTasks = 0;
        [ObservableProperty] private int overdueTasks = 0;
        [ObservableProperty] private string tasksSubtitle = "Нет активных задач";
        [ObservableProperty] private bool hasTasks = false;

        [ObservableProperty] private List<TodayTaskItem> todayTasks = [];
        [ObservableProperty] private bool hasTodayTasks = false;

        [ObservableProperty] private int todayFocusMinutes = 0;
        [ObservableProperty] private string todayFocusText = "0 мин";
        [ObservableProperty] private int todaySessionsCount = 0;
        [ObservableProperty] private string activePresetLabel = "25 / 5";

        [ObservableProperty] private List<WeekBarPoint> weekChart = [];

        private readonly IHabitService _habitService;
        private readonly ITaskService _taskService;
        private readonly IPomodoroService _pomodoroService;
        private readonly INavigationService _navigationService;
        private int profileId;

        public DashboardViewModel(
            IHabitService habitService,
            ITaskService taskService,
            IPomodoroService pomodoroService,
            INavigationService navigationService)
        {
            UpdateGreeting();
            _habitService = habitService;
            _taskService = taskService;
            _pomodoroService = pomodoroService;
            _navigationService = navigationService;
            profileId = SessionService.GetProfileId();
        }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                await LoadAllAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAllAsync()
        {
            UpdateGreeting();
            await LoadHabitsAsync();
            await LoadTasksAsync();
            await LoadTodayTasksAsync();
            await LoadPomodoroAsync();
            await LoadWeekChartAsync();
            UpdateWeekProgress();
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            GreetingText = hour switch
            {
                < 6 => "Доброй ночи,",
                < 12 => "Доброе утро,",
                < 17 => "Добрый день,",
                < 22 => "Добрый вечер,",
                _ => "Доброй ночи,"
            };
            DateText = DateTime.Now.ToString("dddd, d MMMM",
                new System.Globalization.CultureInfo("ru-RU"));
        }

        private async Task LoadHabitsAsync()
        {
            var activeHabits = await _habitService.GetActiveAsync(profileId);
            TotalHabits = activeHabits.Count;

            if(TotalHabits > 0)
            {
                var progressTasks = activeHabits
                    .Select(h => _habitService.GetTodayProgressAsync(h.Id));
                var progresses = await Task.WhenAll(progressTasks);
                CompletedToday = progresses.Count(p => p > 0);
            }
            else
            {
                CompletedToday = 0;
            }

            HasHabits = TotalHabits > 0;
            HabitsSubtitle = HasHabits
                ? $"Выполнено сегодня: {CompletedToday} из {TotalHabits}"
                : "Добавь первую привычку!";
            HabitsCompletedWeek = 
                await _habitService.GetHabitsCompletedWeek(
                    profileId,
                    DateTime.Today.AddDays(-6),
                    DateTime.Today);

            HabitsTargetWeek = TotalHabits * 7;
        }

        private async Task LoadTasksAsync()
        {

            var allTasks = await _taskService.GetActiveAsync(profileId);

            TotalTasks = allTasks
                .Count(t => t.Status == TaskStatus.Open
                         || t.Status == TaskStatus.InProgress);

            OverdueTasks = allTasks
                .Count(t => t.DueAt < DateTime.Now
                    && t.Status != TaskStatus.Done);

            HasTasks = TotalTasks > 0;
            TasksSubtitle = OverdueTasks > 0
                ? $"⚠ Просрочено: {OverdueTasks}"
                : HasTasks ? $"Открытых задач: {TotalTasks}" : "Нет активных задач";
            TasksClosedWeek = 
                await _taskService.GetTasksClosedWeek(
                    profileId,
                    DateTime.Today.AddDays(-6),
                    DateTime.Today);
        }

        private async Task LoadTodayTasksAsync()
        {
            var allTasks = await _taskService.GetActiveAsync(profileId);

            var todayDate = DateTime.Today;

            var filtered = allTasks
                .Where(t => t.Status != TaskStatus.Done
                         && (t.DueAt == null || t.DueAt.Value.Date == todayDate))
                .OrderByDescending(t => t.Priority)
                .Take(10)
                .ToList();

            TodayTasks = filtered.Select(t => new TodayTaskItem
            {
                Id = t.Id,
                Title = t.Title,
                Priority = GetPriorityLabel(t.Priority),
                PriorityColor = GetPriorityColor(t.Priority),
                IsDone = t.Status == TaskStatus.Done
            }).ToList();


            HasTodayTasks = TodayTasks.Count > 0;
        }

        private static string GetPriorityLabel(TaskPriority p) => p switch
        {
            TaskPriority.Urgent => "Критичный",
            TaskPriority.High => "Высокий",
            TaskPriority.Medium => "Средний",
            TaskPriority.Low => "Низкий",
            _ => "—"
        };

        private static string GetPriorityColor(TaskPriority p) => p switch
        {
            TaskPriority.Urgent => "#EF4444",
            TaskPriority.High   => "#F97316",
            TaskPriority.Medium => "#3B82F6",
            TaskPriority.Low    => "#22C55E",
            _                   => "#64748B"
        };

        private async Task LoadPomodoroAsync()
        {
            var today = DateTime.Today;

            var weekStart = today.AddDays(-6);
            var tomorrow = today.AddDays(1);

            var weekStats = await _pomodoroService
                .GetDailyStatsAsync(profileId, weekStart, tomorrow);

            var todayStats = weekStats
                .Where(s => s.Day == today.ToString("yyyy-MM-dd"))
                .ToList();


            TodayFocusMinutes = todayStats.Sum(s => s.Minutes ?? 0);
            TodaySessionsCount = todayStats.Sum(s => s.Sessions);
            PomodoroWeek = weekStats.Sum(s => s.Sessions);

            TodayFocusText = TodayFocusMinutes >= 60
            ? $"{TodayFocusMinutes / 60} ч {TodayFocusMinutes % 60} мин"
            : $"{TodayFocusMinutes} мин";

            var presets = await _pomodoroService.GetPresetsAsync(profileId);
            var defaultPreset = presets.FirstOrDefault(p => p.IsDefault);
            ActivePresetLabel = defaultPreset is not null
                ? $"{defaultPreset.WorkMinutes} / {defaultPreset.ShortBreakMin}"
                : "25 / 5";
        }

        private async Task LoadWeekChartAsync()
        {
            var dayNames = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
            var today = DateTime.Today;

            var weekStart = today.AddDays(-6);
            var tomorrow = today.AddDays(1);

            var pomodoroStats = await _pomodoroService
                .GetDailyStatsAsync(profileId, weekStart, tomorrow);

            var pomodoroByDay = pomodoroStats
                .ToDictionary(s => s.Day, s => s.Sessions);

            var points = new List<WeekBarPoint>();

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);

                var dayKey = day.ToString("yyyy-MM-dd");

                var pomodoroCount = pomodoroByDay.GetValueOrDefault(dayKey, 0);
                var habitsCount = await _habitService.GetCompletedCountByDayAsync(profileId, day);
                var tasksCount  = await _taskService.GetClosedCountByDayAsync(profileId, day);

                points.Add(new WeekBarPoint
                {
                    DayLabel = dayNames[(int)day.DayOfWeek],
                    IsToday = day == today,
                    HeightRatio = 0,
                    HabitsCount = habitsCount,
                    TasksCount = tasksCount,
                    PomodoroCount = pomodoroCount
                });
            }

            var maxScore = points.Max(p => p.HabitsCount + p.TasksCount * 2 + p.PomodoroCount);
            if (maxScore > 0)
                foreach (var p in points)
                    p.HeightRatio = (double)(p.HabitsCount + p.TasksCount * 2 + p.PomodoroCount) / maxScore;

            WeekChart = points;
        }

        private void UpdateWeekProgress()
        {
            var target = Math.Max(1, HabitsTargetWeek + 7 + 10);
            var current = HabitsCompletedWeek + TasksClosedWeek + PomodoroWeek;
            WeekProgress = Math.Min(1.0, (double)current / target);
            WeekProgressText = $"{(int)(WeekProgress * 100)}%";
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAllAsync();

        [RelayCommand]
        private async Task QuickAddHabitAsync()
        {
            await _navigationService.GoToAsync(nameof(CreateHabitPage));
        }

        [RelayCommand]
        private async Task QuickAddTaskAsync()
        {
            await _navigationService.GoToAsync(nameof(AddTaskPage));
        }

        [RelayCommand]
        private async Task StartPomodoroAsync()
        {
            await _navigationService.SwitchToModuleAsync("FlyoutPomodoro");
        }

        [RelayCommand]
        private async Task GoToHabitsAsync() => await _navigationService.SwitchToModuleAsync("FlyoutHabits");

        [RelayCommand]
        private async Task GoToTasksAsync() => await _navigationService.SwitchToModuleAsync("FlyoutTasks");

        [RelayCommand]
        private async Task GoToAnalyticsAsync() => await _navigationService.SwitchToModuleAsync("FlyoutAnalytics");
    }

    public class WeekBarPoint
    {
        public string DayLabel { get; set; } = "";
        public bool IsToday { get; set; }
        public double HeightRatio { get; set; }
        public int HabitsCount { get; set; }
        public int TasksCount { get; set; }
        public int PomodoroCount { get; set; }
        public double BarHeight => Math.Max(4, HeightRatio * 130);
        public Color BarColor => IsToday
            ? Color.FromArgb("#019301")
            : Color.FromArgb("#1E3A5F");
        public Color DayLabelColor => IsToday
            ? Color.FromArgb("#019301")
            : Color.FromArgb("#FFFFFF");
    }

    public class TodayTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Priority { get; set; } = "";
        public string PriorityColor { get; set; } = "#64748B";
        public bool IsDone { get; set; }
    }
}
