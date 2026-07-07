using SmartOrder.Entities.Security.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Security.Services
{
    public class RoleManagementService
    {
        private readonly HttpClient _httpClient;

        public RoleManagementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<RoleDto>>>("api/roles");
            return response?.Data ?? Enumerable.Empty<RoleDto>();
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<RoleDto>>($"api/roles/{id}");
            return response?.Data;
        }

        public async Task<ApiResponse<object>?> CreateAsync(RoleDto role)
        {
            var response = await _httpClient.PostAsJsonAsync("api/roles", role);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> UpdateAsync(RoleDto role)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/roles/{role.RoleId}", role);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> DeactivateAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/roles/{id}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}
