using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

public partial class HabitsPage : ContentPage
{
    public HabitsPage(HabitsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

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

        if(BindingContext is HabitsViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}