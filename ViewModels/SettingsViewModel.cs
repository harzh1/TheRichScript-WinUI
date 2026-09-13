using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ISettingsService _settingsService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IBusyIndicatorService _busyIndicatorService;
    private readonly IDataCacheService _dataCacheService;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string country = "IN";

    [ObservableProperty]
    private string currency = "INR";

    [ObservableProperty]
    private string? avatarUrl;

    [ObservableProperty]
    private ImageSource? profilePictureImage;

    [ObservableProperty]
    private bool hasAvatar;

    [ObservableProperty]
    private string initials = "U";

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    public string AppVersion
    {
        get
        {
            try
            {
                var package = Windows.ApplicationModel.Package.Current;
                var v = package.Id.Version;
                return $"v{v.Major}.{v.Minor}.{v.Build}";
            }
            catch
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return v != null ? $"v{v.Major}.{v.Minor}.{v.Build}" : "v0.1.3";
            }
        }
    }

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string selectedTheme = "Default";

    [ObservableProperty]
    private string apiBaseUrl = AppSettings.DefaultProductionApiUrl;

    [ObservableProperty]
    private bool isSavingProfile;

    [ObservableProperty]
    private bool isUploadingAvatar;

    [ObservableProperty]
    private bool isUpdatingPassword;

    [ObservableProperty]
    private bool isSyncing;

    [ObservableProperty]
    private bool isResettingData;

    [ObservableProperty]
    private bool isDeletingAccount;

    public IReadOnlyList<CountryInfo> AllCountries => CountryCurrencyData.AllCountries;
    public IReadOnlyList<CurrencyInfo> AllCurrencies => CountryCurrencyData.AllCurrencies;
    public List<string> AvailableThemes { get; } = new() { "Default", "Light", "Dark" };

    public SettingsViewModel(
        IAuthService authService,
        ISettingsService settingsService,
        IAnalyticsService analyticsService,
        IDialogService dialogService,
        INavigationService navigationService,
        IBusyIndicatorService busyIndicatorService,
        IDataCacheService dataCacheService)
    {
        _authService = authService;
        _settingsService = settingsService;
        _analyticsService = analyticsService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _busyIndicatorService = busyIndicatorService;
        _dataCacheService = dataCacheService;

        _authService.UserStateChanged += user =>
        {
            if (user != null)
            {
                UpdateUserProperties(user);
            }
        };

        LoadCurrentSettings();
    }

    public void LoadCurrentSettings()
    {
        var user = _authService.CurrentUser;
        if (user != null)
        {
            UpdateUserProperties(user);
        }

        SelectedTheme = _settingsService.Settings.AppTheme;
        ApiBaseUrl = _settingsService.Settings.ApiBaseUrl;
    }

    private void UpdateUserProperties(User user)
    {
        FirstName = user.FirstName;
        LastName = user.LastName ?? string.Empty;
        Email = user.Email;
        Country = user.Country;
        Currency = user.Currency;
        AvatarUrl = user.AvatarUrl;
        Initials = user.Initials;
        HasAvatar = !string.IsNullOrWhiteSpace(user.AvatarUrl);

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl) && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out var uri))
        {
            ProfilePictureImage = new BitmapImage(uri);
        }
        else
        {
            ProfilePictureImage = null;
        }
    }

    [RelayCommand]
    public async Task PreviewAvatarAsync(XamlRoot? xamlRoot = null)
    {
        var user = _authService.CurrentUser;
        if (user != null && !string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            var dialog = new AvatarPreviewDialog(user);
            if (xamlRoot != null)
            {
                dialog.XamlRoot = xamlRoot;
            }
            else if (App.MainWindowInstance?.Content?.XamlRoot != null)
            {
                dialog.XamlRoot = App.MainWindowInstance.Content.XamlRoot;
            }

            var result = await dialog.ShowAsync();
            if (dialog.ChangePhotoRequested)
            {
                await UploadAvatarAsync(xamlRoot);
            }
        }
        else
        {
            await UploadAvatarAsync(xamlRoot);
        }
    }

    [RelayCommand]
    public async Task UploadAvatarAsync(XamlRoot? xamlRoot = null)
    {
        if (IsUploadingAvatar) return;

        try
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            // Open 360x360 Crop & Zoom Selector
            var cropDialog = new CropAvatarDialog(file.Path);
            if (xamlRoot != null)
            {
                cropDialog.XamlRoot = xamlRoot;
            }
            else if (App.MainWindowInstance?.Content?.XamlRoot != null)
            {
                cropDialog.XamlRoot = App.MainWindowInstance.Content.XamlRoot;
            }

            var cropResult = await cropDialog.ShowAsync();
            if (cropResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(cropDialog.CroppedFilePath))
            {
                return;
            }

            IsUploadingAvatar = true;

            using (_busyIndicatorService.BeginBusy("Uploading profile photo..."))
            {
                var success = await _authService.UploadAvatarAsync(cropDialog.CroppedFilePath);
                if (success)
                {
                    if (_authService.CurrentUser != null)
                    {
                        UpdateUserProperties(_authService.CurrentUser);
                    }
                    await _dialogService.ShowMessageAsync("Photo Updated", "Your profile photo has been updated successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Failed to upload profile photo. Please try again.");
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, "Error uploading photo");
        }
        finally
        {
            IsUploadingAvatar = false;
        }
    }

    [RelayCommand]
    public async Task RemoveAvatarAsync()
    {
        if (IsUploadingAvatar) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Remove Profile Photo",
            "Are you sure you want to remove your profile photo?",
            "Remove",
            "Cancel");

        if (!confirmed) return;

        IsUploadingAvatar = true;

        using (_busyIndicatorService.BeginBusy("Removing profile photo..."))
        {
            try
            {
                var success = await _authService.RemoveAvatarAsync();
                if (success)
                {
                    if (_authService.CurrentUser != null)
                    {
                        UpdateUserProperties(_authService.CurrentUser);
                    }
                    await _dialogService.ShowMessageAsync("Photo Removed", "Your profile photo has been removed.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Failed to remove profile photo.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsUploadingAvatar = false;
            }
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            await _dialogService.ShowErrorAsync("First name is required.");
            return;
        }

        IsSavingProfile = true;

        using (_busyIndicatorService.BeginBusy("Saving profile changes..."))
        {
            try
            {
                var req = new UpdateProfileRequest
                {
                    FirstName = FirstName.Trim(),
                    LastName = string.IsNullOrWhiteSpace(LastName) ? null : LastName.Trim(),
                    Country = Country?.Trim().ToUpperInvariant() ?? "IN",
                    Currency = Currency?.Trim().ToUpperInvariant() ?? "INR"
                };

                var success = await _authService.UpdateProfileAsync(req);
                if (success)
                {
                    await _dialogService.ShowMessageAsync("Success", "Profile updated successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Failed to update profile.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsSavingProfile = false;
            }
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            await _dialogService.ShowErrorAsync("Please enter your current password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            await _dialogService.ShowErrorAsync("New password must be at least 6 characters.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            await _dialogService.ShowErrorAsync("New passwords do not match.");
            return;
        }

        IsUpdatingPassword = true;

        using (_busyIndicatorService.BeginBusy("Updating account password..."))
        {
            try
            {
                var success = await _authService.UpdatePasswordAsync(CurrentPassword, NewPassword);
                if (success)
                {
                    CurrentPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                    await _dialogService.ShowMessageAsync("Success", "Password changed successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Failed to change password. Please verify your current password.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsUpdatingPassword = false;
            }
        }
    }

    [RelayCommand]
    private void SaveAppSettings()
    {
        _settingsService.Settings.AppTheme = SelectedTheme;
        _settingsService.Settings.ApiBaseUrl = ApiBaseUrl;
        _settingsService.Settings.PreferredCurrency = Currency;
        _settingsService.SaveSettings();

        _dialogService.ShowMessageAsync("Settings Saved", "Application settings have been saved.");
    }

    public void SaveThemePreference()
    {
        if (!string.IsNullOrEmpty(SelectedTheme))
        {
            App.SetAppTheme(SelectedTheme);
            _settingsService.Settings.AppTheme = SelectedTheme;
            _settingsService.SaveSettings();
        }
    }

    [RelayCommand]
    private void SetProductionApiUrl()
    {
        ApiBaseUrl = AppSettings.DefaultProductionApiUrl;
    }

    [RelayCommand]
    private void SetLocalApiUrl()
    {
        ApiBaseUrl = AppSettings.LocalApiUrl;
    }

    [RelayCommand]
    private async Task RunCronJobsAsync()
    {
        if (IsSyncing) return;
        IsSyncing = true;

        using (_busyIndicatorService.BeginBusy("Executing recurring schedules..."))
        {
            try
            {
                var response = await _analyticsService.TriggerCronJobsAsync();
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.All);
                    await _dialogService.ShowMessageAsync("Cron Jobs Executed", "Recurring plans and auto-funding schedules processed successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Cron execution failed.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }

    [RelayCommand]
    public async Task ResetDataAsync(XamlRoot? xamlRoot = null)
    {
        if (IsResettingData || IsDeletingAccount) return;

        var dialog = new PasswordConfirmationDialog(
            title: "Reset All Financial Data",
            message: "This will permanently erase all your transactions, assets, debts, budgets, and savings goals. Your account profile, email, and password will remain active. Are you sure?",
            primaryButtonText: "Reset All Data",
            warningTitle: "Irreversible Action Warning",
            warningMessage: "All recorded financial data across your workspace will be wiped permanently.");

        if (xamlRoot != null)
        {
            dialog.XamlRoot = xamlRoot;
        }
        else if (App.MainWindowInstance?.Content?.XamlRoot != null)
        {
            dialog.XamlRoot = App.MainWindowInstance.Content.XamlRoot;
        }

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.ConfirmedPassword))
        {
            return;
        }

        IsResettingData = true;
        using (_busyIndicatorService.BeginBusy("Resetting all financial records..."))
        {
            try
            {
                var response = await _authService.ResetAllDataAsync(dialog.ConfirmedPassword);
                if (response.Success)
                {
                    await _dialogService.ShowMessageAsync(
                        "Data Reset Complete",
                        "All transactions, assets, budgets, debts, and goals have been successfully reset.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to reset data. Please check your password.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message, "Error Resetting Data");
            }
            finally
            {
                IsResettingData = false;
            }
        }
    }

    [RelayCommand]
    public async Task DeleteAccountAsync(XamlRoot? xamlRoot = null)
    {
        if (IsResettingData || IsDeletingAccount) return;

        var dialog = new PasswordConfirmationDialog(
            title: "Delete Account Permanently",
            message: "This will permanently delete your account, your profile, and all associated financial records from our cloud servers. You will be logged out and cannot recover this data. Are you sure you want to proceed?",
            primaryButtonText: "Delete Account Forever",
            warningTitle: "Critical Danger: Account Deletion",
            warningMessage: "This will completely destroy your account and all cloud data. This action CANNOT be reversed.");

        if (xamlRoot != null)
        {
            dialog.XamlRoot = xamlRoot;
        }
        else if (App.MainWindowInstance?.Content?.XamlRoot != null)
        {
            dialog.XamlRoot = App.MainWindowInstance.Content.XamlRoot;
        }

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.ConfirmedPassword))
        {
            return;
        }

        IsDeletingAccount = true;
        using (_busyIndicatorService.BeginBusy("Permanently deleting account..."))
        {
            try
            {
                var response = await _authService.DeleteAccountAsync(dialog.ConfirmedPassword);
                if (response.Success)
                {
                    await _dialogService.ShowMessageAsync(
                        "Account Deleted",
                        "Your account and all personal and financial data have been permanently removed.");
                    _navigationService.NavigateRoot(typeof(Views.LoginPage));
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to delete account. Please check your password.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message, "Error Deleting Account");
            }
            finally
            {
                IsDeletingAccount = false;
            }
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        _navigationService.NavigateRoot(typeof(Views.LoginPage));
    }
}
