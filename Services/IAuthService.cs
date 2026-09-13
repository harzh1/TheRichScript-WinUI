using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    string? Token { get; }
    string? RefreshToken { get; }

    event Action<User?>? UserStateChanged;
    event Action? SessionExpired;

    Task<bool> AutoLoginAsync();
    Task<AuthResponse> LoginAsync(string email, string password);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<bool> RefreshTokenAsync();
    Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
    Task<bool> UploadAvatarAsync(string filePath);
    Task<bool> RemoveAvatarAsync();
    Task<bool> UpdatePasswordAsync(string currentPassword, string newPassword);
    Task<ApiResponse<object>> DeleteAccountAsync(string password);
    Task<ApiResponse<object>> ResetAllDataAsync(string password);
    void Logout();
}

public class AuthSession
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public User? User { get; set; }
}

public class AuthService : IAuthService
{
    private static string SessionFilePath => AppStorageHelper.GetFilePath("session.json");

    private readonly IApiService _apiService;
    private readonly ISettingsService _settingsService;
    private readonly System.Threading.SemaphoreSlim _refreshLock = new(1, 1);

    public User? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && CurrentUser != null;

    public event Action<User?>? UserStateChanged;
    public event Action? SessionExpired;

    public AuthService(IApiService apiService, ISettingsService settingsService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _apiService.SetTokenRefreshHandler(
            async () => await RefreshTokenAsync(),
            () =>
            {
                Logout();
                SessionExpired?.Invoke();
            });
    }

    public async Task<bool> AutoLoginAsync()
    {
        try
        {
            var path = SessionFilePath;
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var session = JsonSerializer.Deserialize<AuthSession>(json);

                if (session != null && !string.IsNullOrWhiteSpace(session.Token))
                {
                    Token = session.Token;
                    RefreshToken = session.RefreshToken;
                    CurrentUser = session.User;
                    _apiService.SetAuthToken(Token);

                    // Fetch fresh profile from backend
                    try
                    {
                        var response = await _apiService.GetAsync<User>("auth/me");
                        if (response.Success && response.Data != null)
                        {
                            CurrentUser = response.Data;
                            SaveSession(Token, RefreshToken, CurrentUser);
                            _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
                            _settingsService.SaveSettings();
                            UserStateChanged?.Invoke(CurrentUser);
                            return true;
                        }
                        else if (response.StatusCode == 401 || !response.Success)
                        {
                            // Access token might be expired. Verify with Refresh Token API!
                            if (!string.IsNullOrWhiteSpace(RefreshToken))
                            {
                                var refreshed = await RefreshTokenAsync();
                                if (refreshed)
                                {
                                    var retryMe = await _apiService.GetAsync<User>("auth/me");
                                    if (retryMe.Success && retryMe.Data != null)
                                    {
                                        CurrentUser = retryMe.Data;
                                        SaveSession(Token, RefreshToken, CurrentUser);
                                        _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
                                        _settingsService.SaveSettings();
                                    }
                                    UserStateChanged?.Invoke(CurrentUser);
                                    return true;
                                }
                            }

                            // Refresh token API failed, was rejected, or token is stale over 30 days
                            Logout();
                            SessionExpired?.Invoke();
                            return false;
                        }
                    }
                    catch
                    {
                        // Use cached profile if network request fails
                        if (CurrentUser != null)
                        {
                            UserStateChanged?.Invoke(CurrentUser);
                            return true;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore auto login errors
        }

        Logout();
        return false;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return false;
            }

            var request = new RefreshTokenRequest { RefreshToken = RefreshToken };
            var response = await _apiService.PostAsync<RefreshTokenResponse>("auth/refresh-token", request);

            if (response.Success && response.Data != null &&
                (!string.IsNullOrWhiteSpace(response.Data.AccessToken) || !string.IsNullOrWhiteSpace(response.Data.Token)))
            {
                Token = response.Data.AccessToken ?? response.Data.Token;
                if (!string.IsNullOrWhiteSpace(response.Data.RefreshToken))
                {
                    RefreshToken = response.Data.RefreshToken;
                }
                if (response.Data.User != null)
                {
                    CurrentUser = response.Data.User;
                }

                _apiService.SetAuthToken(Token);
                SaveSession(Token, RefreshToken, CurrentUser);
                UserStateChanged?.Invoke(CurrentUser);
                return true;
            }

            // Backend verified that the refresh token is stale (>30 days), expired, or revoked
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email.Trim(), Password = password };
        var response = await _apiService.PostAsync<AuthResponse>("auth/login", request);

        if (response.Success && response.Data != null && !string.IsNullOrWhiteSpace(response.Data.Token))
        {
            Token = response.Data.AccessToken ?? response.Data.Token;
            RefreshToken = response.Data.RefreshToken;
            CurrentUser = response.Data.User;
            _apiService.SetAuthToken(Token);

            SaveSession(Token, RefreshToken, CurrentUser);
            if (CurrentUser != null)
            {
                _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
                _settingsService.SaveSettings();
            }

            UserStateChanged?.Invoke(CurrentUser);
            return response.Data;
        }

        return new AuthResponse
        {
            Success = false,
            Error = response.Error ?? "Login failed. Please check your credentials."
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _apiService.PostAsync<AuthResponse>("auth/register", request);

        if (response.Success && response.Data != null && !string.IsNullOrWhiteSpace(response.Data.Token))
        {
            Token = response.Data.AccessToken ?? response.Data.Token;
            RefreshToken = response.Data.RefreshToken;
            CurrentUser = response.Data.User;
            _apiService.SetAuthToken(Token);

            SaveSession(Token, RefreshToken, CurrentUser);
            if (CurrentUser != null)
            {
                _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
                _settingsService.SaveSettings();
            }

            UserStateChanged?.Invoke(CurrentUser);
            return response.Data;
        }

        return new AuthResponse
        {
            Success = false,
            Error = response.Error ?? "Registration failed. Please try again."
        };
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await _apiService.PutAsync<User>("auth/profile", request);
        if (response.Success && response.Data != null)
        {
            CurrentUser = response.Data;
            SaveSession(Token, RefreshToken, CurrentUser);
            _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
            _settingsService.SaveSettings();
            UserStateChanged?.Invoke(CurrentUser);
            return true;
        }
        return false;
    }

    public async Task<bool> UploadAvatarAsync(string filePath)
    {
        var stringFields = new Dictionary<string, string>();
        var files = new List<(string fieldName, string filePath)>
        {
            ("avatar", filePath)
        };

        var response = await _apiService.PutMultipartAsync<User>("auth/avatar", stringFields, files);
        if (response.Success && response.Data != null)
        {
            CurrentUser = response.Data;
            SaveSession(Token, RefreshToken, CurrentUser);
            UserStateChanged?.Invoke(CurrentUser);
            return true;
        }
        return false;
    }

    public async Task<bool> RemoveAvatarAsync()
    {
        var response = await _apiService.DeleteAsync<User>("auth/avatar");
        if (response.Success && response.Data != null)
        {
            CurrentUser = response.Data;
            SaveSession(Token, RefreshToken, CurrentUser);
            UserStateChanged?.Invoke(CurrentUser);
            return true;
        }
        return false;
    }

    public async Task<bool> UpdatePasswordAsync(string currentPassword, string newPassword)
    {
        var request = new UpdatePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        };
        var response = await _apiService.PutAsync<object>("auth/password", request);
        return response.Success;
    }

    public async Task<ApiResponse<object>> DeleteAccountAsync(string password)
    {
        var response = await _apiService.DeleteAsync<object>("auth/account", new { password });
        if (response.Success)
        {
            Logout();
        }
        return response;
    }

    public async Task<ApiResponse<object>> ResetAllDataAsync(string password)
    {
        var response = await _apiService.PostAsync<object>("user/reset-data", new { password });
        if (response.Success)
        {
            try
            {
                var dataCacheService = App.GetService<IDataCacheService>();
                dataCacheService?.ClearAll();
            }
            catch
            {
                // Ignore
            }
        }
        return response;
    }

    public void Logout()
    {
        var currentRefreshToken = RefreshToken;
        Token = null;
        RefreshToken = null;
        CurrentUser = null;
        _apiService.SetAuthToken(null);

        if (!string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _apiService.PostAsync<object>("auth/logout", new { refreshToken = currentRefreshToken });
                }
                catch
                {
                    // Ignore
                }
            });
        }

        try
        {
            var dataCacheService = App.GetService<IDataCacheService>();
            dataCacheService?.ClearAll();
        }
        catch
        {
            // Ignore
        }

        try
        {
            var path = SessionFilePath;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore
        }

        UserStateChanged?.Invoke(null);
    }

    private void SaveSession(string? token, string? refreshToken, User? user)
    {
        try
        {
            var session = new AuthSession { Token = token, RefreshToken = refreshToken, User = user };
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionFilePath, json);
        }
        catch
        {
            // Ignore
        }
    }
}
