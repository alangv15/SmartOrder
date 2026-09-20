using SmartOrder.Entities.Reports.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Reports.Services
{
    public class MonthlyProfitReportService
    {
        private readonly HttpClient _http;

        public MonthlyProfitReportService(HttpClient http)
        {
            _http = http;
        }

        public async Task<MonthlyProfitReportDto> GetMonthlyProfitReportAsync(int months = 6)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<MonthlyProfitReportDto>>(
                $"api/reports/monthly-profit?months={months}");

            return response?.Data ?? new MonthlyProfitReportDto();
        }
    }
}
