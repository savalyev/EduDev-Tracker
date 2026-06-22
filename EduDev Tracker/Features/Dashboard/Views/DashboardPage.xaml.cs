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
        ContentGrid.Opacity = 0;
        ContentGrid.TranslationY = 14;
        try { await _vm.InitializeAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dashboard] {ex}"); }
        await Task.WhenAll(
            ContentGrid.FadeTo(1, 260, Easing.CubicOut),
            ContentGrid.TranslateTo(0, 0, 260, Easing.CubicOut)
        );
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