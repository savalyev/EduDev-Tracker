using EduDev_Tracker.Features.Auth.ViewModels;

namespace EduDev_Tracker.Features.Auth.Views;

public partial class AuthPage : ContentPage
{
    public AuthPage(AuthViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static void SetInputFocus(Border border, bool focused)
    {
        border.Stroke = new SolidColorBrush(Color.FromArgb(focused ? "#5B5FEF" : "#495061"));
    }

    private void OnLoginEmailFocused(object sender, FocusEventArgs e) => SetInputFocus(LoginEmailBorder, true);
    private void OnLoginEmailUnfocused(object sender, FocusEventArgs e) => SetInputFocus(LoginEmailBorder, false);
    private void OnLoginPasswordFocused(object sender, FocusEventArgs e) => SetInputFocus(LoginPasswordBorder, true);
    private void OnLoginPasswordUnfocused(object sender, FocusEventArgs e) => SetInputFocus(LoginPasswordBorder, false);
    private void OnRegNameFocused(object sender, FocusEventArgs e) => SetInputFocus(RegNameBorder, true);
    private void OnRegNameUnfocused(object sender, FocusEventArgs e) => SetInputFocus(RegNameBorder, false);
    private void OnRegEmailFocused(object sender, FocusEventArgs e) => SetInputFocus(RegEmailBorder, true);
    private void OnRegEmailUnfocused(object sender, FocusEventArgs e) => SetInputFocus(RegEmailBorder, false);
    private void OnRegPasswordFocused(object sender, FocusEventArgs e) => SetInputFocus(RegPasswordBorder, true);
    private void OnRegPasswordUnfocused(object sender, FocusEventArgs e) => SetInputFocus(RegPasswordBorder, false);
}