namespace codecrafters_redis.Storage;

enum ValueType
{
    None,
    String,
    List
}

internal record struct TypedValue(ValueType Type, object Value)
{
    public string GetAsString() => (string)Value;
    public LinkedList<string> GetAsStringList() => (LinkedList<string>)Value;
}

internal record struct StorageValue(TypedValue Value, DateTime? ExpireAt = null);
