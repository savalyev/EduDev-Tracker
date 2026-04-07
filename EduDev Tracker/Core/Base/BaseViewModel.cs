using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EduDev_Tracker.Core.Base
{
    public partial class BaseViewModel: ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        public bool IsNotBusy => !IsBusy;

        public virtual Task InitializeAsync(IDictionary<string, object>? query = null)
        {
            return Task.CompletedTask;
        }

    }
}
