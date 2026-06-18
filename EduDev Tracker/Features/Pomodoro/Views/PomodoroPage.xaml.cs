using EduDev_Tracker.Features.Pomodoro.ViewModels;

namespace EduDev_Tracker.Features.Pomodoro.Views;

public partial class PomodoroPage : ContentPage
{
	private readonly PomodoroViewModel _vm;
    private Action? _ringInvalidatedHandler;
    private bool _isDesktop;

	public PomodoroPage(PomodoroViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;

        _ringInvalidatedHandler = () => MainThread.BeginInvokeOnMainThread(() =>
        {
            TimerRingDesktop.Invalidate();
            TimerRingMobile.Invalidate();
        });
        vm.TimerRing.Invalidated += _ringInvalidatedHandler;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.TimerRing.Invalidated += _ringInvalidatedHandler;
        _vm.OnPageReappearing();
        try
        {
            await _vm.InitializeAsync();
            UpdateLayout(Width);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PomodoroPage.OnAppearing] {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.TimerRing.Invalidated -= _ringInvalidatedHandler;
        _vm.OnPageDisappearing();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateLayout(width);
    }

    private void UpdateLayout(double width)
    {
        if (width <= 0) return;
        bool isDesktop = width >= 768;
        if (isDesktop == _isDesktop) return;
        _isDesktop = isDesktop;
        DesktopLayout.IsVisible = isDesktop;
        MobileLayout.IsVisible = !isDesktop;
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}
