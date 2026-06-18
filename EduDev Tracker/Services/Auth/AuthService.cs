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

        public async Task<Profile> LoginWithOAuthAsync(OAuthUserInfo info)
        {
            var existing = await _repo.FindByOAuthAsync(info.Provider, info.ProviderId);

            if (existing is not null)
            {
                existing.Name        = info.Name;
                existing.AvatarUrl   = info.AvatarUrl;
                existing.IsActive    = true;
                existing.LastLoginAt = DateTime.UtcNow;
                await _repo.SaveAsync(existing);
                return existing;
            }

            // Если уже есть аккаунт с тем же email — привяжем OAuth к нему
            if (!string.IsNullOrEmpty(info.Email))
            {
                var byEmail = await _repo.FindByEmailAsync(info.Email);
                if (byEmail is not null)
                {
                    byEmail.OAuthProvider = info.Provider;
                    byEmail.OAuthId       = info.ProviderId;
                    byEmail.AvatarUrl     = info.AvatarUrl;
                    byEmail.IsActive      = true;
                    byEmail.LastLoginAt   = DateTime.UtcNow;
                    await _repo.SaveAsync(byEmail);
                    return byEmail;
                }
            }

            var profile = new Profile
            {
                Name          = info.Name,
                Email         = info.Email,
                OAuthProvider = info.Provider,
                OAuthId       = info.ProviderId,
                AvatarUrl     = info.AvatarUrl,
                IsLocal       = false,
                IsActive      = true,
                CreatedAt     = DateTime.UtcNow,
                LastLoginAt   = DateTime.UtcNow
            };

            await _repo.SaveAsync(profile);
            return profile;
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
