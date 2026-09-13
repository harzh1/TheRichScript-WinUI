using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IDebtService
{
    Task<ApiResponse<List<Debt>>> GetDebtsAsync();
    Task<ApiResponse<Debt>> CreateDebtAsync(CreateDebtRequest request);
    Task<ApiResponse<Debt>> UpdateDebtAsync(string id, UpdateDebtRequest request);
    Task<ApiResponse<Debt>> SettleDebtAsync(string id, SettleDebtRequest request);
    Task<ApiResponse<Debt>> SettleDebtAsync(string id, double amount, string assetId);
    Task<ApiResponse<object>> WriteOffDebtAsync(string id);
}

public class DebtService : IDebtService
{
    private readonly IApiService _apiService;

    public DebtService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<Debt>>> GetDebtsAsync()
    {
        return await _apiService.GetAsync<List<Debt>>("debts");
    }

    public async Task<ApiResponse<Debt>> CreateDebtAsync(CreateDebtRequest request)
    {
        return await _apiService.PostAsync<Debt>("debts", request);
    }

    public async Task<ApiResponse<Debt>> UpdateDebtAsync(string id, UpdateDebtRequest request)
    {
        return await _apiService.PutAsync<Debt>($"debts/{id}", request);
    }

    public async Task<ApiResponse<Debt>> SettleDebtAsync(string id, SettleDebtRequest request)
    {
        var resp = await _apiService.PutAsync<Debt>($"debts/{id}/settle", request);
        if (!resp.Success && resp.StatusCode == 404)
        {
            resp = await _apiService.PostAsync<Debt>($"debts/{id}/settle", request);
        }
        return resp;
    }

    public async Task<ApiResponse<Debt>> SettleDebtAsync(string id, double amount, string assetId)
    {
        return await SettleDebtAsync(id, new SettleDebtRequest { Amount = amount, AssetId = assetId });
    }

    public async Task<ApiResponse<object>> WriteOffDebtAsync(string id)
    {
        return await _apiService.PutAsync<object>($"debts/{id}/writeoff");
    }
}
