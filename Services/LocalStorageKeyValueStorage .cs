using System.Text.Json;
using Microsoft.JSInterop;

namespace FlightApp.Services;

public class LocalStorageKeyValueStorage : IKeyValueStorage
{
    private readonly IJSRuntime _js;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LocalStorageKeyValueStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);

        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T> GetOrDefaultAsync<T>(string key, T defaultValue)
    {
        var value = await GetAsync<T>(key);
        return value is null ? defaultValue : value;
    }

    public async Task RemoveAsync(string key)
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task<bool> ContainsAsync(string key)
    {
        var value = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        return value is not null;
    }
}