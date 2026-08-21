using System;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IAnalyticsService
{
    Task<ApiResponse<AnalyticsSummary>> GetAnalyticsSummaryAsync(string? month = null, int? tzOffset = null);
    Task<ApiResponse<object>> TriggerCronJobsAsync();
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IApiService _apiService;

    public AnalyticsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<AnalyticsSummary>> GetAnalyticsSummaryAsync(string? month = null, int? tzOffset = null)
    {
        var offset = tzOffset ?? -(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
        var endpoint = $"analytics?tzOffset={offset}";
        if (!string.IsNullOrWhiteSpace(month))
        {
            endpoint += $"&month={month}";
        }

        return await _apiService.GetAsync<AnalyticsSummary>(endpoint);
    }

    public async Task<ApiResponse<object>> TriggerCronJobsAsync()
    {
        return await _apiService.PostAsync<object>("cron/trigger");
    }
}
