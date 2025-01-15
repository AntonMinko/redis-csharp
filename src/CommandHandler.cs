using System.Text;
using codecrafters_redis.UserSettings;

namespace codecrafters_redis;

public class CommandHandler(IStorage storage, IUserSettingsProvider userSettingsProvider)
{
    public byte[] Handle(List<string> command)
    {
        switch (command[0].ToUpperInvariant())
        {
            case "PING":
                return SimpleString("PONG");
            case "ECHO":
                return SimpleString(command[1]);
            case "GET":
                return HandleGet(command);
            case "SET":
                return HandleSet(command);
            case "CONFIG":
                return HandleConfig(command);
            default:
                Console.WriteLine("Unknown command: " + String.Join(" ", command));
                return ErrorString($"Unknown command {command[0]}");
        }
    }

    private byte[] HandleConfig(List<string> command)
    {
        if (command.Count == 1 || command.Count > 3)
        {
            return ErrorString("ERR wrong number of arguments for 'config' command");
        }

        if (command[1].ToUpperInvariant() != "GET")
        {
            return ErrorString($"ERR unknown subcommand '{command[1]}'");
        }
        
        if (command.Count == 2)
        {
            return ErrorString("ERR wrong number of arguments for 'config|get' command");
        }

        var settings = userSettingsProvider.GetUserSettings();
        
        switch (command[2].ToUpperInvariant())
        {
            case "DIR":
                return BulkStringArray(["dir", settings.Persistence.Dir]);
            case "DBFILENAME":
                return BulkStringArray(["dbfilename", settings.Persistence.DbFileName]);
            default:
                return BulkStringArray(null);
        }
    }

    private byte[] HandleSet(List<string> command)
    {
        if (command.Count < 3) return ErrorString("ERR wrong number of arguments for 'set' command");
        
        var key = command[1];
        var value = command[2];
        int? expiresAfterMs = null;
        if (command.Count == 5 && command[3].ToUpperInvariant() == "PX")
        {
            if (int.TryParse(command[4], out int ms))
            {
                expiresAfterMs = ms;
            }
        }
        
        storage.Set(key, value, expiresAfterMs);
        return OkString();
    }

    private byte[] HandleGet(List<string> command)
    {
        if (command.Count < 2) return BulkString(null);
        
        var key = command[1];
        return BulkString(storage.Get(key));
    }

    private byte[] SimpleString(string s)
    {
        return Encoding.UTF8.GetBytes($"+{s}\r\n");
    }

    private byte[] ErrorString(string message)
    {
        return Encoding.UTF8.GetBytes($"-{message}\r\n");
    }

    private byte[] BulkString(string? s) => Encoding.UTF8.GetBytes(s == null ? "$-1\r\n" : BulkStringContent(s));

    private static string BulkStringContent(string s) => $"${s.Length}\r\n{s}\r\n";

    private byte[] BulkStringArray(string[]? strings)
    {
        if (strings == null) return Encoding.UTF8.GetBytes("*-1\r\n");
        if (strings.Length == 0) return Encoding.UTF8.GetBytes("*0\r\n");
        
        var sb = new StringBuilder();
        sb.Append('*');
        sb.Append(strings.Length);
        sb.Append("\r\n");
        foreach (var s in strings)
        {
            sb.Append(BulkStringContent(s));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] OkString()
    {
        return Encoding.UTF8.GetBytes("+OK\r\n");
    }
}