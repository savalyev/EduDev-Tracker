using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Services.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class HabitsViewModel: BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IServiceProvider _services;

        public HabitsViewModel(INavigationService navigation, IServiceProvider services)
        {
            _navigation = navigation;
            _services = services;
        }

        [RelayCommand]
        private async Task OpenAddHabitAsync()
        {
            var modal = _services.GetRequiredService<CreateHabitPage>();
            await Shell.Current.CurrentPage.Navigation.PushModalAsync(modal);
        }   

    }
}
