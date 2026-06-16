using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Pomodoro.Drawing;
using EduDev_Tracker.Features.Pomodoro.Views;
using EduDev_Tracker.Services.Audio;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Pomodoro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EduDev_Tracker.Features.Pomodoro.ViewModels
{
    public partial class DayStatItem : ObservableObject
    {
        [ObservableProperty] private string dayLetter = "";
        [ObservableProperty] private double barHeight = 8;
        [ObservableProperty] private Color barColor = Color.FromArgb("#2DD4BF");
        public int SessionCount { get; set; }
    }

    public class DailyPomodoroStat
    {
        public string Day { get; set; } = "";
        public int Sessions { get; set; }
        public int? Minutes { get; set; }
    }
    public partial class PomodoroViewModel : BaseViewModel
    {
        private readonly IPomodoroService _pomodoroService;
        private readonly TaskRepository _taskRepo;
        private readonly IServiceProvider _services;
        private readonly IAudioService _audioService;
        private readonly BackgroundTimerService _bgTimer;
        private readonly INavigationService _navigationService;

        private IDispatcherTimer? _timer;
        private int _totalSeconds;
        private int _remainingSeconds;
        private PomodoroPhase _currentPhase = PomodoroPhase.Work;
        private int _currentCycle = 1;
        private PomodoroSession? _activeSession;
        private DateTime _sessionStartedAt;

        [ObservableProperty] private string timeDisplay = "25:00";
        [ObservableProperty] private string phaseLabel = "РАБОТА";
        [ObservableProperty] private string cycleDisplay = "Сессия 1 из 4";
        [ObservableProperty] private string startPauseLabel = "Старт";
        [ObservableProperty] private bool isRunning;

        [ObservableProperty] private bool isWorkPhase = true;
        [ObservableProperty] private bool isShortBreakPhase;
        [ObservableProperty] private bool isLongBreakPhase;

        [ObservableProperty] private ObservableCollection<PomodoroPreset> presets = new();
        [ObservableProperty] private PomodoroPreset? activePreset;
        [ObservableProperty] private string selectedPresetsCountText = "3 режима";

        [ObservableProperty] private string linkedTaskTitle = "Не выбрана";
        [ObservableProperty] private string linkedTaskProject = "";
        [ObservableProperty] private string remainingPomodorosText = "—";
        private int? _linkedTaskId;

        [ObservableProperty] private bool soundOnComplete = true;
        [ObservableProperty] private bool soundBeforeEnd = true;
        [ObservableProperty] private string soundDescription = "\"Тихий колокол\" • громкость 60%";

        [ObservableProperty] private string todayPomodoros = "0 помодоро";
        [ObservableProperty] private string weekSessions = "0 сессий";
        [ObservableProperty] private string weekFocusTime = "0 ч 0 мин";
        [ObservableProperty] private string weekStatsDescription = "Загрузка...";
        [ObservableProperty] private string monthTotal = "0";
        [ObservableProperty] private string monthAvgPerDay = "0";
        [ObservableProperty] private string monthBestDay = "—";
        [ObservableProperty] private ObservableCollection<DayStatItem> weekDayStats = new();

        [ObservableProperty] private TimerRingDrawable timerRing = new();

        public PomodoroViewModel(
            IPomodoroService pomodoroService,
            TaskRepository taskRepo,
            IAudioService audioService,
            BackgroundTimerService bgTimer,
            IServiceProvider services,
            INavigationService navigationService)
        {
            _pomodoroService = pomodoroService;
            _taskRepo = taskRepo;
            _audioService = audioService;
            _bgTimer = bgTimer;
            _services = services;
            _navigationService = navigationService;
        }

        public async Task InitializeAsync()
        {
            await LoadPresetsAsync();
            await LoadStatisticsAsync();
            SetupPhase(PomodoroPhase.Work);
        }

        private async Task LoadPresetsAsync()
        {
            int profileId = SessionService.GetProfileId();
            var list = await _pomodoroService.GetPresetsAsync(profileId);

            if (list.Count == 0)
            {
                foreach (var p in GetDefaultPresets(profileId))
                    await _pomodoroService.SavePresetAsync(p);
                list = await _pomodoroService.GetPresetsAsync(profileId);
            }

            Presets = new ObservableCollection<PomodoroPreset>(list);
            ActivePreset = list.FirstOrDefault(p => p.IsDefault) ?? list.FirstOrDefault();
            SelectedPresetsCountText = $"{list.Count} режима";

            if (ActivePreset != null)
                SetupPhase(_currentPhase);
        }

        private async Task LoadStatisticsAsync()
        {
            int profileId = SessionService.GetProfileId();
            var now = DateTime.Now;
            var today = now.Date;

            // Сегодня
            var todayCount = await _pomodoroService.CountCompletedAsync(
                profileId, today, today.AddDays(1));
            TodayPomodoros = $"{todayCount} помодоро";

            // Неделя
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekStats = await _pomodoroService.GetDailyStatsAsync(profileId, weekStart, now);
            var totalWeek = weekStats.Sum(s => s.Sessions);
            var totalMin = weekStats.Sum(s => s.Minutes ?? 0);
            WeekSessions = $"{totalWeek} сессий";
            WeekFocusTime = $"{totalMin / 60} ч {totalMin % 60} мин";
            WeekStatsDescription = totalWeek > 0
                ? $"На этой неделе ты держишь {totalWeek} сессий"
                : "Ещё нет данных за эту неделю";

            BuildWeekChart(weekStats, weekStart);

            // Месяц
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthStats = await _pomodoroService.GetDailyStatsAsync(profileId, monthStart, now);
            var monthTotalCount = monthStats.Sum(s => s.Sessions);
            var daysElapsed = (now.Date - monthStart).Days + 1;
            MonthTotal = monthTotalCount.ToString();
            MonthAvgPerDay = daysElapsed > 0
                ? (monthTotalCount / (double)daysElapsed).ToString("F1")
                : "0";

            if (monthStats.Count > 0)
            {
                var best = monthStats.OrderByDescending(s => s.Sessions).First();
                if (DateTime.TryParse(best.Day, out var bestDate))
                    MonthBestDay = bestDate.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
            }
        }

        private void BuildWeekChart(List<DailyPomodoroStat> stats, DateTime weekStart)
        {
            var days = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            var items = new ObservableCollection<DayStatItem>();
            int maxSessions = stats.Count > 0 ? stats.Max(s => s.Sessions) : 1;
            if (maxSessions == 0) maxSessions = 1;

            for (int i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i).ToString("yyyy-MM-dd");
                var stat = stats.FirstOrDefault(s => s.Day == date);
                int sessions = stat?.Sessions ?? 0;
                double height = Math.Max(8, (sessions / (double)maxSessions) * 60);
                bool isToday = weekStart.AddDays(i).Date == DateTime.Today;

                items.Add(new DayStatItem
                {
                    DayLetter = days[i],
                    SessionCount = sessions,
                    BarHeight = height,
                    BarColor = isToday
                        ? Color.FromArgb("#2DD4BF")
                        : sessions > 0
                            ? Color.FromArgb("#0D9488")
                            : Color.FromArgb("#1E293B")
                });
            }
            WeekDayStats = items;
        }

        private void SetupPhase(PomodoroPhase phase)
        {
            _currentPhase = phase;
            var preset = ActivePreset;
            if (preset == null) return;

            _totalSeconds = phase switch
            {
                PomodoroPhase.Work => preset.WorkMinutes * 60,
                PomodoroPhase.ShortBreak => preset.ShortBreakMin * 60,
                PomodoroPhase.LongBreak => preset.LongBreakMin * 60,
                _ => 25 * 60
            };

            _remainingSeconds = _totalSeconds;
            UpdateTimerDisplay();
            UpdatePhaseLabels(phase);
            UpdateRing();
        }

        private void UpdatePhaseLabels(PomodoroPhase phase)
        {
            IsWorkPhase = phase == PomodoroPhase.Work;
            IsShortBreakPhase = phase == PomodoroPhase.ShortBreak;
            IsLongBreakPhase = phase == PomodoroPhase.LongBreak;

            PhaseLabel = phase switch
            {
                PomodoroPhase.Work => "РАБОТА",
                PomodoroPhase.ShortBreak => "КОРОТКИЙ ПЕРЕРЫВ",
                PomodoroPhase.LongBreak => "ДЛИННЫЙ ПЕРЕРЫВ",
                _ => ""
            };

            int cycles = ActivePreset?.CyclesToLong ?? 4;
            CycleDisplay = $"Сессия {_currentCycle} из {cycles}";
        }

        private void UpdateTimerDisplay()
        {
            int min = _remainingSeconds / 60;
            int sec = _remainingSeconds % 60;
            TimeDisplay = $"{min:D2}:{sec:D2}";
        }
        private void UpdateRing()
        {
            double progress = _totalSeconds > 0
                ? (_totalSeconds - _remainingSeconds) / (double)_totalSeconds
                : 0;
            TimerRing.Progress = progress;
            TimerRing.Phase = _currentPhase;
        }

        [RelayCommand]
        private async Task ToggleTimerAsync()
        {
            if (IsRunning) PauseTimer();
            else await StartTimerAsync();
        }

        private async Task StartTimerAsync()
        {
            IsRunning = true;
            StartPauseLabel = "Пауза";
            _sessionStartedAt = DateTime.UtcNow;

            int profileId = SessionService.GetProfileId();
            _activeSession = new PomodoroSession
            {
                ProfileId = profileId,
                PresetId = ActivePreset?.Id,
                TaskId = _linkedTaskId,
                Phase = _currentPhase,
                StartedAt = _sessionStartedAt,
                PlannedMinutes = _totalSeconds / 60
            };
            await _pomodoroService.SaveSessionAsync(_activeSession);

            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void PauseTimer()
        {
            IsRunning = false;
            StartPauseLabel = "Продолжить";
            _timer?.Stop();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds == 60 && SoundBeforeEnd)
                await _audioService.PlayAsync("bell_soft");

            UpdateTimerDisplay();
            UpdateRing();

            if (_remainingSeconds <= 0)
                await OnPhaseCompleted();
        }

        private async Task OnPhaseCompleted()
        {
            _timer?.Stop();
            IsRunning = false;
            _bgTimer.CancelEndNotification();

            if (_activeSession != null)
            {
                _activeSession.EndedAt = DateTime.UtcNow;
                _activeSession.ActualMinutes = (int)(DateTime.UtcNow - _sessionStartedAt).TotalMinutes;
                _activeSession.WasInterrupted = false;
                await _pomodoroService.SaveSessionAsync(_activeSession);
                _activeSession = null;
            }

            if (SoundOnComplete)
                await _audioService.PlayAsync("bell_complete");

            int cycles = ActivePreset?.CyclesToLong ?? 4;
            if (_currentPhase == PomodoroPhase.Work)
            {
                if (_currentCycle >= cycles)
                {
                    _currentCycle = 1;
                    SetupPhase(PomodoroPhase.LongBreak);
                }
                else
                {
                    _currentCycle++;
                    SetupPhase(PomodoroPhase.ShortBreak);
                }
            }
            else
            {
                SetupPhase(PomodoroPhase.Work);
            }

            StartPauseLabel = "Старт";
            await LoadStatisticsAsync();
        }

        [RelayCommand]
        private async Task SkipPhaseAsync()
        {
            _timer?.Stop();
            _bgTimer.CancelEndNotification();

            if (_activeSession != null)
            {
                _activeSession.EndedAt = DateTime.UtcNow;
                _activeSession.WasInterrupted = true;
                await _pomodoroService.SaveSessionAsync(_activeSession);
                _activeSession = null;
            }
            IsRunning = false;
            await OnPhaseCompleted();
        }

        [RelayCommand]
        private async Task FinishSessionAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Завершить сессию",
                "Текущий прогресс будет сохранён. Завершить?",
                "Завершить", "Отмена");

            if (!confirm) return;

            _timer?.Stop();
            IsRunning = false;
            StartPauseLabel = "Старт";
            _bgTimer.CancelEndNotification();

            if (_activeSession != null)
            {
                _activeSession.EndedAt = DateTime.UtcNow;
                _activeSession.WasInterrupted = true;
                await _pomodoroService.SaveSessionAsync(_activeSession);
                _activeSession = null;
            }

            _currentCycle = 1;
            SetupPhase(PomodoroPhase.Work);
            await LoadStatisticsAsync();
        }

        [RelayCommand]
        private void SwitchPhase(string phaseStr)
        {
            if (IsRunning) return;
            var phase = phaseStr switch
            {
                "Work" => PomodoroPhase.Work,
                "ShortBreak" => PomodoroPhase.ShortBreak,
                "LongBreak" => PomodoroPhase.LongBreak,
                _ => PomodoroPhase.Work
            };
            SetupPhase(phase);
        }

        [RelayCommand]
        private async Task SetDefaultPresetAsync(PomodoroPreset preset)
        {
            if (IsRunning)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Таймер запущен",
                    "Остановите таймер перед сменой пресета.",
                    "OK");
                return;
            }

            int profileId = SessionService.GetProfileId();

            foreach (var p in Presets)
            {
                p.IsDefault = false;
                await _pomodoroService.SavePresetAsync(p);
            }
            preset.IsDefault = true;
            await _pomodoroService.SavePresetAsync(preset);
            ActivePreset = preset;

            var list = await _pomodoroService.GetPresetsAsync(profileId);
            Presets = new ObservableCollection<PomodoroPreset>(list);

            if (!IsRunning)
                SetupPhase(_currentPhase);
        }

        [RelayCommand]
        private async Task OpenCreatePresetAsync()
        {
            var vm = new CreatePresetViewModel(_pomodoroService);

            vm.PresetCreated += async preset =>
            {
                await LoadPresetsAsync();

                if (preset.IsDefault && !IsRunning)
                {
                    ActivePreset = preset;
                    SetupPhase(_currentPhase);
                }
            };

            var page = new CreatePresetPage(vm);
            await Shell.Current.Navigation.PushModalAsync(page, animated: true);
        }

        [RelayCommand]
        private async Task PickTaskAsync()
        {
            var picker = new TaskPickerBottomSheet(_taskRepo);

            picker.TaskPicked += async task =>
            {
                if (task == null)
                {
                    _linkedTaskId = null;
                    LinkedTaskTitle = "Не выбрана";
                    LinkedTaskProject = "";
                    RemainingPomodorosText = "—";

                    if (_activeSession != null)
                        _activeSession.TaskId = null;
                }
                else
                {
                    _linkedTaskId = task.Id;
                    LinkedTaskTitle = task.Title;
                    LinkedTaskProject = task.Category ?? "";
                    RemainingPomodorosText = "—";

                    if (_activeSession != null)
                    {
                        _activeSession.TaskId = task.Id;
                        await _pomodoroService.SaveSessionAsync(_activeSession);
                    }
                }
            };

            await Shell.Current.Navigation.PushModalAsync(picker, animated: true);
        }
        public async void OnPageDisappearing()
        {
            if (!IsRunning) return;

            await _bgTimer.ScheduleEndNotificationAsync(
                remainingSeconds: _remainingSeconds,
                phaseLabel: PhaseLabel,
                taskTitle: LinkedTaskTitle == "Не выбрана" ? "" : LinkedTaskTitle);
        }

        public void OnPageReappearing()
        {
            _bgTimer.CancelEndNotification();
        }

        private static List<PomodoroPreset> GetDefaultPresets(int profileId) => new()
    {
        new PomodoroPreset
        {
            ProfileId = profileId, Name = "Стандарт 25 / 5",
            WorkMinutes = 25, ShortBreakMin = 5, LongBreakMin = 20,
            CyclesToLong = 4, IsDefault = true
        },
        new PomodoroPreset
        {
            ProfileId = profileId, Name = "Глубокая работа 50 / 10",
            WorkMinutes = 50, ShortBreakMin = 10, LongBreakMin = 30,
            CyclesToLong = 3, IsDefault = false
        },
        new PomodoroPreset
        {
            ProfileId = profileId, Name = "Учёба блоками 15 / 3",
            WorkMinutes = 15, ShortBreakMin = 3, LongBreakMin = 15,
            CyclesToLong = 6, IsDefault = false
        },
        new PomodoroPreset
        {
            ProfileId = profileId, Name = "Тестовый",
            WorkMinutes = 1, ShortBreakMin = 1, LongBreakMin = 1,
            CyclesToLong = 2, IsDefault = false
        }
    };
    }
}
