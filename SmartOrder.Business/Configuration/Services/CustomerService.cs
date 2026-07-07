using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Configuration.Services
{
    public class CustomerService
    {
        private readonly HttpClient _http;

        public CustomerService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<CustomerDto>>>("api/Customers");
            return response?.Data ?? Enumerable.Empty<CustomerDto>();
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<CustomerDto>>($"api/Customers/{id}");
            return response?.Data;
        }

        public async Task<ApiResponse<int>?> CreateAsync(CustomerDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/Customers", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
        }

        public async Task<ApiResponse<object>?> UpdateAsync(CustomerDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/Customers/{dto.CustomerId}", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> DeactivateAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/Customers/{id}");
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}
