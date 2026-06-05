using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Navigation
{
    public interface INavigationService
    {
        Task GoToAsync(string route);
        Task GoToAsync(string route, Dictionary<string, object> parameters);
        Task GoBackAsync();
        Task PushModalAsync(Page page);
        Task PopModalAsync();
        Task SwitchToModuleAsync(string flyoutRoute);
    }
}
