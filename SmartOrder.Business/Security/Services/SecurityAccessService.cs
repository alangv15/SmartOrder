using SmartOrder.Entities.Security.Models;
using SmartOrder.Entities.Security.Requests;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Security.Services
{
    public class SecurityAccessService
    {
        private readonly HttpClient _httpClient;

        public SecurityAccessService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RoleAccessDto?> GetByRoleIdAsync(int roleId)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<RoleAccessDto>>($"api/security-access/{roleId}");
            return response?.Data;
        }

        public async Task<ApiResponse<object>?> UpdateAsync(int roleId, IEnumerable<int> permissionIds)
        {
            var payload = new RoleAccessUpdateDto
            {
                PermissionIds = permissionIds.Distinct().ToList()
            };

            var response = await _httpClient.PutAsJsonAsync($"api/security-access/{roleId}", payload);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}
