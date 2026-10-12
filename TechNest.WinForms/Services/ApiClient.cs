using System.Net.Http.Json;
using System.Text.Json;
using TechNest.WinForms.Models;

namespace TechNest.WinForms.Services;

public class ApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:5261/api/") };
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<Product>> GetProductsAsync(string? search = null)
    {
        var url = "products";
        if (!string.IsNullOrWhiteSpace(search)) url += "?search=" + Uri.EscapeDataString(search);
        return await _http.GetFromJsonAsync<List<Product>>(url, _json) ?? new();
    }
    public async Task<Product> CreateProductAsync(Product product)
    {
        using var response = await _http.PostAsJsonAsync("products", product);
        return await ReadResult<Product>(response);
    }
    public async Task UpdateProductAsync(Product product)
    {
        using var response = await _http.PutAsJsonAsync("products/" + product.ProductId, product);
        await EnsureSuccess(response);
    }
    public async Task DeleteProductAsync(int id)
    {
        using var response = await _http.DeleteAsync("products/" + id);
        await EnsureSuccess(response);
    }
    public async Task AdjustStockAsync(int id, int change)
    {
        using var response = await _http.PostAsJsonAsync($"products/{id}/stock-adjustment", new { change });
        await EnsureSuccess(response);
    }
    public async Task RecordSaleAsync(int productId, int quantity)
    {
        using var response = await _http.PostAsJsonAsync("sales", new { items = new[] { new { productId, quantity } } });
        await EnsureSuccess(response);
    }
    public async Task<List<SaleRecord>> GetSalesAsync() =>
        await _http.GetFromJsonAsync<List<SaleRecord>>("sales", _json) ?? new();
    public async Task<ReportSummary> GetSummaryAsync() =>
        await _http.GetFromJsonAsync<ReportSummary>("reports/summary", _json) ?? new();

    private static async Task<T> ReadResult<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ErrorMessage(response));
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("The API returned an empty response.");
    }
    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ErrorMessage(response));
    }
    private static async Task<string> ErrorMessage(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var message)) return message.GetString() ?? response.ReasonPhrase ?? "Request failed.";
            if (json.RootElement.TryGetProperty("title", out var title)) return title.GetString() ?? response.ReasonPhrase ?? "Request failed.";
        }
        catch { }
        return string.IsNullOrWhiteSpace(body) ? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}" : body;
    }
}
