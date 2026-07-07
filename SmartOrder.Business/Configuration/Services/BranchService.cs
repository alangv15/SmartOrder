using System.Net.Http.Json;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Shared.Responses;

namespace SmartOrder.Business.Configuration.Services;

public class BranchService
{
    private readonly HttpClient _http;

    public BranchService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IEnumerable<BranchDto>> GetAllAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<BranchDto>>>("api/Branches");
        return response?.Data ?? Enumerable.Empty<BranchDto>();
    }

    public async Task<BranchDto?> GetByIdAsync(int id)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<BranchDto>>($"api/Branches/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<object>?> CreateAsync(BranchDto dto)
    {
        var result = await _http.PostAsJsonAsync("api/Branches", dto);
        return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }

    public async Task<ApiResponse<object>?> UpdateAsync(BranchDto dto)
    {
        var result = await _http.PutAsJsonAsync($"api/Branches/{dto.BranchId}", dto);
        return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }

    public async Task<ApiResponse<object>?> DeactivateAsync(int id)
    {
        var result = await _http.DeleteAsync($"api/Branches/{id}");
        return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }
}
