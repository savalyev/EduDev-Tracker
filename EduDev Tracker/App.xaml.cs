using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Auth.Views;
using EduDev_Tracker.Services.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace EduDev_Tracker
{
    public partial class App : Application
    {
        private readonly DatabaseService _db;
        private readonly ProfileRepository _profileRepo;
        private readonly IServiceProvider _services;

        public App(DatabaseService db, ProfileRepository profileRepo, IServiceProvider services)
        {
            InitializeComponent();
            _db = db;
            _profileRepo = profileRepo;
            _services = services;

            MainPage = new ContentPage { BackgroundColor = Color.FromArgb("#0A0E1A") };

            _ = InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _db.InitAsync();

                int profileId = SessionService.GetProfileId();

                System.Diagnostics.Debug.WriteLine($"[App Init] profileId из Preferences = {profileId}");

                if (profileId == 0)
                {
                    GoToAuth();
                    return;
                }

                var profile = await _profileRepo.GetByIdAsync(profileId);

                System.Diagnostics.Debug.WriteLine($"[App Init] profile из БД = {profile?.Name ?? "null"}");

                if (profile is null)
                {
                    SessionService.Clear();
                    GoToAuth();
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new AppShell();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Init] EXCEPTION: {ex}");
                GoToAuth();
            }
        }

        private void GoToAuth()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(
                    _services.GetRequiredService<AuthPage>()
                )
                {
                    BarBackgroundColor = Color.FromArgb("#0A0E1A"),
                    BarTextColor = Color.FromArgb("#E8EAF0")
                };
            });
        }
    }
}