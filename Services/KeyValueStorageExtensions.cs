namespace FlightApp.Services;

public static class KeyValueStorageExtensions
{
    public static Task SetAsync<T>(
        this IKeyValueStorage storage,
        StorageKey<T> key,
        T value)
    {
        return storage.SetAsync(key.Key, value);
    }

    public static Task<T?> GetAsync<T>(
        this IKeyValueStorage storage,
        StorageKey<T> key)
    {
        return storage.GetAsync<T>(key.Key);
    }

    public static Task<T> GetOrDefaultAsync<T>(
        this IKeyValueStorage storage,
        StorageKey<T> key,
        T defaultValue)
    {
        return storage.GetOrDefaultAsync(key.Key, defaultValue);
    }

    public static Task RemoveAsync<T>(
        this IKeyValueStorage storage,
        StorageKey<T> key)
    {
        return storage.RemoveAsync(key.Key);
    }

    public static Task<bool> ContainsAsync<T>(
        this IKeyValueStorage storage,
        StorageKey<T> key)
    {
        return storage.ContainsAsync(key.Key);
    }
}