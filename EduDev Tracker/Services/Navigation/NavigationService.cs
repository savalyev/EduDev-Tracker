using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Navigation
{
    public class NavigationService: INavigationService
    {
        public Task NavigateAsync(Page page)
            => Shell.Current.GoToAsync(page.GetType().FullName);

        public Task NavigateModalAsync(Page page)
            => Shell.Current.CurrentPage.Navigation.PushModalAsync(page);

        public Task GoBackAsync()
            => Shell.Current.GoToAsync("..");

        public Task GoBackModalAsync()
            => Shell.Current.CurrentPage.Navigation.PopModalAsync();
    }
}
