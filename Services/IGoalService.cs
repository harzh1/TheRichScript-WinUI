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
        if (request.ImageFilePaths != null && request.ImageFilePaths.Count > 0)
        {
            var stringFields = new Dictionary<string, string>
            {
                { "name", request.Name },
                { "targetAmount", request.TargetAmount.ToString(CultureInfo.InvariantCulture) },
                { "expectedDate", request.ExpectedDate.ToString("o") }
            };

            if (request.InitialDeposit > 0 && !string.IsNullOrEmpty(request.AssetId))
            {
                stringFields.Add("initialDeposit", request.InitialDeposit.ToString(CultureInfo.InvariantCulture));
                stringFields.Add("assetId", request.AssetId);
            }

            if (request.RecurringAmount.HasValue)
            {
                stringFields.Add("recurringAmount", request.RecurringAmount.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(request.RecurringFrequency))
            {
                stringFields.Add("recurringFrequency", request.RecurringFrequency.ToUpperInvariant());
            }

            if (!string.IsNullOrEmpty(request.SourceAssetId))
            {
                stringFields.Add("sourceAssetId", request.SourceAssetId);
            }

            if (!string.IsNullOrEmpty(request.TargetAssetId))
            {
                stringFields.Add("targetAssetId", request.TargetAssetId);
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
                assetId = request.AssetId,
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
            var stringFields = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(request.Name)) stringFields.Add("name", request.Name);
            if (request.TargetAmount.HasValue) stringFields.Add("targetAmount", request.TargetAmount.Value.ToString(CultureInfo.InvariantCulture));
            if (request.ExpectedDate.HasValue) stringFields.Add("expectedDate", request.ExpectedDate.Value.ToString("o"));
            if (request.RecurringAmount.HasValue) stringFields.Add("recurringAmount", request.RecurringAmount.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(request.RecurringFrequency)) stringFields.Add("recurringFrequency", request.RecurringFrequency.ToUpperInvariant());
            if (request.IsPinned.HasValue) stringFields.Add("isPinned", request.IsPinned.Value.ToString().ToLowerInvariant());
            if (!string.IsNullOrEmpty(request.SourceAssetId)) stringFields.Add("sourceAssetId", request.SourceAssetId);
            if (!string.IsNullOrEmpty(request.TargetAssetId)) stringFields.Add("targetAssetId", request.TargetAssetId);

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

    public async Task<ApiResponse<Goal>> AddFundsToGoalAsync(string id, double amount, string assetId)
    {
        var body = new AddGoalFundsRequest { Amount = amount, AssetId = assetId };
        return await _apiService.PutAsync<Goal>($"goals/{id}/funds", body);
    }

    public async Task<ApiResponse<object>> DeleteGoalAsync(string id, string? refundAssetId = null)
    {
        object body = !string.IsNullOrEmpty(refundAssetId) ? new DeleteGoalRequest { RefundAssetId = refundAssetId } : new { };
        return await _apiService.DeleteAsync<object>($"goals/{id}", body);
    }
}
