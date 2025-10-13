namespace codecrafters_redis.Commands;

public record Command(CommandType Type, string[] Arguments)
{
    public static readonly Command Unknown = new(CommandType.Unknown, []);
    
    public static Command Parse(string command)
    {
        var parts = command.Split("\r\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        string commandTypeStr = parts[2];
        if (!Enum.TryParse(commandTypeStr, true, out CommandType commandType)) return Unknown;
        
        var arguments = new List<string>();
        for (int i = 4; i < parts.Count; i += 2)
        {
            arguments.Add(parts[i].Trim());
        }
        
        return new(commandType, arguments.ToArray());
    }
}