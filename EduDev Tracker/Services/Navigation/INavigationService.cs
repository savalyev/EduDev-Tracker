using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Navigation
{
    public interface INavigationService
    {
        Task NavigateAsync(Page page);
        Task NavigateModalAsync(Page page);
        Task GoBackAsync();
        Task GoBackModalAsync();
    }
}
