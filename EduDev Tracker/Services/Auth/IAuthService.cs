using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Auth
{
    public interface IAuthService
    {
        Task<Profile> RegisterAsync(string name, string email, string password);
        Task<Profile> LoginAsync(string email, string password);
        Task<Profile> LoginOfflineAsync();
        Task LogoutAsync();
    }
}
