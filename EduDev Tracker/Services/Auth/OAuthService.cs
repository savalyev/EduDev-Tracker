using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EduDev_Tracker.Services.Auth
{
    public class OAuthService : IOAuthService
    {
        // ── Google ────────────────────────────────────────────────────────────
        private const string GoogleClientId =
            "373522612585-q752dchpnt71usppi8dq6d51ovorcc9j.apps.googleusercontent.com";
        private const string GoogleRedirectUri =
            "com.googleusercontent.apps.373522612585-q752dchpnt71usppi8dq6d51ovorcc9j:/oauth2redirect";

        // ── GitHub ────────────────────────────────────────────────────────────
        private const string GitHubClientId = "Ov23liWdazIpMuRuOu7y";
        private const string GitHubClientSecret = "07eb9df5d9de73b5183f55e075011b112079a8c8";
        private const string GitHubRedirectUri = "com.companyname.edudevtracker://callback";

        private readonly HttpClient _http = new()
        {
            DefaultRequestHeaders = { { "User-Agent", "EduDevTracker/1.0" } }
        };

        // ── Google PKCE ───────────────────────────────────────────────────────

        public async Task<OAuthUserInfo> LoginWithGoogleAsync()
        {
            var codeVerifier  = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(GoogleClientId)}" +
                "&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(GoogleRedirectUri)}" +
                "&scope=openid%20email%20profile" +
                $"&code_challenge={codeChallenge}" +
                "&code_challenge_method=S256");

            var result = await WebAuthenticator.Default
                .AuthenticateAsync(authUrl, new Uri(GoogleRedirectUri));

            var code = result.Properties["code"];

            var token = await ExchangeGoogleCodeAsync(code, codeVerifier);
            var accessToken = token.GetProperty("access_token").GetString()!;

            using var req = new HttpRequestMessage(
                HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            return new OAuthUserInfo(
                Provider:   "google",
                ProviderId: root.GetProperty("sub").GetString()!,
                Name:       root.TryGetProperty("name",    out var n) ? n.GetString() ?? "Google User" : "Google User",
                Email:      root.TryGetProperty("email",   out var e) ? e.GetString() : null,
                AvatarUrl:  root.TryGetProperty("picture", out var p) ? p.GetString() : null);
        }

        private async Task<JsonElement> ExchangeGoogleCodeAsync(string code, string codeVerifier)
        {
            var resp = await _http.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"]          = code,
                    ["client_id"]     = GoogleClientId,
                    ["redirect_uri"]  = GoogleRedirectUri,
                    ["grant_type"]    = "authorization_code",
                    ["code_verifier"] = codeVerifier
                }));

            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        // ── GitHub ────────────────────────────────────────────────────────────

        public async Task<OAuthUserInfo> LoginWithGitHubAsync()
        {
            var state = Guid.NewGuid().ToString("N")[..16];

            var authUrl = new Uri(
                "https://github.com/login/oauth/authorize" +
                $"?client_id={GitHubClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(GitHubRedirectUri)}" +
                "&scope=user:email" +
                $"&state={state}");

            var result = await WebAuthenticator.Default
                .AuthenticateAsync(authUrl, new Uri(GitHubRedirectUri));

            var code = result.Properties["code"];

            var accessToken = await ExchangeGitHubCodeAsync(code);
            return await GetGitHubUserInfoAsync(accessToken);
        }

        private async Task<string> ExchangeGitHubCodeAsync(string code)
        {
            using var req = new HttpRequestMessage(
                HttpMethod.Post, "https://github.com/login/oauth/access_token");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"]     = GitHubClientId,
                ["client_secret"] = GitHubClientSecret,
                ["code"]          = code,
                ["redirect_uri"]  = GitHubRedirectUri
            });

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        private async Task<OAuthUserInfo> GetGitHubUserInfoAsync(string accessToken)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;

            // Если email скрыт — запросим из /user/emails
            if (string.IsNullOrEmpty(email))
                email = await GetGitHubPrimaryEmailAsync(accessToken);

            return new OAuthUserInfo(
                Provider:   "github",
                ProviderId: root.GetProperty("id").GetInt64().ToString(),
                Name:       root.TryGetProperty("name",       out var n)  ? n.GetString()  ?? "GitHub User" : "GitHub User",
                Email:      email,
                AvatarUrl:  root.TryGetProperty("avatar_url", out var av) ? av.GetString() : null);
        }

        private async Task<string?> GetGitHubPrimaryEmailAsync(string accessToken)
        {
            try
            {
                using var req = new HttpRequestMessage(
                    HttpMethod.Get, "https://api.github.com/user/emails");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    if (entry.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                        return entry.TryGetProperty("email", out var em) ? em.GetString() : null;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── PKCE helpers ──────────────────────────────────────────────────────

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}
