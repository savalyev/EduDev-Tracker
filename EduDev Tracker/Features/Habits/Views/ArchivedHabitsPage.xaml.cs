using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

public partial class ArchivedHabitsPage : ContentPage
{

    private readonly ArchivedHabitsViewModel _vm;
    public ArchivedHabitsPage(ArchivedHabitsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        System.Diagnostics.Debug.WriteLine($"[ArchivedPage] BindingContext = {vm?.GetType().Name ?? "NULL"}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}