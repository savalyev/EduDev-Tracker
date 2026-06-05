using EduDev_Tracker.Data;
using EduDev_Tracker.Services.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace EduDev_Tracker
{
    public partial class App : Application
    {
        private readonly DatabaseService _db;

        public App(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            MainPage = new AppShell();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        System.Diagnostics.Debug.WriteLine($"[UNHANDLED] {e.ExceptionObject}");

            _ = InitializeDatabaseAsync();

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[TASK UNHANDLED] {e.Exception}");
                e.SetObserved();
            };
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _db.InitAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}