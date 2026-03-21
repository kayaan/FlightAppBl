namespace FlightApp.Services;

public sealed class StorageKey<T>
{
    public string Key { get; }

    public StorageKey(string key)
    {
        Key = key;
    }

    public override string ToString() => Key;
}