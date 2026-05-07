using EduDev_Tracker.Data;
using Microsoft.Extensions.DependencyInjection;
namespace EduDev_Tracker
{
    public partial class App : Application
    {
        public App(DatabaseService db)
        {
            InitializeComponent();
            MainPage = new AppShell();
            _ = db.InitAsync();
        }
    }
}