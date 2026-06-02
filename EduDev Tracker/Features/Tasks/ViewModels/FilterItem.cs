using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class FilterItem : ObservableObject
    {
        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private bool isSelected;
    }
}
