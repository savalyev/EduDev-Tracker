using EduDev_Tracker.Features.Pomodoro.ViewModels;

namespace EduDev_Tracker.Features.Pomodoro.Views;

public partial class PomodoroPage : ContentPage
{

	private readonly PomodoroViewModel _vm;
	public PomodoroPage(PomodoroViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;

        vm.TimerRing.Invalidated += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerRingDesktop.Invalidate();
                TimerRingMobile.Invalidate();
            });
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.OnPageReappearing();
        await _vm.InitializeAsync();
        UpdateLayout(Width);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
        DesktopLayout.IsVisible = isDesktop;
        MobileLayout.IsVisible = !isDesktop;
    }
}