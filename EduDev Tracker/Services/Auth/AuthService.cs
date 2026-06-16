using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using BCrypt.Net;
using EduDev_Tracker.Core.Helpers;

namespace EduDev_Tracker.Services.Auth
{
    public class AuthService: IAuthService
    {
        private readonly ProfileRepository _repo;

        public AuthService(ProfileRepository repo)
        {
            _repo = repo;
        }

        public async Task<Profile> RegisterAsync(string name, string email, string password)
        {
            var existing = await _repo.FindByEmailAsync(email.Trim().ToLower());
            if (existing is not null)
                throw new InvalidOperationException("EMAIL_TAKEN");

            var hash = HashPassword(password);

            var profile = new Profile
            {
                Name = name.Trim(),
                Email = email.Trim().ToLower(),
                PasswordHash = hash,
                IsLocal = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _repo.SaveAsync(profile);
            return profile;
        }

        public async Task<Profile> LoginAsync(string email, string password)
        {
            var profile = await _repo.FindByEmailAsync(email.Trim().ToLower());

            if (profile is null)
                throw new InvalidOperationException("USER_NOT_FOUND");

            if (!VerifyPassword(password, profile.PasswordHash))
                throw new InvalidOperationException("WRONG_PASSWORD");

            profile.IsActive = true;
            profile.LastLoginAt = DateTime.UtcNow;
            await _repo.SaveAsync(profile);

            return profile;
        }

        public async Task<Profile> LoginOfflineAsync()
        {
            var local = await _repo.GetLocalProfileAsync();

            if (local is null)
            {
                local = new Profile
                {
                    Name = "Локальный пользователь",
                    IsLocal = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.SaveAsync(local);
            }
            else
            {
                local.IsActive = true;
                local.LastLoginAt = DateTime.UtcNow;
                await _repo.SaveAsync(local);
            }

            return local;
        }

        public async Task LogoutAsync()
        {
            await _repo.DeactivateAllAsync();
            SessionService.Clear();
        }


        private static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384);

        private static bool VerifyPassword(string password, string? hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hash, HashType.SHA384);
        }
    }
}
