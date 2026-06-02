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
            _ = InitializeDatabaseAsync();
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