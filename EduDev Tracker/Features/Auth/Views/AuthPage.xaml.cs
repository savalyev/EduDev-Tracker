using EduDev_Tracker.Features.Auth.ViewModels;

namespace EduDev_Tracker.Features.Auth.Views;

public partial class AuthPage : ContentPage
{
	public AuthPage(AuthViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}