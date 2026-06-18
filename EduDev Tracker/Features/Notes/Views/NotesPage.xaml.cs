using EduDev_Tracker.Features.Notes.ViewModels;

namespace EduDev_Tracker.Features.Notes.Views;

public partial class NotesPage : ContentPage
{

    private readonly NotesViewModel _vm;
    public NotesPage(NotesViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        RootGrid.Opacity = 0;
        RootGrid.TranslationY = 14;
        try
        {
            if (!_vm.IsBusy)
                await _vm.LoadAsync();
            await Task.WhenAll(
                RootGrid.FadeTo(1, 260, Easing.CubicOut),
                RootGrid.TranslateTo(0, 0, 260, Easing.CubicOut));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotesPage.OnAppearing] {ex}");
        }
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _vm.OnSizeChanged(width);
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}