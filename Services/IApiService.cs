using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TheRichScript_WinUI.Services;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; }
}

public interface IApiService
{
    void SetAuthToken(string? token);
    string? GetAuthToken();
    void SetTokenRefreshHandler(Func<Task<bool>> refreshHandler, Action onSessionExpired);
    Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null);
    Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, object? body = null);
    Task<ApiResponse<T>> PostMultipartAsync<T>(string endpoint, Dictionary<string, string> stringFields, List<(string fieldName, string filePath)> files);
    Task<ApiResponse<T>> PutMultipartAsync<T>(string endpoint, Dictionary<string, string> stringFields, List<(string fieldName, string filePath)> files);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private string? _authToken;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
    }

    public void SetAuthToken(string? token)
    {
        _authToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public string? GetAuthToken() => _authToken;

    private Func<Task<bool>>? _tokenRefreshHandler;
    private Action? _onSessionExpired;

    public void SetTokenRefreshHandler(Func<Task<bool>> refreshHandler, Action onSessionExpired)
    {
        _tokenRefreshHandler = refreshHandler;
        _onSessionExpired = onSessionExpired;
    }

    private Uri GetUri(string endpoint)
    {
        var baseUrl = _settingsService.Settings.ApiBaseUrl.TrimEnd('/');
        var cleanEndpoint = endpoint.TrimStart('/');
        return new Uri($"{baseUrl}/{cleanEndpoint}");
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint) => await GetInternalAsync<T>(endpoint, isRetry: false);

    private async Task<ApiResponse<T>> GetInternalAsync<T>(string endpoint, bool isRetry)
    {
        try
        {
            var uri = GetUri(endpoint);
            LogHttp($"GET {uri}");
            using var response = await _httpClient.GetAsync(uri);
            return await HandleResponseAsync<T>(response, $"GET {uri}", isRetry ? null : () => GetInternalAsync<T>(endpoint, true));
        }
        catch (Exception ex)
        {
            LogHttp($"GET {endpoint} EXCEPTION: {ex.GetType()}: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null) => await PostInternalAsync<T>(endpoint, body, isRetry: false);

    private async Task<ApiResponse<T>> PostInternalAsync<T>(string endpoint, object? body, bool isRetry)
    {
        try
        {
            var uri = GetUri(endpoint);
            HttpContent? content = null;
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else
            {
                content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            LogHttp($"POST {uri}");
            using var response = await _httpClient.PostAsync(uri, content);
            return await HandleResponseAsync<T>(response, $"POST {uri}", isRetry ? null : () => PostInternalAsync<T>(endpoint, body, true));
        }
        catch (Exception ex)
        {
            LogHttp($"POST {endpoint} EXCEPTION: {ex.GetType()}: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null) => await PutInternalAsync<T>(endpoint, body, isRetry: false);

    private async Task<ApiResponse<T>> PutInternalAsync<T>(string endpoint, object? body, bool isRetry)
    {
        try
        {
            var uri = GetUri(endpoint);
            HttpContent? content = null;
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else
            {
                content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            LogHttp($"PUT {uri}");
            using var response = await _httpClient.PutAsync(uri, content);
            return await HandleResponseAsync<T>(response, $"PUT {uri}", isRetry ? null : () => PutInternalAsync<T>(endpoint, body, true));
        }
        catch (Exception ex)
        {
            LogHttp($"PUT {endpoint} EXCEPTION: {ex.GetType()}: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, object? body = null) => await DeleteInternalAsync<T>(endpoint, body, isRetry: false);

    private async Task<ApiResponse<T>> DeleteInternalAsync<T>(string endpoint, object? body, bool isRetry)
    {
        try
        {
            var uri = GetUri(endpoint);
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            LogHttp($"DELETE {uri}");
            using var response = await _httpClient.SendAsync(request);
            return await HandleResponseAsync<T>(response, $"DELETE {uri}", isRetry ? null : () => DeleteInternalAsync<T>(endpoint, body, true));
        }
        catch (Exception ex)
        {
            LogHttp($"DELETE {endpoint} EXCEPTION: {ex.GetType()}: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    public async Task<ApiResponse<T>> PostMultipartAsync<T>(string endpoint, Dictionary<string, string> stringFields, List<(string fieldName, string filePath)> files)
    {
        return await SendMultipartAsync<T>(HttpMethod.Post, endpoint, stringFields, files, isRetry: false);
    }

    public async Task<ApiResponse<T>> PutMultipartAsync<T>(string endpoint, Dictionary<string, string> stringFields, List<(string fieldName, string filePath)> files)
    {
        return await SendMultipartAsync<T>(HttpMethod.Put, endpoint, stringFields, files, isRetry: false);
    }

    private async Task<ApiResponse<T>> SendMultipartAsync<T>(HttpMethod method, string endpoint, Dictionary<string, string> stringFields, List<(string fieldName, string filePath)> files, bool isRetry)
    {
        try
        {
            var uri = GetUri(endpoint);
            using var formData = new MultipartFormDataContent();

            foreach (var kvp in stringFields)
            {
                formData.Add(new StringContent(kvp.Value), kvp.Key);
            }

            var fileStreams = new List<FileStream>();
            try
            {
                foreach (var (fieldName, filePath) in files)
                {
                    if (File.Exists(filePath))
                    {
                        var fs = File.OpenRead(filePath);
                        fileStreams.Add(fs);
                        var fileContent = new StreamContent(fs);
                        var fileName = Path.GetFileName(filePath);
                        formData.Add(fileContent, fieldName, fileName);
                    }
                }

                LogHttp($"{method} (Multipart) {uri}");
                using var request = new HttpRequestMessage(method, uri) { Content = formData };
                using var response = await _httpClient.SendAsync(request);
                return await HandleResponseAsync<T>(response, $"{method} (Multipart) {uri}", isRetry ? null : () => SendMultipartAsync<T>(method, endpoint, stringFields, files, true));
            }
            finally
            {
                foreach (var fs in fileStreams)
                {
                    fs.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            LogHttp($"{method} (Multipart) {endpoint} EXCEPTION: {ex.GetType()}: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    private async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response, string requestContext, Func<Task<ApiResponse<T>>>? retryFunc = null)
    {
        var rawContent = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        LogHttp($"RESPONSE [{statusCode}] {requestContext} -> {rawContent}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
            retryFunc != null &&
            _tokenRefreshHandler != null &&
            !requestContext.Contains("auth/login", StringComparison.OrdinalIgnoreCase) &&
            !requestContext.Contains("auth/register", StringComparison.OrdinalIgnoreCase) &&
            !requestContext.Contains("auth/refresh", StringComparison.OrdinalIgnoreCase))
        {
            LogHttp($"401 Unauthorized for {requestContext}. Attempting token refresh via Refresh Token API...");
            var refreshed = await _tokenRefreshHandler();
            if (refreshed)
            {
                LogHttp($"Token refresh succeeded. Retrying request: {requestContext}...");
                return await retryFunc();
            }
            else
            {
                LogHttp($"Token refresh rejected by backend (expired >30 days, revoked, or invalid). Session expired.");
                _onSessionExpired?.Invoke();
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(rawContent);
            var root = doc.RootElement;

            var result = new ApiResponse<T>
            {
                StatusCode = statusCode,
                Success = response.IsSuccessStatusCode
            };

            if (root.TryGetProperty("success", out var successProp))
            {
                result.Success = successProp.GetBoolean();
            }

            if (root.TryGetProperty("message", out var msgProp))
            {
                result.Message = msgProp.GetString();
            }

            if (root.TryGetProperty("error", out var errProp))
            {
                result.Error = CleanErrorMessage(errProp.GetString() ?? "", response);
            }

            // Deserialization strategy based on type and payload shape
            if (typeof(T) == typeof(Models.AuthResponse) || typeof(T) == typeof(Models.TransactionListResponse))
            {
                result.Data = JsonSerializer.Deserialize<T>(rawContent, JsonOptions);
            }
            else if (root.TryGetProperty("data", out var dataProp))
            {
                result.Data = JsonSerializer.Deserialize<T>(dataProp.GetRawText(), JsonOptions);
            }
            else if (root.TryGetProperty("user", out var userProp))
            {
                if (typeof(T) == typeof(Models.User))
                {
                    result.Data = JsonSerializer.Deserialize<T>(userProp.GetRawText(), JsonOptions);
                }
                else
                {
                    try
                    {
                        result.Data = JsonSerializer.Deserialize<T>(rawContent, JsonOptions);
                    }
                    catch
                    {
                        result.Data = JsonSerializer.Deserialize<T>(userProp.GetRawText(), JsonOptions);
                    }
                }
            }
            else if (result.Success && typeof(T) != typeof(object))
            {
                // Try deserializing root directly
                try
                {
                    result.Data = JsonSerializer.Deserialize<T>(rawContent, JsonOptions);
                }
                catch
                {
                    // Ignore
                }
            }

            return result;
        }
        catch
        {
            return new ApiResponse<T>
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = statusCode,
                Error = CleanErrorMessage(rawContent, response)
            };
        }
    }

    private static string CleanErrorMessage(string rawContent, HttpResponseMessage response)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";
        }

        if (rawContent.TrimStart().StartsWith("<", StringComparison.Ordinal))
        {
            var preMatch = System.Text.RegularExpressions.Regex.Match(rawContent, @"<pre>(.*?)</pre>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (preMatch.Success && !string.IsNullOrWhiteSpace(preMatch.Groups[1].Value))
            {
                return preMatch.Groups[1].Value.Trim();
            }

            var titleMatch = System.Text.RegularExpressions.Regex.Match(rawContent, @"<title>(.*?)</title>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (titleMatch.Success && !string.IsNullOrWhiteSpace(titleMatch.Groups[1].Value))
            {
                return $"{titleMatch.Groups[1].Value.Trim()} (HTTP {(int)response.StatusCode})";
            }

            return $"Server Error {(int)response.StatusCode}: {response.ReasonPhrase}";
        }

        return rawContent;
    }

    private static void LogHttp(string message)
    {
        try
        {
            var logPath = AppStorageHelper.GetFilePath("app.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [HTTP] {message}\n");
        }
        catch { }
    }
}
