using SmartOrder.Entities.Reports.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Reports.Services
{
    public class DailySalesReportService
    {
        private readonly HttpClient _http;

        public DailySalesReportService(HttpClient http)
        {
            _http = http;
        }

        public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<DailySalesReportDto>>($"api/reports/daily-sales?date={date:yyyy-MM-dd}");
            return response?.Data ?? new DailySalesReportDto
            {
                Date = date.Date
            };
        }
    }
}
