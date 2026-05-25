using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitsViewModel: BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IServiceProvider _services;
        private readonly IHabitService _habitService;

        public ObservableCollection<HabitItemViewModel> Habits { get; } = new();

        public HabitsViewModel(INavigationService navigation, IServiceProvider services, IHabitService habitService)
        {
            _navigation = navigation;
            _services = services;
            _habitService = habitService;
        }

        [RelayCommand]
        private async Task ToggleHabitAsync(HabitItemViewModel item)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                if (item.IsCompleted)
                   await Shell.Current.DisplayAlertAsync("Тест", "LogAsync", "пизда");
                //await _habitService.LogAsync(item.HabitId);
                else
                    await Shell.Current.DisplayAlertAsync("Тест", "UnlogAsync", "пизда");
                //await _habitService.UnlogAsync(item.HabitId);
            }
            finally
            {
                IsBusy = false;
            }
        }


        [RelayCommand]
        private async Task OpenAddHabitAsync()
        {
            var modal = _services.GetRequiredService<CreateHabitPage>();
            await Shell.Current.CurrentPage.Navigation.PushModalAsync(modal);
        }

        [RelayCommand]        
        private async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Habits.Clear();
                var habits = await _habitService.GetActiveAsync(1);
                foreach(var habit in habits)
                {
                    var shedule = await _habitService.GetScheduleAsync(habit.Id);

                    var item = new HabitItemViewModel(habit, shedule, OnHabitToggled);
                    var competed = await _habitService.IsCompletedTodayAsync(habit.Id);

                    item.SetCompletedSilently(competed);

                    Habits.Add(item);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnHabitToggled(int habitId, bool isCompleted)
        {
            try
            {
                if (isCompleted)
                    await _habitService.LogAsync(habitId, DateTime.Today);
                else
                    await _habitService.UnlogAsync(habitId, DateTime.Today);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

    }
}
