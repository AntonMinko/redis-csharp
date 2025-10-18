using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2)]
[SupportedInSubscribedMode(IsSupported = true)]
internal class Publish(PubSub pubSub, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Publish;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var channel = command.Arguments[0];
        var message = command.Arguments[1];
        
        int deliveries = pubSub.Publish(EventType.Subscription, channel, message);

        return deliveries.ToIntegerValue();
    }
}