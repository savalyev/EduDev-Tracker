namespace EduDev_Tracker.Features.Habits.Views;

public partial class HabitsPage : ContentPage
{
    public HabitsPage()
    {
        InitializeComponent();
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }

    private void OnAddHabitTapped(object? sender, EventArgs e)
    {

    }
}