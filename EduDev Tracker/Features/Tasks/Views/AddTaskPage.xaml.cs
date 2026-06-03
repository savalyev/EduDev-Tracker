using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class AddTaskPage : ContentPage
{
	public AddTaskPage(AddTaskViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}