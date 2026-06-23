using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using System.Collections.ObjectModel;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitDayDetailsViewModel : BaseViewModel
    {
        private readonly IHabitService _habitService;
        private readonly INavigationService _navigation;
        private readonly int _profileId;

        [ObservableProperty]
        private string dateHeader = string.Empty;

        [ObservableProperty]
        private bool hasCompleted;

        [ObservableProperty]
        private bool hasMissed;

        [ObservableProperty]
        private bool isEmpty;

        public ObservableCollection<HabitDayItem> CompletedHabits { get; } = new();
        public ObservableCollection<HabitDayItem> MissedHabits { get; } = new();

        public HabitDayDetailsViewModel(IHabitService habitService, INavigationService navigation)
        {
            _habitService = habitService;
            _navigation = navigation;
            _profileId = SessionService.GetProfileId();
        }

        public async Task InitializeAsync(DateTime date)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                CompletedHabits.Clear();
                MissedHabits.Clear();

                DateHeader = date.ToString("d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

                var habits = await _habitService.GetHabitsWithStatusForDateAsync(_profileId, date);

                foreach (var (habit, isCompleted) in habits)
                {
                    var item = new HabitDayItem
                    {
                        Title = habit.Title,
                        Icon = habit.Icon ?? "default_icon.png",
                        Description = habit.Description ?? string.Empty,
                    };

                    if (isCompleted)
                        CompletedHabits.Add(item);
                    else
                        MissedHabits.Add(item);
                }

                HasCompleted = CompletedHabits.Count > 0;
                HasMissed = MissedHabits.Count > 0;
                IsEmpty = !HasCompleted && !HasMissed;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CloseAsync() => await _navigation.GoBackAsync();
    }

    public class HabitDayItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "default_icon.png";
        public string Description { get; set; } = string.Empty;
    }
}
