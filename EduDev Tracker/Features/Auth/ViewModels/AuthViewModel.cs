using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Services.Auth;
using System.Text.RegularExpressions;
using System.Threading;

namespace EduDev_Tracker.Features.Auth.ViewModels
{
    public partial class AuthViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IOAuthService _oauthService;

        [ObservableProperty] private bool _isLoginMode = true;
        [ObservableProperty] private bool _isRegisterMode;
        [ObservableProperty] private bool _isPasswordVisible;
        [ObservableProperty] private bool _isConfirmPasswordVisible;

        [ObservableProperty] private string _loginEmail = string.Empty;
        [ObservableProperty] private string _loginPassword = string.Empty;

        [ObservableProperty] private string _registerName = string.Empty;
        [ObservableProperty] private string _registerEmail = string.Empty;
        [ObservableProperty] private string _registerPassword = string.Empty;
        [ObservableProperty] private string _registerPasswordConfirm = string.Empty;

        [ObservableProperty] private string _loginEmailError = string.Empty;
        [ObservableProperty] private string _loginPasswordError = string.Empty;
        [ObservableProperty] private string _registerNameError = string.Empty;
        [ObservableProperty] private string _registerEmailError = string.Empty;
        [ObservableProperty] private string _registerPasswordError = string.Empty;
        [ObservableProperty] private string _registerPasswordConfirmError = string.Empty;
        [ObservableProperty] private string _generalError = string.Empty;

        public bool HasLoginEmailError => !string.IsNullOrEmpty(LoginEmailError);
        public bool HasLoginPasswordError => !string.IsNullOrEmpty(LoginPasswordError);
        public bool HasRegisterNameError => !string.IsNullOrEmpty(RegisterNameError);
        public bool HasRegisterEmailError => !string.IsNullOrEmpty(RegisterEmailError);
        public bool HasRegisterPasswordError => !string.IsNullOrEmpty(RegisterPasswordError);
        public bool HasRegisterPasswordConfirmError => !string.IsNullOrEmpty(RegisterPasswordConfirmError);
        public bool HasGeneralError => !string.IsNullOrEmpty(GeneralError);

        [RelayCommand]
        private void TogglePasswordVisible()
        {
            IsPasswordVisible = !IsPasswordVisible;
            OnPropertyChanged(nameof(PasswordEyeIcon));
        }

        [RelayCommand]
        private void ToggleConfirmPasswordVisible()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
            OnPropertyChanged(nameof(ConfirmPasswordEyeIcon));
        }

        public string PasswordEyeIcon => IsPasswordVisible ? "👁" : "🙈";
        public string ConfirmPasswordEyeIcon => IsConfirmPasswordVisible ? "👁" : "🙈";

        public AuthViewModel(IAuthService authService, IOAuthService oauthService)
        {
            _authService  = authService;
            _oauthService = oauthService;
        }

        [RelayCommand]
        private void SwitchToLogin()
        {
            IsLoginMode = true;
            IsRegisterMode = false;
            ClearAllErrors();
        }

        [RelayCommand]
        private void SwitchToRegister()
        {
            IsLoginMode = false;
            IsRegisterMode = true;
            ClearAllErrors();
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (!ValidateLogin()) return;

            IsBusy = true;
            GeneralError = string.Empty;

            try
            {
                var profile = await _authService.LoginAsync(LoginEmail, LoginPassword);
                SessionService.SaveProfileId(profile.Id);
                await NavigateToMainAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message == "WRONG_PASSWORD")
            {
                LoginPasswordError = "Неверный пароль";
                NotifyErrorProperties();
            }
            catch (InvalidOperationException ex) when (ex.Message == "USER_NOT_FOUND")
            {
                GeneralError = "Аккаунт с таким email не найден";
                NotifyErrorProperties();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (!ValidateRegister()) return;

            IsBusy = true;
            GeneralError = string.Empty;

            try
            {
                var profile = await _authService.RegisterAsync(RegisterName, RegisterEmail, RegisterPassword);
                SessionService.SaveProfileId(profile.Id);
                await NavigateToMainAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_TAKEN")
            {
                RegisterEmailError = "Пользователь с таким email уже существует";
                NotifyErrorProperties();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ContinueOfflineAsync()
        {
            IsBusy = true;
            try
            {
                var profile = await _authService.LoginOfflineAsync();
                SessionService.SaveProfileId(profile.Id);
                await NavigateToMainAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Offline] {ex}");
                GeneralError = "Не удалось войти в офлайн-режиме";
            }
            finally
            {
                IsBusy = false;
            }
        }
        private static async Task NavigateToMainAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current!.MainPage = new AppShell();
            });
        }

        private bool ValidateLogin()
        {
            ClearAllErrors();
            bool ok = true;

            if (string.IsNullOrWhiteSpace(LoginEmail))
            {
                LoginEmailError = "Введите email";
                ok = false;
            }
            else if (!IsValidEmail(LoginEmail))
            {
                LoginEmailError = "Некорректный формат email";
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(LoginPassword))
            {
                LoginPasswordError = "Введите пароль";
                ok = false;
            }

            NotifyErrorProperties();
            return ok;
        }

        private bool ValidateRegister()
        {
            ClearAllErrors();
            bool ok = true;

            if (string.IsNullOrWhiteSpace(RegisterName))
            {
                RegisterNameError = "Введите имя";
                ok = false;
            }
            else if (RegisterName.Trim().Length < 2)
            {
                RegisterNameError = "Имя слишком короткое";
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterEmail))
            {
                RegisterEmailError = "Введите email";
                ok = false;
            }
            else if (!IsValidEmail(RegisterEmail))
            {
                RegisterEmailError = "Некорректный формат email";
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterPassword))
            {
                RegisterPasswordError = "Введите пароль";
                ok = false;
            }
            else if (RegisterPassword.Length < 8)
            {
                RegisterPasswordError = "Минимум 8 символов";
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterPasswordConfirm))
            {
                RegisterPasswordConfirmError = "Подтвердите пароль";
                ok = false;
            }
            else if (RegisterPassword != RegisterPasswordConfirm)
            {
                RegisterPasswordConfirmError = "Пароли не совпадают";
                ok = false;
            }

            NotifyErrorProperties();
            return ok;
        }

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private void ClearAllErrors()
        {
            LoginEmailError = string.Empty;
            LoginPasswordError = string.Empty;
            RegisterNameError = string.Empty;
            RegisterEmailError = string.Empty;
            RegisterPasswordError = string.Empty;
            RegisterPasswordConfirmError = string.Empty;
            GeneralError = string.Empty;
            NotifyErrorProperties();
        }

        [RelayCommand]
        private async Task LoginWithGoogleAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            GeneralError = string.Empty;
            try
            {
                var info    = await _oauthService.LoginWithGoogleAsync();
                var profile = await _authService.LoginWithOAuthAsync(info);
                SessionService.SaveProfileId(profile.Id);
                await NavigateToMainAsync();
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (PlatformNotSupportedException)
            {
                GeneralError = "OAuth-вход недоступен на этой платформе";
                NotifyErrorProperties();
            }
            catch (Exception ex)
            {
                GeneralError = "Не удалось войти через Google";
                System.Diagnostics.Debug.WriteLine($"[OAuth Google] {ex}");
                NotifyErrorProperties();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task LoginWithGitHubAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            GeneralError = string.Empty;
            try
            {
                var info    = await _oauthService.LoginWithGitHubAsync();
                var profile = await _authService.LoginWithOAuthAsync(info);
                SessionService.SaveProfileId(profile.Id);
                await NavigateToMainAsync();
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (PlatformNotSupportedException)
            {
                GeneralError = "OAuth-вход недоступен на этой платформе";
                NotifyErrorProperties();
            }
            catch (Exception ex)
            {
                GeneralError = "Не удалось войти через GitHub";
                System.Diagnostics.Debug.WriteLine($"[OAuth GitHub] {ex}");
                NotifyErrorProperties();
            }
            finally { IsBusy = false; }
        }

        private void NotifyErrorProperties()
        {
            OnPropertyChanged(nameof(HasLoginEmailError));
            OnPropertyChanged(nameof(HasLoginPasswordError));
            OnPropertyChanged(nameof(HasRegisterNameError));
            OnPropertyChanged(nameof(HasRegisterEmailError));
            OnPropertyChanged(nameof(HasRegisterPasswordError));
            OnPropertyChanged(nameof(HasRegisterPasswordConfirmError));
            OnPropertyChanged(nameof(HasGeneralError));
        }



    }
}
