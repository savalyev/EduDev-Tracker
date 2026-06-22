using EduDev_Tracker.Features.DevTools.ViewModels;

namespace EduDev_Tracker.Features.DevTools.Views;

public partial class ConvertersPage : ContentPage
{
    private readonly ConvertersViewModel _vm;
    public ConvertersPage(ConvertersViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.InitializeAsync();
            UpdateLayout(Width);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConvertersPage.OnAppearing] {ex}");
        }
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
    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}