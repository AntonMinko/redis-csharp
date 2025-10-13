using System.Text;
using codecrafters_redis.Commands;


namespace codecrafters_redis.Helpers;

internal static class RedisTypesExtensions
{
    public const string OkString = "+OK\r\n";
    public static readonly RedisValue NullBulkString = new(BulkString, Encoding.UTF8.GetBytes("$-1\r\n"));
    public static readonly RedisValue NullBulkStringArray = new(BulkString, Encoding.UTF8.GetBytes("*-1\r\n"));
    public static readonly RedisValue EmptyBulkStringArray = new(BulkString, Encoding.UTF8.GetBytes("*0\r\n"));
    public static readonly RedisValue OkBytes = new(SimpleString, Encoding.UTF8.GetBytes(OkString));
    public static readonly RedisValue ReplicaConnectionResponse = new(ReplicaConnection, []);
    public static readonly RedisValue NoResponse = new(RedisTypes.Void, []);
    
    public static RedisValue ToSimpleString(this string s) => new(SimpleString, Encoding.UTF8.GetBytes($"+{s}\r\n"));

    public static RedisValue ToErrorString(this string message) => new(ErrorString, Encoding.UTF8.GetBytes($"-{message}\r\n"));

    public static RedisValue ToBulkString(this string? s)
    {
        var value = s == null ? Encoding.UTF8.GetBytes("$-1\r\n") : Encoding.UTF8.GetBytes(s.ToBulkStringContent());
        return new RedisValue(BulkString, value);
    }
    
    public static RedisValue ToBulkString(this TypedValue? typedValue)
    {
        string? stringValue = typedValue?.GetAsString();
        var value = stringValue == null ? Encoding.UTF8.GetBytes("$-1\r\n") : Encoding.UTF8.GetBytes(stringValue.ToBulkStringContent());
        return new RedisValue(BulkString, value);
    }

    public static RedisValue ToBulkStringArray(this IEnumerable<string> strings)
    {
        var sb = new StringBuilder();
        int count = 0;
        foreach (var s in strings)
        {
            sb.Append(s.ToBulkStringContent());
            count++;
        }

        sb.Insert(0, $"*{count}\r\n");
        return new RedisValue(BulkString, Encoding.UTF8.GetBytes(sb.ToString()));
    }
    
    public static RedisValue ToBulkStringArray(this Command command) => 
        new[] { command.Type.ToString() }.Concat(command.Arguments).ToBulkStringArray();

    public static RedisValue ToBinaryContent(this byte[] bytes)
    {
        var prefix = Encoding.UTF8.GetBytes($"${bytes.Length}\r\n");
        return new RedisValue(BinaryContent, prefix.Concat(bytes));
    }

    public static RedisValue ToIntegerString(this int value)
    {
        return new(Integer, Encoding.UTF8.GetBytes($":{value}\r\n"));
    }
    
    private static string ToBulkStringContent(this string s) => $"${s.Length}\r\n{s}\r\n";
}