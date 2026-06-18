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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PageScrollView.Opacity = 0;
        PageScrollView.TranslationY = 14;
        if (BindingContext is TasksViewModel vm && !vm.IsBusy)
            await vm.LoadCommand.ExecuteAsync(null);
        await Task.WhenAll(
            PageScrollView.FadeTo(1, 260, Easing.CubicOut),
            PageScrollView.TranslateTo(0, 0, 260, Easing.CubicOut)
        );
    }

}