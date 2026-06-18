using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Auth.Views;
using EduDev_Tracker.Services.Auth;

namespace EduDev_Tracker.Features.Profile.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly ProfileRepository _profileRepo;
        private readonly IAuthService      _authService;
        private readonly DatabaseService   _db;
        private readonly IServiceProvider  _services;

        private const string KeyLanguage       = "setting_language";
        private const string KeySmartReminders = "setting_smart_reminders";
        private const string KeySoundVibration = "setting_sound_vibration";

        // ── Профиль ───────────────────────────────────────────────────────────
        [ObservableProperty] private string userName        = "Пользователь";
        [ObservableProperty] private string userEmail       = "—";
        [ObservableProperty] private string accountTypeLabel = "Локальный режим";
        [ObservableProperty] private string accountTypeBg   = "#1E293B";
        [ObservableProperty] private string accountTypeColor = "#94A3B8";
        [ObservableProperty] private string memberSince     = "—";
        [ObservableProperty] private string avatarInitials  = "?";
        [ObservableProperty] private Color  avatarColor     = Color.FromArgb("#3B82F6");

        // ── Статистика ────────────────────────────────────────────────────────
        [ObservableProperty] private int habitsCount   = 0;
        [ObservableProperty] private int tasksCount    = 0;
        [ObservableProperty] private int notesCount    = 0;
        [ObservableProperty] private int pomodoroCount = 0;

        // ── Настройки ─────────────────────────────────────────────────────────
        [ObservableProperty] private string selectedLanguage      = "Русский";
        [ObservableProperty] private bool   smartRemindersEnabled = false;
        [ObservableProperty] private bool   soundVibrationEnabled = true;

        public List<string> Languages { get; } = ["Русский", "English"];

        public ProfileViewModel(
            ProfileRepository profileRepo,
            IAuthService authService,
            DatabaseService db,
            IServiceProvider services)
        {
            _profileRepo = profileRepo;
            _authService = authService;
            _db          = db;
            _services    = services;
        }

        public async Task InitializeAsync()
        {
            int profileId = SessionService.GetProfileId();
            if (profileId == 0) return;

            var profile = await _profileRepo.GetByIdAsync(profileId);
            if (profile is null) return;

            // Профиль
            UserName = string.IsNullOrWhiteSpace(profile.Name) ? "Пользователь" : profile.Name;
            UserEmail = string.IsNullOrWhiteSpace(profile.Email) ? "Без email" : profile.Email;
            AvatarInitials = GetInitials(UserName);
            AvatarColor    = GetAvatarColor(UserName);

            (AccountTypeLabel, AccountTypeBg, AccountTypeColor) = profile.OAuthProvider switch
            {
                "google" => ("Google аккаунт", "#1A2A40", "#4285F4"),
                "github" => ("GitHub аккаунт", "#1A1F2A", "#A0A0A0"),
                _ when profile.IsLocal => ("Локальный режим", "#1E293B", "#94A3B8"),
                _ => ("Email аккаунт", "#1A2540", "#60A5FA")
            };

            MemberSince = profile.CreatedAt.ToString("d MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));

            // Статистика
            var conn = _db.Connection;
            HabitsCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM habits WHERE ProfileId=? AND IsArchived=0", profileId);
            TasksCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM tasks WHERE ProfileId=? AND IsArchived=0 AND Status!='Done'", profileId);
            NotesCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM notes WHERE ProfileId=? AND IsArchived=0", profileId);
            PomodoroCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM pomodoro_sessions WHERE ProfileId=? AND Phase='Work' AND WasInterrupted=0",
                profileId);

            // Настройки
            var prefix = $"_{profileId}";
            SelectedLanguage      = Preferences.Default.Get(KeyLanguage       + prefix, "Русский");
            SmartRemindersEnabled = Preferences.Default.Get(KeySmartReminders + prefix, false);
            SoundVibrationEnabled = Preferences.Default.Get(KeySoundVibration + prefix, true);
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            var prefix = $"_{SessionService.GetProfileId()}";
            Preferences.Default.Set(KeyLanguage + prefix, value);
        }

        partial void OnSmartRemindersEnabledChanged(bool value)
        {
            var prefix = $"_{SessionService.GetProfileId()}";
            Preferences.Default.Set(KeySmartReminders + prefix, value);
        }

        partial void OnSoundVibrationEnabledChanged(bool value)
        {
            var prefix = $"_{SessionService.GetProfileId()}";
            Preferences.Default.Set(KeySoundVibration + prefix, value);
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Выход", "Выйти из аккаунта?", "Выйти", "Отмена");
            if (!confirm) return;

            await _authService.LogoutAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.MainPage = new NavigationPage(
                    _services.GetRequiredService<AuthPage>())
                {
                    BarBackgroundColor = Color.FromArgb("#0A0E1A"),
                    BarTextColor       = Color.FromArgb("#E8EAF0")
                };
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
                : char.ToUpper(parts[0][0]).ToString();
        }

        private static Color GetAvatarColor(string name)
        {
            var colors = new[]
            {
                "#3B82F6", "#10B981", "#8B5CF6",
                "#F59E0B", "#EF4444", "#06B6D4", "#EC4899"
            };
            if (string.IsNullOrEmpty(name)) return Color.FromArgb(colors[0]);
            return Color.FromArgb(colors[char.ToUpper(name[0]) % colors.Length]);
        }
    }
}
