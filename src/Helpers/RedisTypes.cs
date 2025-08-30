using System.Text;

namespace codecrafters_redis.Helpers;

public static class RedisTypes
{
    public static readonly byte[] OkString = Encoding.UTF8.GetBytes("+OK\r\n");
    public static readonly byte[] NullBulkString = Encoding.UTF8.GetBytes("$-1\r\n");
    public static readonly byte[] NullBulkStringArray = Encoding.UTF8.GetBytes("*-1\r\n");
    public static readonly byte[] EmptyBulkStringArray = Encoding.UTF8.GetBytes("*0\r\n");
    
    public static byte[] ToSimpleString(this string s)
    {
        return Encoding.UTF8.GetBytes($"+{s}\r\n");
    }

    public static byte[] ToErrorString(this string message)
    {
        return Encoding.UTF8.GetBytes($"-{message}\r\n");
    }

    public static byte[] ToBulkString(this string? s) => s == null ? NullBulkString : Encoding.UTF8.GetBytes(s.ToBulkStringContent());

    public static byte[] ToBulkStringArray(this string[] strings)
    {
        if (strings.Length == 0) return EmptyBulkStringArray;
        
        var sb = new StringBuilder();
        sb.Append('*');
        sb.Append(strings.Length);
        sb.Append("\r\n");
        foreach (var s in strings)
        {
            sb.Append(s.ToBulkStringContent());
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
    
    private static string ToBulkStringContent(this string s) => $"${s.Length}\r\n{s}\r\n";
}