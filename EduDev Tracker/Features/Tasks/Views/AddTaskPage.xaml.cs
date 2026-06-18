using EduDev_Tracker.Features.Tasks.ViewModels;

namespace EduDev_Tracker.Features.Tasks.Views;

public partial class AddTaskPage : ContentPage
{
    public AddTaskPage(AddTaskViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static void SetInputFocus(Border border, bool focused)
    {
        border.Stroke = new SolidColorBrush(Color.FromArgb(focused ? "#5B5FEF" : "#495061"));
    }

    private void OnTitleFocused(object sender, FocusEventArgs e) => SetInputFocus(TitleBorder, true);
    private void OnTitleUnfocused(object sender, FocusEventArgs e) => SetInputFocus(TitleBorder, false);
    private void OnDescFocused(object sender, FocusEventArgs e) => SetInputFocus(DescBorder, true);
    private void OnDescUnfocused(object sender, FocusEventArgs e) => SetInputFocus(DescBorder, false);
}