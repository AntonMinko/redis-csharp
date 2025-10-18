using System.Collections.Concurrent;

namespace codecrafters_redis.Subscriptions;

internal class PubSub
{
    private class Subscription(int subscriberId)
    {
        public readonly int SubsсriberId = subscriberId;
        public ConcurrentQueue<string> Messages { get; } = new();
    }

    private class Subscriber(ClientConnection client)
    {
        public readonly int Id = client.Id;
        public readonly ClientConnection Client = client;
        public readonly LinkedList<Subscription> Subscriptions = new();

        public int SubscriptionsCount => Subscriptions.Count;
    }

    private readonly Dictionary<string, LinkedList<Subscription>> _subscriptions = new();
    private readonly Dictionary<int, Subscriber> _subscribers = new();
    
    public int Subscribe(EventType eventType, string eventKey, ClientConnection clientConnection)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions))
        {
            subscriptions = new LinkedList<Subscription>();
        }
        var subscriber = _subscribers.ContainsKey(clientConnection.Id) 
            ? _subscribers[clientConnection.Id] 
            : new Subscriber(clientConnection);
        _subscribers[clientConnection.Id] = subscriber;

        if (subscriptions.All(x => x.SubsсriberId != subscriber.Id))
        {
            var subscription = new Subscription(subscriber.Id);
            subscriptions.AddLast(subscription);
            subscriber.Subscriptions.AddLast(subscription);
            _subscriptions[topic] = subscriptions;
        }

        return subscriber.SubscriptionsCount;
    }

    public int Unsubscribe(EventType eventType, string eventKey, ClientConnection clientConnection)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscribers.TryGetValue(clientConnection.Id, out var subscriber)) return 0;
        if (!_subscriptions.TryGetValue(topic, out var subscriptions)) return subscriber.SubscriptionsCount;
        
        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriber.Id);
        if (subscription == null) return subscriber.SubscriptionsCount;
        
        subscriptions.Remove(subscription);
        subscriber.Subscriptions.Remove(subscription);
        return subscriber.SubscriptionsCount;
    }

    public int Publish(EventType eventType, string eventKey, string eventPayload)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions)) return 0;

        return eventType switch
        {
            EventType.ListPushed => PublishListPushedMessage(subscriptions, eventPayload),
            EventType.Subscription => PublishSubscriptionMessage(eventKey, subscriptions, eventPayload),
            _ => throw new Exception($"Unknown event type {eventType}")
        };
    }
    
    public bool TryGetListPushedValue(string eventKey, ClientConnection clientConnection, out string? value)
    {
        value = null;
        var topic = GetTopicName(EventType.ListPushed, eventKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions)) return false;
        if (!_subscribers.TryGetValue(clientConnection.Id, out var subscriber)) return false;
        
        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriber.Id);
        if (subscription == null) return false;
        
        return subscription.Messages.TryDequeue(out value);
    }

    private int PublishSubscriptionMessage(string eventKey, LinkedList<Subscription> subscriptions, string eventPayload)
    {
        var message = new PubSubMessage(eventKey, eventPayload);
        foreach (var subscription in subscriptions)
        {
            _subscribers[subscription.SubsсriberId].Client.MessagesQueue.Add(message);
        }
        return subscriptions.Count;
    }

    private int PublishListPushedMessage(LinkedList<Subscription> subscriptions, string eventPayload)
    {
        var subscription = subscriptions.FirstOrDefault(s => s.Messages.Count == 0);
        if (subscription == null) return 0;

        subscription.Messages.Enqueue(eventPayload);
        return 1;
    }

    public bool IsEventFired(EventType eventType, string eventKey, int subscriberId)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions))
        {
            return false;
        }

        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriberId);
        return subscription?.Messages.Any() ?? false;
    }

    private string GetTopicName(EventType eventType, string eventKey) => $"{eventType}:{eventKey}";
}