using SmartOrder.Entities.Orders.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Orders.Services
{
    public class OrderStatusService
    {
        private readonly HttpClient _http;

        public OrderStatusService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<OrderStatusDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<OrderStatusDto>>>("api/OrderStatus");
            return response?.Data ?? Enumerable.Empty<OrderStatusDto>();
        }
    }
}
