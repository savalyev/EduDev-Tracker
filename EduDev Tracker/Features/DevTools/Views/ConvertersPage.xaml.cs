using EduDev_Tracker.Features.DevTools.ViewModels;

namespace EduDev_Tracker.Features.DevTools.Views;

public partial class ConvertersPage : ContentPage
{
    private readonly ConvertersViewModel _vm;
    public ConvertersPage(ConvertersViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;

        // TimePicker не имеет XAML-события выбора — слушаем смену Time вручную
        MobileTimeTimePicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
                ComposeMobileTimeInput();
        };
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.InitializeAsync();
            // Синхронизируем мобильные пикеры с текущим временем (TimeInput после Init = сейчас)
            var now = DateTime.Now;
            MobileTimeDatePicker.Date = now.Date;
            MobileTimeTimePicker.Time = new TimeSpan(now.Hour, now.Minute, 0);
            UpdateLayout(Width);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConvertersPage.OnAppearing] {ex}");
        }
    }

    // Дата/время выбраны пикерами → собираем строку формата, который ждёт ViewModel
    private void OnMobileTimeChanged(object? sender, DateChangedEventArgs e) => ComposeMobileTimeInput();

    private void OnMobileUseNowClicked(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        MobileTimeDatePicker.Date = now.Date;
        MobileTimeTimePicker.Time = new TimeSpan(now.Hour, now.Minute, 0);
        ComposeMobileTimeInput();
    }

    private void ComposeMobileTimeInput()
    {
        var date = (MobileTimeDatePicker.Date ?? DateTime.Today).Date;
        var time = MobileTimeTimePicker.Time ?? TimeSpan.Zero;
        var dt = date + time;
        _vm.TimeInput = dt.ToString("yyyy-MM-dd HH:mm");
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