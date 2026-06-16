using EduDev_Tracker.Features.Dashboard.ViewModels;
using EduDev_Tracker.Services.Notification;

namespace EduDev_Tracker.Features.Dashboard.Views;

public partial class DashboardPage : ContentPage
{

    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
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
        TriggerAdaptiveState(Width);
        await _vm.InitializeAsync();
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        TriggerAdaptiveState(width);
    }

    private void TriggerAdaptiveState(double width)
    {
        if (width <= 0) return;

        var state = width >= 768 ? "Wide" : "Narrow";
        VisualStateManager.GoToState(CardsGrid, state);
    }


}