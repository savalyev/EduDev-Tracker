using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class TaskDetailsPage : ContentPage
{
    private readonly TaskDetailsViewModel _vm;

    private string _taskId;
    public string TaskId
    {
        get => _taskId;
        set
        {
            _taskId = value;
            _ = LoadAsync();
        }
    }

	public TaskDetailsPage(TaskDetailsViewModel vm)
	{
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    private async Task LoadAsync()
    {
        if (int.TryParse(TaskId, out var id))
            await _vm.InitializeAsync(id);
    }
}