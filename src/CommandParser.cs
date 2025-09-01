namespace codecrafters_redis;

public static class CommandParser
{
    public static string[] Parse(this string command)
    {
        var result = new List<string>();
        var parts = command.Split("\r\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        for (int i = 2; i < parts.Count; i += 2)
        {
            result.Add(parts[i]);
        }
        
        return result.ToArray();
    }
}