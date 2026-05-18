using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
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
    public partial class CreateHabitViewModel: BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IHabitService _habitService;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private HabitType selectedType = HabitType.Binary;

        [ObservableProperty]
        private DateTime minDate = DateTime.Today;

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddMonths(3);

        [ObservableProperty]
        private DateTime reminderDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan reminderTime = TimeSpan.FromHours(8);

        public List<string> SelectedDays =>
            WeekDays.Where(d => d.IsSelected)
            .Select(d => d.Name)
            .ToList();

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

        public CreateHabitViewModel(INavigationService navigation, IHabitService habitService)
        {
            _navigation = navigation;
            _habitService = habitService;

            foreach (var day in WeekDays)
                day.PropertyChanged += Day_PropertyChanged;
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
            IsBusy = true;
            try
            {
                var habit = await _habitService.CreateAsync(
                    profileId: 1,
                    title: Title,
                    type: SelectedType,
                    description: Description);

                var schedule = new HabitSchedule
                {
                    HabitId = habit.Id,
                    DayMask = BuildDaysMask(),
                    TimeOfDay = ReminderTime.ToString(@"hh\:mm")
                };

                await _navigation.GoBackModalAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }


        [RelayCommand]
        private async Task CancelAsync()
        {
            await _navigation.GoBackModalAsync();
        }

        [RelayCommand]
        private void ToggleDay(string day)
        {
            if (SelectedDays.Contains(day))
            {
                SelectedDays.Remove(day);
            }
            else
            {
                SelectedDays.Add(day);
            }
        }


    }

    public partial class DayItem: ObservableObject
    {
        public string Name { get; }

        [ObservableProperty]
        private bool _isSelected;

        public DayItem(string name)
        {
            Name = name;
        }
}
