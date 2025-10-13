using System.Collections.Frozen;

namespace codecrafters_redis.Commands;

internal class Processor(IEnumerable<ICommandHandler> commandHandlers, MasterManager masterManager)
{
    private readonly IDictionary<CommandType, ICommandHandler> _commandHandlers =
        commandHandlers.ToFrozenDictionary(x => x.CommandType);
    
    public async Task<RedisValue> Handle(string commandPayload, ClientConnection connection)
    {
        var command = Command.Parse(commandPayload);

        if (_commandHandlers.TryGetValue(command.Type, out var handler))
        {
            var response = await handler.Handle(command, connection);
            
            if (response.Success && handler.SupportsReplication)
            {
                connection.LastCommandOffset = masterManager.PropagateCommand(command);
                $"Last command offset: {connection.LastCommandOffset}".WriteLineEncoded();
            }
            return response;
        }

        $"Unknown command: {commandPayload}".WriteLineEncoded();
        return "Unknown command".ToErrorString();
    }
}