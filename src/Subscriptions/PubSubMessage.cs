namespace codecrafters_redis.Subscriptions;

internal record PubSubMessage(string Channel, string Message);