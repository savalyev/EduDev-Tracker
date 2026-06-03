using EduDev_Tracker.Features.Dashboard.ViewModels;
using EduDev_Tracker.Services.Notification;

namespace EduDev_Tracker.Features.Dashboard.Views;

public partial class DashboardPage : ContentPage
{

    private readonly INotificationService _notificationService;

    const double WidthThreshold = 900;
    public DashboardPage(DashboardViewModel vm, INotificationService notificationService)
    {
        InitializeComponent();
        BindingContext = vm;
        _notificationService = notificationService;

        this.SizeChanged += DashboardPage_SizeChanged;
    }

    private void DashboardPage_SizeChanged(object? sender, EventArgs e)
    {
        if (Width < WidthThreshold)
        {
            VisualStateManager.GoToState(CardsGrid, "Narrow");
        }
        else
        {
            VisualStateManager.GoToState(CardsGrid, "Wide");
        }
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
        await _notificationService.RequestPermissionAsync();
    }
}