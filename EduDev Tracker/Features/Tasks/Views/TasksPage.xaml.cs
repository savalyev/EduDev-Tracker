using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _vm;
	public TasksPage(TasksViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if(BindingContext is TasksViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}