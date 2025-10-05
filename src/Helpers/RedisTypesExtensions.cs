using System.Text;
    

namespace codecrafters_redis.Helpers;

public static class RedisTypesExtensions
{
    public const string OkString = "+OK\r\n";
    public static readonly RedisValue NullBulkString = new(BulkString, Encoding.UTF8.GetBytes("$-1\r\n"));
    public static readonly RedisValue NullBulkStringArray = new(BulkString, Encoding.UTF8.GetBytes("*-1\r\n"));
    public static readonly RedisValue OkBytes = new(SimpleString, Encoding.UTF8.GetBytes(OkString));
    private static readonly byte[] EmptyBulkStringArray = Encoding.UTF8.GetBytes("*0\r\n");
    
    public static RedisValue ToSimpleString(this string s) => new(SimpleString, Encoding.UTF8.GetBytes($"+{s}\r\n"));

    public static RedisValue ToErrorString(this string message) => new(ErrorString, Encoding.UTF8.GetBytes($"-{message}\r\n"));

    public static RedisValue ToBulkString(this string? s)
    {
        var value = s == null ? Encoding.UTF8.GetBytes("$-1\r\n") : Encoding.UTF8.GetBytes(s.ToBulkStringContent());
        return new RedisValue(BulkString, value);
    }

    public static RedisValue ToBulkStringArray(this string[] strings)
    {
        if (strings.Length == 0) return new RedisValue(BulkString, EmptyBulkStringArray);
        
        var sb = new StringBuilder();
        sb.Append('*');
        sb.Append(strings.Length);
        sb.Append("\r\n");
        foreach (var s in strings)
        {
            sb.Append(s.ToBulkStringContent());
        }
        return new RedisValue(BulkString, Encoding.UTF8.GetBytes(sb.ToString()));
    }

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