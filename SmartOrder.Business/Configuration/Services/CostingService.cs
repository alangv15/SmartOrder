using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Shared.Responses;
using System.Net.Http.Json;

namespace SmartOrder.Business.Configuration.Services;

public sealed class CostingService
{
    private readonly HttpClient _http;

    public CostingService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IEnumerable<CostItemDto>> GetCostItemsAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<CostItemDto>>>("api/Costing/cost-items");
        if (response?.Success != true || response.Data == null)
            throw new InvalidOperationException("No se pudieron consultar los insumos y servicios.");
        return response.Data;
    }

    public async Task<CostItemDto?> GetCostItemAsync(int id)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<CostItemDto>>($"api/Costing/cost-items/{id}");
        if (response?.Success != true || response.Data == null)
            throw new InvalidOperationException("No se pudo consultar el concepto de costo.");
        return response.Data;
    }

    public async Task<ApiResponse<int>?> SaveCostItemAsync(SaveCostItemRequestDto request)
    {
        return await SaveAsync("api/Costing/cost-items", request);
    }

    public async Task<IEnumerable<ProductRecipeDto>> GetRecipesAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<ProductRecipeDto>>>("api/Costing/recipes");
        return response?.Data ?? Enumerable.Empty<ProductRecipeDto>();
    }

    public async Task<ProductRecipeDto?> GetCurrentRecipeAsync(int productId)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<ProductRecipeDto>>($"api/Costing/recipes/current/{productId}");
        return response?.Data;
    }

    public async Task<ApiResponse<int>?> SaveRecipeAsync(SaveProductRecipeRequestDto request)
    {
        return await SaveAsync("api/Costing/recipes", request);
    }

    public async Task<IEnumerable<BaseRecipeDto>> GetBaseRecipesAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<BaseRecipeDto>>>("api/Costing/base-recipes");
        if (response?.Success != true || response.Data == null)
            throw new InvalidOperationException("No se pudieron consultar las recetas base.");
        return response.Data;
    }

    public Task<ApiResponse<int>?> SaveBaseRecipeAsync(SaveBaseRecipeRequestDto request) =>
        SaveAsync("api/Costing/base-recipes", request);

    private async Task<ApiResponse<int>?> SaveAsync<T>(string path, T request)
    {
        using var result = await _http.PostAsJsonAsync(path, request);
        var response = await result.Content.ReadFromJsonAsync<ApiResponse<int>>();
        if (!result.IsSuccessStatusCode || response?.Success != true || response.Data <= 0)
            throw new InvalidOperationException(response?.Errors is { Count: > 0 }
                ? string.Join(Environment.NewLine, response.Errors) : "No se pudo guardar la informacion de costos.");
        return response;
    }

    public async Task<IEnumerable<ProductCostingIssueDto>> GetProductIssuesAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<ProductCostingIssueDto>>>("api/Costing/products/issues");
        return response?.Data ?? Enumerable.Empty<ProductCostingIssueDto>();
    }
}
