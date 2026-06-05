using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route)
            => Shell.Current.GoToAsync(route);

        public Task GoToAsync(string route, Dictionary<string, object> parameters)
            => Shell.Current.GoToAsync(route, parameters);

        public Task GoBackAsync()
            => Shell.Current.GoToAsync("..");

        public Task PushModalAsync(Page page)
            => Application.Current.MainPage.Navigation.PushModalAsync(page, false);

        public Task PopModalAsync()
            => Application.Current.MainPage.Navigation.PopModalAsync();

        public Task SwitchToModuleAsync(string flyoutRoute)
        {
            var item = Shell.Current.Items
                .FirstOrDefault(i => i.Route == flyoutRoute);
            if (item != null)
                Shell.Current.CurrentItem = item;
            return Task.CompletedTask;
        }
    }
}
