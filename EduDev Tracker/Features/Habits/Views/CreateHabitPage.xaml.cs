using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

public partial class CreateHabitPage : ContentPage
{
	public CreateHabitPage(CreateHabitViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private readonly HashSet<string> _selectedDays = new();

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var selectedItem = (HabitTypeItem)picker.SelectedItem;
        if(selectedItem.Title == "Бинарная")
        {
            LabelTargetUnit.IsVisible = false;
            LabelTargetValue.IsVisible = false;
            EditorTargetUnit.IsVisible = false;
            EditorTargetValue.IsVisible = false;
        }
        else
        {
            LabelTargetUnit.IsVisible = true;
            LabelTargetValue.IsVisible = true;
            EditorTargetUnit.IsVisible = true;
            EditorTargetValue.IsVisible = true;
        }
    }

    //private void OnDayTapped(object? sender, TappedEventArgs e)
    //{
    //    if (sender is not Border border) return;

    //    var day = e.Parameter?.ToString() ?? string.Empty;
    //    var label = border.Content as Label;

    //    if (_selectedDays.Contains(day))
    //    {
    //        _selectedDays.Remove(day);
    //        border.BackgroundColor = Color.FromArgb("#14182E");
    //        border.Stroke = new SolidColorBrush(Colors.White);
    //        if (label != null)
    //            label.TextColor = Color.FromArgb("#99FFFFFF");
    //    }
    //    else
    //    {
    //        _selectedDays.Add(day);
    //        border.BackgroundColor = Color.FromArgb("#4F6EF7");
    //        border.Stroke = new SolidColorBrush(Color.FromArgb("#4F6EF7"));
    //        if (label != null)
    //            label.TextColor = Colors.White;

    //        if (BindingContext is CreateHabitViewModel vm)
    //            vm.SelectedDays = _selectedDays.ToList();
    //    }
    //}
}