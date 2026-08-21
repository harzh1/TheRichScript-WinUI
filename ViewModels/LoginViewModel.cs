using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingStatusText = "Sign In";

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string apiBaseUrl;

    public LoginViewModel(
        IAuthService authService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;
        apiBaseUrl = _settingsService.Settings.ApiBaseUrl;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            _settingsService.Settings.ApiBaseUrl = ApiBaseUrl.Trim();
            _settingsService.SaveSettings();
        }

        IsLoading = true;
        LoadingStatusText = "Signing in...";

        try
        {
            LogMessage($"Attempting login for: {Email.Trim()} on endpoint: {_settingsService.Settings.ApiBaseUrl}");
            var response = await _authService.LoginAsync(Email, Password);
            if (response.Success)
            {
                LogMessage($"Login succeeded for: {Email.Trim()}");
                LoadingStatusText = "Loading data...";
                await _dataCacheService.WarmUpAllDataAsync();
                _navigationService.NavigateRoot(typeof(Views.ShellPage));
            }
            else
            {
                ErrorMessage = response.Error ?? "Login failed. Please check your credentials.";
                LogMessage($"Login failed for: {Email.Trim()} - Error: {ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Network or server error: {ex.Message}";
            LogMessage($"Login exception: {ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
            LoadingStatusText = "Sign In";
        }
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        _navigationService.NavigateRoot(typeof(Views.RegisterPage));
    }

    private static void LogMessage(string message)
    {
        try
        {
            var logPath = AppStorageHelper.GetFilePath("app.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOGIN] {message}\n");
        }
        catch { }
    }
}
