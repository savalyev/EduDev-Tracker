namespace EduDev_Tracker.Services.Auth
{
    public record OAuthUserInfo(
        string Provider,
        string ProviderId,
        string Name,
        string? Email,
        string? AvatarUrl);

    public interface IOAuthService
    {
        Task<OAuthUserInfo> LoginWithGoogleAsync();
        Task<OAuthUserInfo> LoginWithGitHubAsync();
    }
}
