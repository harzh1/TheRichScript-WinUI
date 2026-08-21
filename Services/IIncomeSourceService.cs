using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IIncomeSourceService
{
    Task<ApiResponse<List<IncomeSource>>> GetIncomeSourcesAsync();
    Task<ApiResponse<IncomeSourcesSummary>> GetIncomeSourcesSummaryAsync();
    Task<ApiResponse<IncomeSource>> CreateIncomeSourceAsync(CreateIncomeSourceRequest request);
    Task<ApiResponse<IncomeSource>> UpdateIncomeSourceAsync(string id, UpdateIncomeSourceRequest request);
    Task<ApiResponse<object>> DeleteIncomeSourceAsync(string id);
}

public class IncomeSourceService : IIncomeSourceService
{
    private readonly IApiService _apiService;

    public IncomeSourceService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<IncomeSource>>> GetIncomeSourcesAsync()
    {
        return await _apiService.GetAsync<List<IncomeSource>>("income-sources");
    }

    public async Task<ApiResponse<IncomeSourcesSummary>> GetIncomeSourcesSummaryAsync()
    {
        return await _apiService.GetAsync<IncomeSourcesSummary>("income-sources/summary");
    }

    public async Task<ApiResponse<IncomeSource>> CreateIncomeSourceAsync(CreateIncomeSourceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IconFilePath))
        {
            var stringFields = new Dictionary<string, string>
            {
                { "name", request.Name },
                { "type", request.Type },
                { "isFixed", request.IsFixed.ToString().ToLowerInvariant() }
            };

            if (request.ExpectedAmount.HasValue)
            {
                stringFields["expectedAmount"] = request.ExpectedAmount.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(request.Frequency))
            {
                stringFields["frequency"] = request.Frequency;
            }

            if (!string.IsNullOrWhiteSpace(request.DefaultAssetId))
            {
                stringFields["defaultAssetId"] = request.DefaultAssetId;
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                stringFields["description"] = request.Description;
            }

            var files = new List<(string, string)>
            {
                ("icon", request.IconFilePath)
            };

            return await _apiService.PostMultipartAsync<IncomeSource>("income-sources", stringFields, files);
        }
        else
        {
            var body = new
            {
                name = request.Name,
                type = request.Type,
                isFixed = request.IsFixed,
                expectedAmount = request.ExpectedAmount,
                frequency = request.Frequency,
                defaultAssetId = request.DefaultAssetId,
                description = request.Description
            };

            return await _apiService.PostAsync<IncomeSource>("income-sources", body);
        }
    }

    public async Task<ApiResponse<IncomeSource>> UpdateIncomeSourceAsync(string id, UpdateIncomeSourceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IconFilePath))
        {
            var stringFields = new Dictionary<string, string>();
            if (request.Name != null) stringFields["name"] = request.Name;
            if (request.Type != null) stringFields["type"] = request.Type;
            if (request.IsFixed.HasValue) stringFields["isFixed"] = request.IsFixed.Value.ToString().ToLowerInvariant();
            if (request.ExpectedAmount.HasValue) stringFields["expectedAmount"] = request.ExpectedAmount.Value.ToString(CultureInfo.InvariantCulture);
            if (request.Frequency != null) stringFields["frequency"] = request.Frequency;
            if (request.DefaultAssetId != null) stringFields["defaultAssetId"] = request.DefaultAssetId;
            if (request.Description != null) stringFields["description"] = request.Description;
            if (request.IsActive.HasValue) stringFields["isActive"] = request.IsActive.Value.ToString().ToLowerInvariant();

            var files = new List<(string, string)>
            {
                ("icon", request.IconFilePath)
            };

            return await _apiService.PutMultipartAsync<IncomeSource>($"income-sources/{id}", stringFields, files);
        }
        else
        {
            var body = new
            {
                name = request.Name,
                type = request.Type,
                isFixed = request.IsFixed,
                expectedAmount = request.ExpectedAmount,
                frequency = request.Frequency,
                defaultAssetId = request.DefaultAssetId,
                description = request.Description,
                isActive = request.IsActive
            };

            return await _apiService.PutAsync<IncomeSource>($"income-sources/{id}", body);
        }
    }

    public async Task<ApiResponse<object>> DeleteIncomeSourceAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"income-sources/{id}");
    }
}
