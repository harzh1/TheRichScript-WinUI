using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface ITransactionService
{
    Task<TransactionListResponse> GetTransactionsAsync(string? category = null, string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int limit = 30, string? search = null);
    Task<ApiResponse<Transaction>> CreateTransactionAsync(CreateTransactionRequest request);
    Task<ApiResponse<object>> DeleteTransactionAsync(string id);
}

public class TransactionService : ITransactionService
{
    private readonly IApiService _apiService;

    public TransactionService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<TransactionListResponse> GetTransactionsAsync(string? category = null, string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int limit = 30, string? search = null)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"limit={limit}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "ALL")
        {
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(type) && type != "ALL")
        {
            queryParams.Add($"type={Uri.EscapeDataString(type.ToUpperInvariant())}");
        }

        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        var endpoint = $"transactions?{string.Join("&", queryParams)}";
        var response = await _apiService.GetAsync<TransactionListResponse>(endpoint);

        if (response.Success && response.Data != null)
        {
            return response.Data;
        }

        return new TransactionListResponse
        {
            Success = false,
            Error = response.Error ?? "Failed to fetch transactions"
        };
    }

    public async Task<ApiResponse<Transaction>> CreateTransactionAsync(CreateTransactionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ReceiptFilePath))
        {
            var stringFields = new Dictionary<string, string>
            {
                { "assetId", request.AssetId },
                { "type", request.Type.ToUpperInvariant() },
                { "amount", request.Amount.ToString(CultureInfo.InvariantCulture) },
                { "category", request.Category.ToUpperInvariant() },
                { "description", request.Description },
                { "date", request.Date.ToString("o") }
            };

            if (!string.IsNullOrWhiteSpace(request.IncomeSourceId))
            {
                stringFields["incomeSourceId"] = request.IncomeSourceId;
            }
            else if (!string.IsNullOrWhiteSpace(request.IncomeSourceName))
            {
                stringFields["incomeSource"] = request.IncomeSourceName;
            }

            var files = new List<(string, string)>
            {
                ("receipt", request.ReceiptFilePath)
            };

            return await _apiService.PostMultipartAsync<Transaction>("transactions", stringFields, files);
        }
        else
        {
            var body = new
            {
                assetId = request.AssetId,
                type = request.Type.ToUpperInvariant(),
                amount = request.Amount,
                category = request.Category.ToUpperInvariant(),
                incomeSourceId = request.IncomeSourceId,
                incomeSource = request.IncomeSourceName,
                description = request.Description,
                date = request.Date
            };

            return await _apiService.PostAsync<Transaction>("transactions", body);
        }
    }

    public async Task<ApiResponse<object>> DeleteTransactionAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"transactions/{id}");
    }
}
