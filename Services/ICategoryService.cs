using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface ICategoryService
{
    Task<ApiResponse<List<Category>>> GetCategoriesAsync();
    Task<ApiResponse<Category>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ApiResponse<Category>> UpdateCategoryAsync(string id, UpdateCategoryRequest request);
    Task<ApiResponse<object>> DeleteCategoryAsync(string id);
}

public class CategoryService : ICategoryService
{
    private readonly IApiService _apiService;

    public CategoryService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<Category>>> GetCategoriesAsync()
    {
        return await _apiService.GetAsync<List<Category>>("categories");
    }

    public async Task<ApiResponse<Category>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        return await _apiService.PostAsync<Category>("categories", request);
    }

    public async Task<ApiResponse<Category>> UpdateCategoryAsync(string id, UpdateCategoryRequest request)
    {
        return await _apiService.PutAsync<Category>($"categories/{id}", request);
    }

    public async Task<ApiResponse<object>> DeleteCategoryAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"categories/{id}");
    }
}
