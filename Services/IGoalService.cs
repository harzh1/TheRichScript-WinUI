using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IGoalService
{
    Task<ApiResponse<List<Goal>>> GetGoalsAsync();
    Task<ApiResponse<Goal>> CreateGoalAsync(CreateGoalRequest request);
    Task<ApiResponse<Goal>> UpdateGoalAsync(string id, UpdateGoalRequest request, List<string>? imageFilePaths = null);
    Task<ApiResponse<Goal>> AddFundsToGoalAsync(string id, AddGoalFundsRequest request);
    Task<ApiResponse<Goal>> AddFundsToGoalAsync(string id, double amount, string assetId);
    Task<ApiResponse<object>> DeleteGoalAsync(string id, string? refundAssetId = null);
}

public class GoalService : IGoalService
{
    private readonly IApiService _apiService;

    public GoalService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<Goal>>> GetGoalsAsync()
    {
        return await _apiService.GetAsync<List<Goal>>("goals");
    }

    public async Task<ApiResponse<Goal>> CreateGoalAsync(CreateGoalRequest request)
    {
        var effectiveAssetId = !string.IsNullOrEmpty(request.AssetId)
            ? request.AssetId
            : (!string.IsNullOrEmpty(request.TargetAssetId) ? request.TargetAssetId : request.SourceAssetId);

        if (request.ImageFilePaths.Count > 0)
        {
            var stringFields = new List<KeyValuePair<string, string>>
            {
                new("name", request.Name),
                new("targetAmount", request.TargetAmount.ToString(CultureInfo.InvariantCulture)),
                new("initialDeposit", request.InitialDeposit.ToString(CultureInfo.InvariantCulture)),
                new("expectedDate", request.ExpectedDate.ToString("o"))
            };

            if (!string.IsNullOrEmpty(request.Currency))
            {
                stringFields.Add(new("currency", request.Currency.ToUpperInvariant()));
            }

            if (!string.IsNullOrEmpty(effectiveAssetId))
            {
                stringFields.Add(new("assetId", effectiveAssetId));
            }

            if (request.RecurringAmount.HasValue)
            {
                stringFields.Add(new("recurringAmount", request.RecurringAmount.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (!string.IsNullOrEmpty(request.RecurringFrequency))
            {
                stringFields.Add(new("recurringFrequency", request.RecurringFrequency.ToUpperInvariant()));
            }

            if (!string.IsNullOrEmpty(request.SourceAssetId))
            {
                stringFields.Add(new("sourceAssetId", request.SourceAssetId));
            }

            if (!string.IsNullOrEmpty(request.TargetAssetId))
            {
                stringFields.Add(new("targetAssetId", request.TargetAssetId));
            }

            var files = new List<(string, string)>();
            foreach (var imgPath in request.ImageFilePaths)
            {
                files.Add(("images", imgPath));
            }

            return await _apiService.PostMultipartAsync<Goal>("goals", stringFields, files);
        }
        else
        {
            var body = new
            {
                name = request.Name,
                targetAmount = request.TargetAmount,
                initialDeposit = request.InitialDeposit,
                currency = request.Currency?.ToUpperInvariant(),
                assetId = effectiveAssetId,
                expectedDate = request.ExpectedDate,
                recurringAmount = request.RecurringAmount,
                recurringFrequency = request.RecurringFrequency,
                sourceAssetId = request.SourceAssetId,
                targetAssetId = request.TargetAssetId
            };

            return await _apiService.PostAsync<Goal>("goals", body);
        }
    }

    public async Task<ApiResponse<Goal>> UpdateGoalAsync(string id, UpdateGoalRequest request, List<string>? imageFilePaths = null)
    {
        if (imageFilePaths != null && imageFilePaths.Count > 0)
        {
            var stringFields = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(request.Name)) stringFields.Add(new("name", request.Name));
            if (request.TargetAmount.HasValue) stringFields.Add(new("targetAmount", request.TargetAmount.Value.ToString(CultureInfo.InvariantCulture)));
            if (!string.IsNullOrEmpty(request.Currency)) stringFields.Add(new("currency", request.Currency.ToUpperInvariant()));
            if (request.ExpectedDate.HasValue) stringFields.Add(new("expectedDate", request.ExpectedDate.Value.ToString("o")));
            if (request.RecurringAmount.HasValue) stringFields.Add(new("recurringAmount", request.RecurringAmount.Value.ToString(CultureInfo.InvariantCulture)));
            if (!string.IsNullOrEmpty(request.RecurringFrequency)) stringFields.Add(new("recurringFrequency", request.RecurringFrequency.ToUpperInvariant()));
            if (request.IsPinned.HasValue) stringFields.Add(new("isPinned", request.IsPinned.Value.ToString().ToLowerInvariant()));
            if (!string.IsNullOrEmpty(request.SourceAssetId)) stringFields.Add(new("sourceAssetId", request.SourceAssetId));
            if (!string.IsNullOrEmpty(request.TargetAssetId)) stringFields.Add(new("targetAssetId", request.TargetAssetId));

            if (request.ImageUrls != null)
            {
                foreach (var url in request.ImageUrls)
                {
                    stringFields.Add(new("imageUrls", url));
                }
            }

            var files = new List<(string, string)>();
            foreach (var imgPath in imageFilePaths)
            {
                files.Add(("images", imgPath));
            }

            return await _apiService.PutMultipartAsync<Goal>($"goals/{id}", stringFields, files);
        }
        else
        {
            return await _apiService.PutAsync<Goal>($"goals/{id}", request);
        }
    }

    public async Task<ApiResponse<Goal>> AddFundsToGoalAsync(string id, AddGoalFundsRequest request)
    {
        var resp = await _apiService.PostAsync<Goal>($"goals/{id}/fund", request);
        if (!resp.Success && resp.StatusCode == 404)
        {
            resp = await _apiService.PutAsync<Goal>($"goals/{id}/funds", request);
        }
        return resp;
    }

    public async Task<ApiResponse<Goal>> AddFundsToGoalAsync(string id, double amount, string assetId)
    {
        return await AddFundsToGoalAsync(id, new AddGoalFundsRequest { Amount = amount, AssetId = assetId });
    }

    public async Task<ApiResponse<object>> DeleteGoalAsync(string id, string? refundAssetId = null)
    {
        object body = !string.IsNullOrEmpty(refundAssetId) ? new DeleteGoalRequest { RefundAssetId = refundAssetId } : new { };
        return await _apiService.DeleteAsync<object>($"goals/{id}", body);
    }
}
