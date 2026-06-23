using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

[QueryProperty(nameof(DateString), "date")]
public partial class HabitDayDetailsPage : AnimatedModalPage
{
    private readonly HabitDayDetailsViewModel _vm;
    private string _dateString = string.Empty;

    public string DateString
    {
        get => _dateString;
        set
        {
            _dateString = value;
            _ = LoadAsync();
        }
    }

    public HabitDayDetailsPage(HabitDayDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async Task LoadAsync()
    {
        if (DateTime.TryParseExact(DateString, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
        {
            await _vm.InitializeAsync(date);
        }
    }
}
