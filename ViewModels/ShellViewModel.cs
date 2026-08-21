using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IDataCacheService _dataCacheService;
    private readonly IBusyIndicatorService _busyIndicatorService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(DisplayEmail))]
    [NotifyPropertyChangedFor(nameof(Initials))]
    [NotifyPropertyChangedFor(nameof(ProfilePictureImage))]
    private User? currentUser;

    public string DisplayName => CurrentUser?.FullName ?? "Account User";
    public string DisplayEmail => CurrentUser?.DisplayEmail ?? "user@therichscript.local";
    public string Initials => CurrentUser?.Initials ?? "U";

    public ImageSource? ProfilePictureImage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CurrentUser?.AvatarUrl) && Uri.TryCreate(CurrentUser.AvatarUrl, UriKind.Absolute, out var uri))
            {
                return new BitmapImage(uri);
            }
            return null;
        }
    }

    [ObservableProperty]
    private string pageTitle = "Dashboard";

    [ObservableProperty]
    private bool isSyncing;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string busyMessage = "Processing request...";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    public ShellViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IAnalyticsService analyticsService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IDataCacheService dataCacheService,
        IBusyIndicatorService busyIndicatorService)
    {
        _navigationService = navigationService;
        _authService = authService;
        _analyticsService = analyticsService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _dataCacheService = dataCacheService;
        _busyIndicatorService = busyIndicatorService;

        CurrentUser = _authService.CurrentUser;
        _authService.UserStateChanged += user =>
        {
            CurrentUser = user;
        };

        _busyIndicatorService.BusyStateChanged += (busy, msg) =>
        {
            IsBusy = busy;
            BusyMessage = msg ?? "Processing request...";
        };
    }

    [RelayCommand]
    private async Task TriggerSyncAsync()
    {
        if (IsSyncing) return;
        IsSyncing = true;

        using (_busyIndicatorService.BeginBusy("Running background synchronization..."))
        {
            try
            {
                var response = await _analyticsService.TriggerCronJobsAsync();
                if (response.Success)
                {
                    _dataCacheService.Invalidate(DataCacheType.All);
                    await _dialogService.ShowMessageAsync("Sync Complete", "Recurring budgets and auto-funded goals have been processed successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(response.Error ?? "Failed to run background sync.");
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
    private void Logout()
    {
        _authService.Logout();
        _navigationService.NavigateRoot(typeof(Views.LoginPage));
    }
}
