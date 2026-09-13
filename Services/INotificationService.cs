using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface INotificationService
{
    Task<NotificationListResponse> GetNotificationsAsync(int page = 1, int limit = 20, bool unreadOnly = false);
    Task<ApiResponse<object>> MarkAsReadAsync(string id);
    Task<ApiResponse<object>> MarkAllAsReadAsync();
    Task<ApiResponse<object>> DeleteNotificationAsync(string id);
}

public class NotificationService : INotificationService
{
    private readonly IApiService _apiService;

    public NotificationService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(int page = 1, int limit = 20, bool unreadOnly = false)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"limit={limit}"
        };

        if (unreadOnly)
        {
            queryParams.Add("unreadOnly=true");
        }

        var endpoint = $"notifications?{string.Join("&", queryParams)}";
        var response = await _apiService.GetAsync<NotificationListResponse>(endpoint);

        if (response.Success && response.Data != null)
        {
            return response.Data;
        }

        return new NotificationListResponse
        {
            Success = false,
            Error = response.Error ?? "Failed to fetch notifications"
        };
    }

    public async Task<ApiResponse<object>> MarkAsReadAsync(string id)
    {
        return await _apiService.PutAsync<object>($"notifications/{id}/read");
    }

    public async Task<ApiResponse<object>> MarkAllAsReadAsync()
    {
        return await _apiService.PutAsync<object>("notifications/read-all");
    }

    public async Task<ApiResponse<object>> DeleteNotificationAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"notifications/{id}");
    }
}
