using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

[QueryProperty(nameof(HabitId), "habitId")]
public partial class HabitDetailsPage : AnimatedModalPage
{

    private readonly HabitDetailsViewModel _vm;
    private string _habitId;
    public string HabitId
    {
        get => _habitId;
        set
        {
            _habitId = value;
            _ = LoadAsync();
        }
    }
    public HabitDetailsPage(HabitDetailsViewModel vm)
	{
		InitializeComponent();
        BindingContext = _vm = vm;
     }
    private async Task LoadAsync()
    {
        if (int.TryParse(HabitId, out var id))
            await _vm.InitializeAsync(id);
    }

}