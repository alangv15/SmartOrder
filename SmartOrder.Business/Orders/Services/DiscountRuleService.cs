using SmartOrder.Entities.Orders.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Orders.Services
{
    public class DiscountRuleService
    {
        private readonly HttpClient _http;

        public DiscountRuleService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<DiscountRuleDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<DiscountRuleDto>>>("api/DiscountRule");
            return response?.Data ?? Enumerable.Empty<DiscountRuleDto>();
        }

        public async Task<DiscountRuleDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<DiscountRuleDto>>($"api/DiscountRule/{id}");
            return response?.Data;
        }

        public async Task<int?> CreateAsync(DiscountRuleDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/DiscountRule", dto);
            var response = await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
            return response?.Data;
        }

        public async Task<bool> UpdateAsync(DiscountRuleDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/DiscountRule/{dto.DiscountRuleId}", dto);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/DiscountRule/{id}");
            return result.IsSuccessStatusCode;
        }
    }
}
