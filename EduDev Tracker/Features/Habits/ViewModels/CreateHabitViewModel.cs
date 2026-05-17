using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Services.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace EduDev_Tracker.Features.Habits.ViewModels
{
    public partial class CreateHabitViewModel: BaseViewModel
    {
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _selectedType = "Бинарная";

        [ObservableProperty]
        private string _selectedFrequency = "Ежедневно";

        [ObservableProperty]
        private bool[] _selectedDays = new bool[7];

        [ObservableProperty]
        private DateTime _reminderDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan _reminderTime = TimeSpan.FromHours(8);

        public CreateHabitViewModel(INavigationService navigation)
        {
            _navigation = navigation;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            //вызываем сервис и сохраняем в бдшечку
            await _navigation.GoBackModalAsync();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Description);
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await _navigation.GoBackModalAsync();
        }


    }
}
