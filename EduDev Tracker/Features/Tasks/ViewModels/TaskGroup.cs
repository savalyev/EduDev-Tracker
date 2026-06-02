using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public partial class TaskGroup: ObservableObject
    {
        [ObservableProperty] private string groupTitle = string.Empty;
        [ObservableProperty] private string groupColor = "#AAAAAA";

        public ObservableCollection<TaskItemViewModel> Items { get; } = new();
    }
}
