using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class TasksPage : ContentPage
{
	public TasksPage(TasksViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}