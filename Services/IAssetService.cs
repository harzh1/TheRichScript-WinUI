using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IAssetService
{
    Task<ApiResponse<List<Asset>>> GetAssetsAsync();
    Task<ApiResponse<List<string>>> GetAssetTypesAsync();
    Task<ApiResponse<Asset>> CreateAssetAsync(CreateAssetRequest request);
    Task<ApiResponse<Asset>> UpdateAssetAsync(string id, UpdateAssetRequest request);
    Task<ApiResponse<object>> DeleteAssetAsync(string id);
    Task<ApiResponse<Transaction>> TransferAssetAsync(TransferAssetRequest request);
}

public class AssetService : IAssetService
{
    private readonly IApiService _apiService;

    public AssetService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<Asset>>> GetAssetsAsync()
    {
        return await _apiService.GetAsync<List<Asset>>("assets");
    }

    public async Task<ApiResponse<List<string>>> GetAssetTypesAsync()
    {
        return await _apiService.GetAsync<List<string>>("assets/types");
    }

    public async Task<ApiResponse<Asset>> CreateAssetAsync(CreateAssetRequest request)
    {
        return await _apiService.PostAsync<Asset>("assets", request);
    }

    public async Task<ApiResponse<Asset>> UpdateAssetAsync(string id, UpdateAssetRequest request)
    {
        return await _apiService.PutAsync<Asset>($"assets/{id}", request);
    }

    public async Task<ApiResponse<object>> DeleteAssetAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"assets/{id}");
    }

    public async Task<ApiResponse<Transaction>> TransferAssetAsync(TransferAssetRequest request)
    {
        return await _apiService.PostAsync<Transaction>("assets/transfer", request);
    }
}
