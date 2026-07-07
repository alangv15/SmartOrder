using SmartOrder.Entities.Reports.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Reports.Services
{
    public class SalesSummaryReportService
    {
        private readonly HttpClient _http;

        public SalesSummaryReportService(HttpClient http)
        {
            _http = http;
        }

        public async Task<SalesSummaryReportDto> GetSalesSummaryReportAsync(DateTime startDate, DateTime endDate)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<SalesSummaryReportDto>>(
                $"api/reports/sales-summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            return response?.Data ?? new SalesSummaryReportDto
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };
        }
    }
}
