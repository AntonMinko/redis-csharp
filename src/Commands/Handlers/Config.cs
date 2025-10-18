using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
internal class Config(Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Config;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var subCommand = command.Arguments[0];

        if (subCommand != "GET")
        {
            return $"ERR unknown subcommand '{subCommand}'".ToErrorString();
        }
        
        if (command.Arguments.Length == 1)
        {
            return "ERR wrong number of arguments for 'config|get' command".ToErrorString();
        }
        
        string configName = command.Arguments[1].ToUpperInvariant();
        switch (configName)
        {
            case "DIR":
                return new[] {"dir", Settings.Persistence.Dir}.ToBulkStringArray();
            case "DBFILENAME":
                return new[] {"dbfilename", Settings.Persistence.DbFileName}.ToBulkStringArray();
            default:
                return NullBulkStringArray;
        }
    }
}