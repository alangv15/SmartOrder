using SmartOrder.Entities.Security.Requests;
using SmartOrder.Entities.Security.Responses;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartOrder.Business.Security.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<UserResponseDto>?> LoginAsync(string email, string password)
        {
            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/users/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse<UserResponseDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
