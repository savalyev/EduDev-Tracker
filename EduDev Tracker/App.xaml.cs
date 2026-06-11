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

                int profileId = Preferences.Default.Get("active_profile_id", 0);

                if (profileId == 0)
                {
                    GoToAuth();
                    return;
                }

                var profile = await _profileRepo.GetByIdAsync(profileId);
                if (profile is null)
                {
                    Preferences.Default.Remove("active_profile_id");
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
                System.Diagnostics.Debug.WriteLine($"[App Init] {ex}");
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