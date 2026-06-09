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
        await _vm.LoadAsync();
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