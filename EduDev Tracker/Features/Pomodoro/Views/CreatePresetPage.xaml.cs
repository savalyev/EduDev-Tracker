using EduDev_Tracker.Features.Pomodoro.ViewModels;

namespace EduDev_Tracker.Features.Pomodoro.Views;

public partial class CreatePresetPage : ContentPage
{

	public CreatePresetPage(CreatePresetViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}