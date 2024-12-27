using System.Reflection.Metadata;
using System.Text;

namespace codecrafters_redis;

public class CommandHandler
{
    public byte[] Handle(List<string> command)
    {
        switch (command[0].ToUpperInvariant())
        {
            case "PING":
                return SimpleString("PONG");
            case "ECHO":
                return SimpleString(command[1]);
                break;
            default:
                Console.WriteLine("Unknown command: " + String.Join(" ", command));
                return ErrorString($"Unknown command {command[0]}");
        }
    }

    private byte[] SimpleString(string s)
    {
        return Encoding.UTF8.GetBytes($"+{s}\r\n");
    }

    private byte[] ErrorString(string message)
    {
        return Encoding.UTF8.GetBytes($"-{message}\r\n");
    }

}