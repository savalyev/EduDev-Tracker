using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _vm;
	public TasksPage(TasksViewModel vm)
	{
        System.Diagnostics.Debug.WriteLine("[TasksPage] Constructor START");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[TasksPage] InitializeComponent OK");
        BindingContext = vm;
        _vm = vm;
        System.Diagnostics.Debug.WriteLine("[TasksPage] BindingContext OK");
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
        this.Opacity = 0;
        this.FadeToAsync(1, 200, Easing.CubicOut);
        if (BindingContext is TasksViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }

}