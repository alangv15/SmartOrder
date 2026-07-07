using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Configuration.Services
{
    public class CategoryService
    {
        private readonly HttpClient _http;

        public CategoryService(HttpClient http)
        {
            _http = http;
        }

        // Obtener todas las categorías
        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<CategoryDto>>>("api/Category");
            return response?.Data ?? Enumerable.Empty<CategoryDto>();
        }

        // Obtener una categoría por ID
        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<CategoryDto>>($"api/Category/{id}");
            return response?.Data;
        }

        // Crear una nueva categoría
        public async Task<ApiResponse<int>?> CreateAsync(CategoryDto dto)
        {
            var result = await _http.PostAsJsonAsync("api/Category", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
        }

        // Actualizar una categoría existente
        public async Task<ApiResponse<object>?> UpdateAsync(CategoryDto dto)
        {
            var result = await _http.PutAsJsonAsync($"api/Category/{dto.CategoryId}", dto);
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }

        // Eliminar una categoría
        public async Task<ApiResponse<object>?> DeleteAsync(int id)
        {
            var result = await _http.DeleteAsync($"api/Category/{id}");
            return await result.Content.ReadFromJsonAsync<ApiResponse<object>>();
        }
    }
}

