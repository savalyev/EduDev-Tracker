using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

public partial class CreateHabitPage : ContentPage
{
	public CreateHabitPage(CreateHabitViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}