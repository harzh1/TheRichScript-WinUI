using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;

namespace TheRichScript_WinUI.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDataCacheService _dataCacheService;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string country = "IN";

    [ObservableProperty]
    private string currency = "INR";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public RegisterViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IDataCacheService dataCacheService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _dataCacheService = dataCacheService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "First name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email address is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters.";
            return;
        }

        IsLoading = true;

        try
        {
            var req = new RegisterRequest
            {
                FirstName = FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(LastName) ? null : LastName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                Country = Country,
                Currency = Currency
            };

            var response = await _authService.RegisterAsync(req);
            if (response.Success)
            {
                await _dataCacheService.WarmUpAllDataAsync();
                _navigationService.NavigateRoot(typeof(Views.ShellPage));
            }
            else
            {
                ErrorMessage = response.Error ?? "Registration failed.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        _navigationService.NavigateRoot(typeof(Views.LoginPage));
    }
}
