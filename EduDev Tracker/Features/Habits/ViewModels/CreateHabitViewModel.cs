using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class CreateHabitViewModel: BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IHabitService _habitService;
        private readonly INotificationService _habitNotificationService;
        private readonly int _profileId;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private HabitTypeItem? selectedHabitType;

        [ObservableProperty]
        private DateTime minDate = DateTime.Today;

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddMonths(3);

        [ObservableProperty]
        private DateTime reminderDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan reminderTime = TimeSpan.FromHours(8);

        [ObservableProperty]
        private string targetValue = string.Empty;

        [ObservableProperty]
        private string targetUnit = string.Empty;

        public double ParsedTargerValue =>
            double.TryParse(TargetValue, out var v) ? v : 0;

        public List<string> SelectedDays =>
            WeekDays.Where(d => d.IsSelected)
            .Select(d => d.Name)
            .ToList();

        public ObservableCollection<HabitTypeItem> HabitTypes { get; } = new()
        {
            new() { Type = HabitType.Binary, Title = "Бинарная" },
            new() { Type = HabitType.Quantitative, Title = "Количественная" },
            new() { Type = HabitType.Time, Title = "По времени" }
        };

        public ObservableCollection<DayItem> WeekDays { get; } = new()
        {
            new DayItem("Пн"),
            new DayItem("Вт"),
            new DayItem("Ср"),
            new DayItem("Чт"),
            new DayItem("Пт"),
            new DayItem("Сб"),
            new DayItem("Вс"),
        };

        public CreateHabitViewModel(INavigationService navigation, IHabitService habitService,
            INotificationService habitNotificationService)
        {
            _navigation = navigation;
            _habitService = habitService;
            _habitNotificationService = habitNotificationService;

            foreach (var day in WeekDays)
                day.PropertyChanged += Day_PropertyChanged;

            _profileId = Preferences.Default.Get("active_profile_id", 0);
        }
        private void Day_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DayItem.IsSelected))
                return;

            OnPropertyChanged(nameof(SelectedDays));
        }

        private int BuildDaysMask()
        {
            int mask = 0;

            foreach(var day in WeekDays)
            {
                if (!day.IsSelected)
                {
                    continue;
                }

                mask |= day.Name switch
                {
                    "Пн" => 1,
                    "Вт" => 2,
                    "Ср" => 4,
                    "Чт" => 8,
                    "Пт" => 16,
                    "Сб" => 32,
                    "Вс" => 64,
                    _ => 0
                };
            }

            return mask;
        }
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                var Schedule = new HabitSchedule
                {
                    DayMask = BuildDaysMask(),
                    TimeOfDay = $"{ReminderTime.Hours:00}:{ReminderTime.Minutes:00}"
                };

                var habit = await _habitService.CreateAsync(
                    profileId: _profileId,
                    title: Title,
                    type: SelectedHabitType?.Type ?? HabitType.Binary,
                    schedule: Schedule,
                    targetUnit: TargetUnit,
                    targetValue: ParsedTargerValue,
                    description: Description
                    );
                await _habitNotificationService.ScheduleHabitNotificationAsync(habit, Schedule);
                await _navigation.PopModalAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }


        [RelayCommand]
        private async Task CancelAsync()
        {
            await _navigation.GoBackAsync();
        }

        [RelayCommand]
        private void ToggleDay(string dayName)
        {
            var day = WeekDays.FirstOrDefault(d => d.Name == dayName);
            if(day is not null)
            {
                day.IsSelected = !day.IsSelected;
            }
        }
    }

    public partial class DayItem : ObservableObject
    {
        public string Name { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundColor))]
        [NotifyPropertyChangedFor(nameof(TextColor))]
        [NotifyPropertyChangedFor(nameof(BorderColor))]
        private bool _isSelected;

        public Color BackgroundColor => IsSelected
            ? Color.FromArgb("#4F6EF7")
            : Color.FromArgb("#14182E");

        public Color TextColor => IsSelected
            ? Colors.White
            : Color.FromArgb("#99FFFFFF");

        public Color BorderColor => IsSelected
            ? Color.FromArgb("#4F6EF7")
            : Colors.White;

        public DayItem(string name)
        {
            Name = name;
        }
    }

    public class HabitTypeItem
    {
        public HabitType Type {  get; set; }
        public string Title { get; set; } = "";
    }
}
