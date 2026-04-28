namespace EduDev_Tracker.Features.Dashboard.Views;

public partial class DashboardPage : ContentPage
{

    const double WidthThreshold = 900;
    public DashboardPage()
    {
        InitializeComponent();

        this.SizeChanged += DashboardPage_SizeChanged;
    }

    private void DashboardPage_SizeChanged(object? sender, EventArgs e)
    {
        if (Width < WidthThreshold)
        {
            VisualStateManager.GoToState(CardsGrid, "Narrow");
        }
        else
        {
            VisualStateManager.GoToState(CardsGrid, "Wide");
        }
    }
}