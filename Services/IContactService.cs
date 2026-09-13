using System.Collections.Generic;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface IContactService
{
    Task<ApiResponse<List<Contact>>> GetContactsAsync();
    Task<ApiResponse<Contact>> CreateContactAsync(CreateContactRequest request);
    Task<ApiResponse<Contact>> UpdateContactAsync(string id, UpdateContactRequest request);
    Task<ApiResponse<object>> DeleteContactAsync(string id);
}

public class ContactService : IContactService
{
    private readonly IApiService _apiService;

    public ContactService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<Contact>>> GetContactsAsync()
    {
        return await _apiService.GetAsync<List<Contact>>("contacts");
    }

    public async Task<ApiResponse<Contact>> CreateContactAsync(CreateContactRequest request)
    {
        return await _apiService.PostAsync<Contact>("contacts", request);
    }

    public async Task<ApiResponse<Contact>> UpdateContactAsync(string id, UpdateContactRequest request)
    {
        return await _apiService.PutAsync<Contact>($"contacts/{id}", request);
    }

    public async Task<ApiResponse<object>> DeleteContactAsync(string id)
    {
        return await _apiService.DeleteAsync<object>($"contacts/{id}");
    }
}
