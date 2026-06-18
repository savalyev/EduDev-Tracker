using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class ReminderSettingsPage : ContentPage
{
    private readonly ReminderSettingsViewModel _vm;

    public int TaskId
    {
        set => _ = _vm.InitializeAsync(value);
    }

    public ReminderSettingsPage(ReminderSettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}
