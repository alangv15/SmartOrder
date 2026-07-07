using SmartOrder.Entities.Orders.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Orders.Services
{
    public class PaymentStatusService
    {
        private readonly HttpClient _http;

        public PaymentStatusService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<PaymentStatusDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<PaymentStatusDto>>>("api/PaymentStatuses");
            return response?.Data ?? Enumerable.Empty<PaymentStatusDto>();
        }
    }
}
