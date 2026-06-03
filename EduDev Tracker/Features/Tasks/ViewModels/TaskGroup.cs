using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EduDev_Tracker.Features.Tasks.ViewModels
{
    public class TaskGroup : ObservableCollection<TaskItemViewModel>
    {
        public string GroupTitle { get; }
        public string GroupColor { get; }

        public TaskGroup(string title, string color, IEnumerable<TaskItemViewModel> items)
            : base(items)
        {
            GroupTitle = title;
            GroupColor = color;
        }
    }
}
