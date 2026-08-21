using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IPlanService
{
    Task<ApiResponse<List<PlannedExpense>>> GetPlansAsync();
    Task<ApiResponse<PlannedExpense>> CreatePlanAsync(CreatePlanRequest request);
    Task<ApiResponse<PlannedExpense>> UpdatePlanAsync(string id, UpdatePlanRequest request);
    Task<ApiResponse<object>> DeletePlanAsync(string id);
}

public class PlanService : IPlanService
{
    private readonly IApiService _apiService;

    public PlanService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<PlannedExpense>>> GetPlansAsync()
    {
        return await _apiService.GetAsync<List<PlannedExpense>>("plans");
    }

    public async Task<ApiResponse<PlannedExpense>> CreatePlanAsync(CreatePlanRequest request)
    {
        return await _apiService.PostAsync<PlannedExpense>("plans", request);
    }

    public async Task<ApiResponse<PlannedExpense>> UpdatePlanAsync(string id, UpdatePlanRequest request)
    {
        return await _apiService.PutAsync<PlannedExpense>($"plans/{id}", request);
    }

    public async Task<ApiResponse<object>> DeletePlanAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"plans/{id}");
    }
}
