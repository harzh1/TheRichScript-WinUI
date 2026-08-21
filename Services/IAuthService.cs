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

    event Action<User?>? UserStateChanged;

    Task<bool> AutoLoginAsync();
    Task<AuthResponse> LoginAsync(string email, string password);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
    Task<bool> UploadAvatarAsync(string filePath);
    Task<bool> RemoveAvatarAsync();
    Task<bool> UpdatePasswordAsync(string currentPassword, string newPassword);
    void Logout();
}

public class AuthSession
{
    public string? Token { get; set; }
    public User? User { get; set; }
}

public class AuthService : IAuthService
{
    private static string SessionFilePath => AppStorageHelper.GetFilePath("session.json");

    private readonly IApiService _apiService;
    private readonly ISettingsService _settingsService;

    public User? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && CurrentUser != null;

    public event Action<User?>? UserStateChanged;

    public AuthService(IApiService apiService, ISettingsService settingsService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
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
                    CurrentUser = session.User;
                    _apiService.SetAuthToken(Token);

                    // Fetch fresh profile from backend
                    try
                    {
                        var response = await _apiService.GetAsync<User>("auth/me");
                        if (response.Success && response.Data != null)
                        {
                            CurrentUser = response.Data;
                            SaveSession(Token, CurrentUser);
                            _settingsService.Settings.PreferredCurrency = CurrentUser.Currency;
                            _settingsService.SaveSettings();
                        }
                    }
                    catch
                    {
                        // Use cached profile if network request fails
                    }

                    if (CurrentUser != null)
                    {
                        UserStateChanged?.Invoke(CurrentUser);
                        return true;
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

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email.Trim(), Password = password };
        var response = await _apiService.PostAsync<AuthResponse>("auth/login", request);

        if (response.Success && response.Data != null && !string.IsNullOrWhiteSpace(response.Data.Token))
        {
            Token = response.Data.Token;
            CurrentUser = response.Data.User;
            _apiService.SetAuthToken(Token);

            SaveSession(Token, CurrentUser);
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
            Token = response.Data.Token;
            CurrentUser = response.Data.User;
            _apiService.SetAuthToken(Token);

            SaveSession(Token, CurrentUser);
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
            SaveSession(Token, CurrentUser);
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
            SaveSession(Token, CurrentUser);
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
            SaveSession(Token, CurrentUser);
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

    public void Logout()
    {
        Token = null;
        CurrentUser = null;
        _apiService.SetAuthToken(null);

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

    private void SaveSession(string? token, User? user)
    {
        try
        {
            var session = new AuthSession { Token = token, User = user };
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionFilePath, json);
        }
        catch
        {
            // Ignore
        }
    }
}
