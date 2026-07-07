using SmartOrder.Entities.Orders.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net;
using System.Net.Http.Json;

namespace SmartOrder.Business.Orders.Services
{
    public class DiscountLimitRuleService
    {
        private readonly HttpClient _http;

        public DiscountLimitRuleService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<DiscountLimitRuleDto>> GetAllAsync()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<DiscountLimitRuleDto>>>("api/DiscountLimitRule");
                return response?.Data ?? Enumerable.Empty<DiscountLimitRuleDto>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Enumerable.Empty<DiscountLimitRuleDto>();
            }
        }

        public async Task<DiscountLimitRuleDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<DiscountLimitRuleDto>>($"api/DiscountLimitRule/{id}");
            return response?.Data;
        }

        public async Task<int?> CreateAsync(DiscountLimitRuleDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/DiscountLimitRule", dto);
            var response = await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
            return response?.Data;
        }

        public async Task<bool> UpdateAsync(DiscountLimitRuleDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/DiscountLimitRule/{dto.DiscountLimitRuleId}", dto);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/DiscountLimitRule/{id}");
            return result.IsSuccessStatusCode;
        }
    }
}
