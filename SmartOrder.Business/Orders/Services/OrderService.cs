using SmartOrder.Entities.Orders.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Globalization;
using System.Net.Http.Json;

namespace SmartOrder.Business.Orders.Services
{
    public class OrderService
    {
        private readonly HttpClient _http;

        public OrderService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<OrderDto>>>("api/Orders");
            return response?.Data ?? Enumerable.Empty<OrderDto>();
        }

        public async Task<IEnumerable<OrderDto>> GetActiveCustomOrdersAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<OrderDto>>>("api/Orders/custom/active");
            return response?.Data ?? Enumerable.Empty<OrderDto>();
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetCustomOrderSummariesAsync(DateTime startUtc, DateTime endExclusiveUtc)
        {
            var start = Uri.EscapeDataString(startUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            var end = Uri.EscapeDataString(endExclusiveUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<OrderSummaryDto>>>(
                $"api/Orders/custom/summary?startUtc={start}&endExclusiveUtc={end}");
            return response?.Data ?? Enumerable.Empty<OrderSummaryDto>();
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<OrderDto>>($"api/Orders/{id}");
            return response?.Data;
        }

        public async Task<OrderDto?> GetInStoreSaleByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<OrderDto>>($"api/Orders/sales/{id}");
            return response?.Data;
        }

        public async Task<OrderDto?> GetCustomOrderByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<OrderDto>>($"api/Orders/custom/{id}");
            return response?.Data;
        }

        public async Task<int?> CreateAsync(OrderDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/Orders", dto);
            var response = await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
            return response?.Data;
        }

        public async Task<bool> UpdateAsync(OrderDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/Orders/{dto.OrderId}", dto);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateStatusAsync(int orderId, OrderStatusUpdateDto dto)
        {
            var result = await _http.PatchAsJsonAsync($"api/Orders/custom/{orderId}/status", dto);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/Orders/{id}");
            return result.IsSuccessStatusCode;
        }
    }
}
