using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Features.Pomodoro.Views;

public partial class TaskPickerBottomSheet : ContentPage
{
    private readonly TaskRepository _taskRepo;
    private List<TaskItem> _allTasks = new();
    private TaskItem? _selectedTask;

    public event Action<TaskItem?>? TaskPicked;
    public TaskPickerBottomSheet(TaskRepository taskRepo)
	{
		InitializeComponent();
        _taskRepo = taskRepo;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        int profileId = SessionService.GetProfileId();

        var all = await _taskRepo.GetAllAsync();
            _allTasks = all
            .Where(t => t.ProfileId == profileId
                     && !t.IsArchived
                     && t.Status is TaskStatus.Open or TaskStatus.InProgress)
            .OrderBy(t => t.DueAt.HasValue ? 0 : 1)
            .ThenBy(t => t.DueAt)
            .ThenByDescending(t => t.Priority)
            .ToList();

        TaskList.ItemsSource = _allTasks;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            TaskList.ItemsSource = _allTasks;
            return;
        }

        var filtered = _allTasks
            .Where(t => t.Title.ToLower().Contains(query)
                     || (t.Category?.ToLower().Contains(query) ?? false))
            .ToList();

        TaskList.ItemsSource = filtered;
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedTask = e.CurrentSelection.FirstOrDefault() as TaskItem;
    }

    private void OnCloseClicked(object sender, EventArgs e)
        => Navigation.PopModalAsync();

    private void OnClearTaskClicked(object sender, EventArgs e)
    {
        _selectedTask = null;
        TaskPicked?.Invoke(null);
        Navigation.PopModalAsync();
    }

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        TaskPicked?.Invoke(_selectedTask);
        Navigation.PopModalAsync();
    }
}