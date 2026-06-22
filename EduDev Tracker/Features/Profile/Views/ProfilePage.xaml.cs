using EduDev_Tracker.Features.Profile.ViewModels;

namespace EduDev_Tracker.Features.Profile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilePage.OnAppearing] {ex}");
        }
    }

    private void OnMenuTapped(object sender, TappedEventArgs e)
        => Shell.Current.FlyoutIsPresented = true;
}