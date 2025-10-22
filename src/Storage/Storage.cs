namespace codecrafters_redis.Storage;

internal class StorageManager(
    KvpStorage kvpStorage,
    ListStorage listStorage)
{
    public KvpStorage KvpStorage { get; } = kvpStorage;
    public ListStorage ListStorage { get; } = listStorage;

    public IEnumerable<string> GetAllKeys()
    {
        return KvpStorage.Keys.Union(ListStorage.Keys).Order();
    }
    
    public ValueType GetType(string key)
    {
        if (KvpStorage.Get(key) != null) return ValueType.String;
        if (ListStorage.TryGetList(key, out _)) return ValueType.List;
        return ValueType.None;
    }
}