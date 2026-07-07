using SmartOrder.Entities.Security.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Security.Services
{
    public class UserManagementService
    {
        private readonly HttpClient _httpClient;

        public UserManagementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<UserDto>>>("api/users");
            return response?.Data ?? Enumerable.Empty<UserDto>();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserDto>>($"api/users/{id}");
            return response?.Data;
        }

        public async Task<ApiResponse<object>?> CreateAsync(UserDto user)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users", user);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> UpdateAsync(UserDto user)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{user.UserId}", user);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> DeactivateAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/users/{id}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}
