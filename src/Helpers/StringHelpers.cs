namespace codecrafters_redis.Helpers;

public static class StringHelpers
{
    public static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static void WriteLineEncoded(this string str)
    {
        WriteLine(str.Replace("\r\n", "\\r\\n"));
    }
}