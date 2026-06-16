using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitsViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IServiceProvider _services;
        private readonly IHabitService _habitService;
        private readonly int _profileId;

        public ObservableCollection<HabitItemViewModel> Habits { get; } = new();
        private readonly Dictionary<int, HabitSchedule> _scheduleCache = new();

        public ObservableCollection<FilterItem> FilterTypes { get; } = new()
        {
            new FilterItem { Title = "Все типы", IsSelected = true },
            new FilterItem { Title = "Бинарные" },
            new FilterItem { Title = "Количественные" },
            new FilterItem { Title = "По времени" }
        };

        public ObservableCollection<DayItemStats> DaysItems { get; } = new();

        private int activeToday = 0;
        private int completedTitle = 0;
        [ObservableProperty]
        private string title = $"Сегодня активных: 0, выполнено: 0";

        [ObservableProperty]
        private string selectedFilter = "Все типы";

        [ObservableProperty]
        private string selectedPeriod = "Сегодня";

        [ObservableProperty]
        private string analyticsCompletionRate;
        [ObservableProperty]
        private string analyticsTotalActive;
        [ObservableProperty]
        private string analyticsBestDays;
        [ObservableProperty]
        private string analyticsWorstDay;

        public HabitsViewModel(INavigationService navigation, IServiceProvider services, IHabitService habitService)
        {
            _navigation = navigation;
            _services = services;
            _habitService = habitService;

            _profileId = SessionService.GetProfileId();
        }

        [RelayCommand]
        private async Task SelectFilterAsync(string filter)
        {
            SelectedFilter = filter;
            foreach (var f in FilterTypes)
            {
                f.IsSelected = f.Title == filter;
            }
            await RefreshHabitsAsync();
        }

        private async Task RefreshHabitsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Habits.Clear();
                _scheduleCache.Clear();

                var allhabits = await _habitService.GetActiveAsync(_profileId);

                foreach (var habit in allhabits)
                {
                    var schedule = await _habitService.GetScheduleAsync(habit.Id);
                    if (schedule != null)
                        _scheduleCache[habit.Id] = schedule;
                }

                await UpdateTitleAsync(allhabits);
                var habits = allhabits;

                if (SelectedFilter != "Все типы")
                {
                    var typeMap = new Dictionary<string, HabitType>
            {
                { "Бинарные",       HabitType.Binary },
                { "Количественные", HabitType.Quantitative },
                { "По времени",     HabitType.Time }
            };
                    if (typeMap.TryGetValue(SelectedFilter, out var habitType))
                        habits = habits.Where(h => h.Type == habitType).ToList();
                }

                habits = SelectedPeriod switch
                {
                    "Сегодня" => FilterByPeriod(habits, DateTime.Today, DateTime.Today),
                    "Неделя" => FilterByPeriod(habits, DateTime.Today.AddDays(-6), DateTime.Today),
                    _ => habits
                };

                foreach (var habit in habits)
                {
                    var item = new HabitItemViewModel(habit, _scheduleCache.GetValueOrDefault(habit.Id), OnHabitToggled);
                    var completed = await _habitService.IsCompletedTodayAsync(habit.Id);
                    item.SetCompletedSilently(completed);
                    var progress = await _habitService.GetTodayProgressAsync(habit.Id);
                    var streak = await _habitService.GetCurrentStreakAsync(habit.Id);
                    item.SetProgressSilently(progress, streak);
                    Habits.Add(item);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<Habit> FilterByPeriod(List<Habit> habits, DateTime from, DateTime to)
        {
            var dayFlags = Enumerable
                .Range(0, (to - from).Days + 1)
                .Select(d => from.AddDays(d).DayOfWeek)
                .Aggregate(0, (mask, dow) => mask | DayOfWeekToMask(dow));

            return habits
                .Where(h => _scheduleCache.TryGetValue(h.Id, out var schedule)
                            && (schedule.DayMask & dayFlags) != 0)
                .ToList();
        }

        private static int DayOfWeekToMask(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => 0b0000001,
            DayOfWeek.Tuesday => 0b0000010,
            DayOfWeek.Wednesday => 0b0000100,
            DayOfWeek.Thursday => 0b0001000,
            DayOfWeek.Friday => 0b0010000,
            DayOfWeek.Saturday => 0b0100000,
            DayOfWeek.Sunday => 0b1000000,
            _ => 0
        };

        private static string DayOfWeekToRussianVersion(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => "Пн",
            DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday => "Чт",
            DayOfWeek.Friday => "Пт",
            DayOfWeek.Saturday => "Сб",
            DayOfWeek.Sunday => "Вс",
            _ => "EX"
        };

        [RelayCommand]
        private async Task SelectPeriodAsync(string period)
        {
            SelectedPeriod = period;
            await RefreshHabitsAsync();
        }

        [RelayCommand]
        private async Task OpenDelailsDayAsync(DateTime date)
        {
           
        }


        [RelayCommand]
        private async Task OpenAddHabitAsync()
        {
            //var modal = _services.GetRequiredService<CreateHabitPage>();
            //await _navigation.PushModalAsync(modal);
            await _navigation.GoToAsync(nameof(CreateHabitPage));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RefreshHabitsAsync();
            await LoadDaysStats();
            await LoadAnalytic();
        }

        private async Task OnHabitToggled(int habitId, bool isCompleted, string habitType)
        {
            try
            {
                if (isCompleted)
                {
                    if (habitType != "Binary")
                    {
                        double value;
                        string result = await Shell.Current.DisplayPromptAsync(
                            title: "Введите значение",
                            message: "Укажите нужное значение",
                            accept: "OK",
                            cancel: "Отмена",
                            placeholder: "Например: 42",
                            maxLength: 10,
                            keyboard: Keyboard.Default);
                        if (result == null)
                        {
                            await LoadAsync();
                            return;
                        }
                        else
                        {
                            if (!double.TryParse(result, out value))
                            {
                                await Shell.Current.DisplayAlertAsync("Ошибка", "Некорректный ввод", "Принято");
                                await LoadAsync();
                                return;
                            }
                        }

                        await _habitService.LogAsync(habitId, DateTime.Today, value);
                    }
                    else
                    {
                        await _habitService.LogAsync(habitId, DateTime.Today);
                    }
                }
                else
                {
                    await _habitService.UnlogAsync(habitId, DateTime.Today);
                }
                await RefreshHabitsAsync();
                await LoadDaysStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task UpdateTitleAsync(IEnumerable<Habit> allHabits)
        {
            var todayMask = DayOfWeekToMask(DateTime.Today.DayOfWeek);

            int activeTodayCount = 0;
            int completedTodayCount = 0;

            foreach (var habit in allHabits)
            {
                if (_scheduleCache.TryGetValue(habit.Id, out var schedule) &&
                    (schedule.DayMask & todayMask) != 0)
                {
                    activeTodayCount++;

                    if (await _habitService.IsCompletedTodayAsync(habit.Id))
                        completedTodayCount++;
                }
            }

            Title = $"Сегодня активных: {activeTodayCount}, выполнено: {completedTodayCount}";
        }

        private async Task LoadDaysStats()
        {
            DaysItems.Clear();

            var allHabit = await _habitService.GetActiveAsync(_profileId);
            AnalyticsTotalActive = allHabit.Count().ToString();
            DateTime today = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var dayMask = DayOfWeekToMask(day.DayOfWeek);

                var habitsForDay = allHabit
                    .Where(h => _scheduleCache.TryGetValue(h.Id, out var schedule)
                    && (schedule.DayMask & dayMask) != 0)
                    .ToList();

                bool done = false;
                int missedCount = 0;

                if (habitsForDay.Count > 0)
                {
                    var competionChecks = await Task.WhenAll(
                        habitsForDay.Select(h => _habitService.IsCompletedTodayAsync(h.Id, day))
                        );
                    done = competionChecks.All(c => c);
                    missedCount = competionChecks.Count(c => !c);
                }

                DaysItems.Add(new DayItemStats
                {
                    Title = $"{day.Day}\n{DayOfWeekToRussianVersion(day.DayOfWeek)}",
                    Date = day,
                    Done = done,
                    MissedCount = missedCount,
                    TotalCount = habitsForDay.Count
                });
            }
        }

        private Task LoadAnalytic()
        {
            int countDoneDay = DaysItems.Where(h => h.Done == true).Count();
            AnalyticsCompletionRate = $"{(int)Math.Round((double)countDoneDay / 7 * 100)}%";

            var bestDays = DaysItems
               .Where(d => d.Done)
               .Select(d => DayOfWeekToRussianVersion(d.Date.DayOfWeek));

            AnalyticsBestDays = bestDays.Any()
                ? string.Join(", ", bestDays)
                : "—";

            var worstDay = DaysItems
                .Where(d => d.MissedCount > 0)
                .OrderByDescending(d => d.MissedCount)
                .FirstOrDefault();

            AnalyticsWorstDay = worstDay != null
                ? $"{DayOfWeekToRussianVersion(worstDay.Date.DayOfWeek)} ({worstDay.MissedCount} пропуска)"
                : "Нет пропусков";

            return Task.CompletedTask;

        }

        [RelayCommand]
        private async Task OpenHabitDetailsAsync(int id)
        {
            await _navigation.GoToAsync($"{nameof(HabitDetailsPage)}?habitId={id}");
        }

        [RelayCommand]
        private async Task OpenArchiveAsync()
        {
            try
            {
                await _navigation.GoToAsync(nameof(ArchivedHabitsPage));
                //var modal = _services.GetRequiredService<ArchivedHabitsPage>();
                //await _navigation.PushModalAsync(modal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Archive] Ошибка открытия: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    public partial class FilterItem : ObservableObject
    {
        public string Title { get; set; }

        [ObservableProperty]
        private bool isSelected;
    }

    public partial class DayItemStats : ObservableObject
    {
        public string Title { get; set;  }
        public DateTime Date { get; set; }

        [ObservableProperty]
        private bool done;

        public int MissedCount { get; set; } 
        public int TotalCount { get; set; }
    }

}
