using EduDev_Tracker.Features.Analytics.ViewModels;

namespace EduDev_Tracker.Features.Analytics.Views;

public partial class AnalyticsPage : ContentPage
{
    private readonly AnalyticsViewModel _vm;
    private readonly AnalyticsChartDrawable _chartDrawable = new();

    public AnalyticsPage(AnalyticsViewModel vm)
	{
		InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        ChartViewDesktop.Drawable = _chartDrawable;
        ChartViewMobile.Drawable = _chartDrawable;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.ChartPoints))
            {
                _chartDrawable.Points = vm.ChartPoints;
                ChartViewDesktop.Invalidate();
                ChartViewMobile.Invalidate();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateLayout(Width);
        await _vm.InitializeAsync();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateLayout(width);
    }

    private void UpdateLayout(double width)
    {
        bool desktop = width >= 768;
        DesktopLayout.IsVisible = desktop;
        MobileLayout.IsVisible = !desktop;
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}