using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class TaskDetailsPage : ContentPage
{
    private readonly TaskDetailsViewModel _vm;

	public TaskDetailsPage(TaskDetailsViewModel vm)
	{
            InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.InitCommand.Execute(this);

        this.Opacity = 0;
        this.Scale = 0.95;
        await Task.WhenAll(
            this. FadeTo(1, 250, Easing.CubicOut),
            this.ScaleTo(1, 250, Easing.CubicOut)
            );
    }
}