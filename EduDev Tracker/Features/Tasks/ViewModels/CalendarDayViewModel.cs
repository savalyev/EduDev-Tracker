using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class CalendarDayViewModel : ObservableObject
    {
        [ObservableProperty] private DateTime date;
        [ObservableProperty] private string dayNumber = string.Empty;
        [ObservableProperty] private bool isToday;
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private bool isOtherMonth;
        [ObservableProperty] private bool hasCritical;
        [ObservableProperty] private bool hasHigh;
        [ObservableProperty] private bool hasAny;
    }
}
