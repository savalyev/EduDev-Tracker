using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Analytics.Models;
using EduDev_Tracker.Services.Analytics;
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
        private readonly IAnalyticsService _analyticsService;
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
                < 6 => "Доброй ночи 🌙",
                < 12 => "Доброе утро ☀️",
                < 17 => "Добрый день 👋",
                < 22 => "Добрый вечер 🌆",
                _ => "Доброй ночи 🌙"
            };
            DateText = DateTime.Now.ToString("dddd, d MMMM",
                new System.Globalization.CultureInfo("ru-RU"));
        }

        private async Task LoadHabitsAsync()
        {
            var allActiveHabits = await _habitService.GetActiveAsync(profileId);

            CompletedToday = (int)await _habitService.GetTodayProgressAsync(profileId);
            TotalHabits = allActiveHabits.Count;

            HasHabits = TotalHabits > 0;
            HabitsSubtitle = HasHabits
                ? $"Выполнено сегодня: {CompletedToday} из {TotalHabits}"
                : "Добавь первую привычку!";
            HabitsCompletedWeek = 
                await _habitService.GetHabitsCompletedWeek(
                    profileId,
                    DateTime.Today.AddDays(-6),
                    DateTime.Today);
        }

        private async Task LoadTasksAsync()
        {

            var allTasks = await _taskService.GetActiveAsync(profileId);

            // TODO: TotalTasks = await _taskRepo.GetOpenCountAsync(profileId);
            // TODO: OverdueTasks = await _taskRepo.GetOverdueCountAsync(profileId);
            TotalTasks = allTasks
                .Where(h => h.Status == TaskStatus.Open || h.Status == TaskStatus.InProgress)
                .Count();
            OverdueTasks = allTasks
                .Where(h => h.DueAt < DateTime.Now)
                .Count();
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
            var alltasks = await _taskService.GetActiveAsync(profileId);

            foreach (var task in alltasks)
            {
                TodayTaskItem newTask = new TodayTaskItem
                {
                    Id = task.Id,
                    Title = task.Title,
                    Priority = GetPriorityLabel(task.Priority),
                    PriorityColor = GetPriorityColor(task.Priority),
                    IsDone = task.Status == TaskStatus.Done
                };

                TodayTasks.Add(newTask);
            }
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
            TaskPriority.Urgent => "#2A0A0A",
            TaskPriority.High => "#2A1800",
            TaskPriority.Medium => "#0A1A2A",
            TaskPriority.Low => "#0A2010",
            _ => "#182333"
        };

        private async Task LoadPomodoroAsync()
        {
            // TODO: IPomodoroRepository
            await Task.Delay(1);

            // TODO: реальные данные из pomodoro_sessions за сегодня
            TodayFocusMinutes = 0;
            TodaySessionsCount = 0;
            ActivePresetLabel = "25 / 5"; // TODO: из активного пресета
            TodayFocusText = TodayFocusMinutes >= 60
                ? $"{TodayFocusMinutes / 60} ч {TodayFocusMinutes % 60} мин"
                : $"{TodayFocusMinutes} мин";
            PomodoroWeek = 0; // TODO: сессий за неделю
        }

        private async Task LoadWeekChartAsync()
        {
            await Task.Delay(1);
            var dayNames = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
            var today = DateTime.Today;
            var points = new List<WeekBarPoint>();

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                // TODO: реальные данные из репозиториев за каждый день
                points.Add(new WeekBarPoint
                {
                    DayLabel = dayNames[(int)day.DayOfWeek],
                    IsToday = day == today,
                    HeightRatio = 0,   // TODO: нормализованное значение 0..1
                    HabitsCount = 0,
                    TasksCount = 0,
                    PomodoroCount = 0
                });
            }

            // Нормализуем высоты
            var maxScore = points.Max(p => p.HabitsCount + p.TasksCount * 2 + p.PomodoroCount);
            if (maxScore > 0)
                foreach (var p in points)
                    p.HeightRatio = (double)(p.HabitsCount + p.TasksCount * 2 + p.PomodoroCount) / maxScore;

            WeekChart = points;
        }

        private void UpdateWeekProgress()
        {
            // Простая формула: (выполнено привычек + закрыто задач + помодоро) / цель
            // TODO: настраиваемая цель
            var target = Math.Max(1, HabitsTargetWeek + 7 + 10); // привычки + задачи + помодоро
            var current = HabitsCompletedWeek + TasksClosedWeek + PomodoroWeek;
            WeekProgress = Math.Min(1.0, (double)current / target);
            WeekProgressText = $"{(int)(WeekProgress * 100)}%";
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAllAsync();

        [RelayCommand]
        private async Task QuickAddHabitAsync()
        {
            // TODO: открыть модальное окно добавления привычки
            await Shell.Current.GoToAsync("//habits");
        }

        [RelayCommand]
        private async Task QuickAddTaskAsync()
        {
            // TODO: открыть модальное окно добавления задачи
            await Shell.Current.GoToAsync("//tasks");
        }

        [RelayCommand]
        private async Task StartPomodoroAsync()
        {
            await Shell.Current.GoToAsync("//pomodoro");
        }

        [RelayCommand]
        private async Task GoToHabitsAsync() => await Shell.Current.GoToAsync("//habits");

        [RelayCommand]
        private async Task GoToTasksAsync() => await Shell.Current.GoToAsync("//tasks");

        [RelayCommand]
        private async Task GoToAnalyticsAsync() => await Shell.Current.GoToAsync("//analytics");
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
