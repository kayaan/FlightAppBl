namespace FlightApp.Services;

public interface IKeyValueStorage
{
    Task SetAsync<T>(string key, T value);
    Task<T?> GetAsync<T>(string key);
    Task<T> GetOrDefaultAsync<T>(string key, T defaultValue);
    Task RemoveAsync(string key);
    Task<bool> ContainsAsync(string key);
}

