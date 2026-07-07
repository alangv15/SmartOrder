using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Configuration.Services
{
    public class ProductService
    {
        private readonly HttpClient _http;

        public ProductService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<ProductDto>>>("api/Products");
            return response?.Data ?? Enumerable.Empty<ProductDto>();
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<ProductDto>>($"api/Products/{id}");
            return response?.Data;
        }

        public async Task<ApiResponse<int>?> CreateAsync(ProductDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/Products", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
        }

        public async Task<ApiResponse<object>?> UpdateAsync(ProductDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/Products/{dto.ProductId}", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        public async Task<ApiResponse<object>?> DeactivateAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/Products/{id}");
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}
