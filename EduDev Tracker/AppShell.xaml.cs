using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Features.Tasks.Views;


namespace EduDev_Tracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(HabitDetailsPage), typeof(HabitDetailsPage));
            Routing.RegisterRoute(nameof(ArchivedHabitsPage), typeof(ArchivedHabitsPage));
            Routing.RegisterRoute(nameof(CreateHabitPage), typeof(CreateHabitPage));
            Routing.RegisterRoute(nameof(HabitDayDetailsPage), typeof(HabitDayDetailsPage));

            Routing.RegisterRoute(nameof(TaskDetailsPage), typeof(TaskDetailsPage));
            Routing.RegisterRoute(nameof(AddTaskPage), typeof(AddTaskPage));
            Routing.RegisterRoute(nameof(ReminderSettingsPage), typeof(ReminderSettingsPage));
        }
    }
}
