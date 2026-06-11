using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Auth.Views;
using EduDev_Tracker.Services.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Profile.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly ProfileRepository _profileRepo;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;

        [ObservableProperty] private string userName = "Пользователь";
        [ObservableProperty] private string userEmail = "—";
        [ObservableProperty] private string accountType = "Локальный профиль";

        public ProfileViewModel(
            ProfileRepository profileRepo,
            IAuthService authService,
            IServiceProvider services)
        {
            _profileRepo = profileRepo;
            _authService = authService;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            int profileId = Preferences.Default.Get("active_profile_id", 0);
            if (profileId == 0) return;

            var profile = await _profileRepo.GetByIdAsync(profileId);
            if (profile is null) return;

            UserName = string.IsNullOrWhiteSpace(profile.Name) ? "Пользователь" : profile.Name;
            UserEmail = string.IsNullOrWhiteSpace(profile.Email) ? "—" : profile.Email;
            AccountType = profile.IsLocal ? "Локальный профиль" : "Аккаунт по email";
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Выход",
                "Выйти из аккаунта?",
                "Выйти",
                "Отмена");

            if (!confirm) return;

            await _authService.LogoutAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.MainPage = new NavigationPage(
                    _services.GetRequiredService<AuthPage>()
                );
            });
        }
    }
}
