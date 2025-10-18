using System.Collections.Concurrent;

namespace codecrafters_redis.Subscriptions;

internal class PubSub
{
    private class Subscription(EventType type, int subscriberId)
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
            var subscription = new Subscription(eventType, subscriber.Id);
            subscriptions.AddLast(subscription);
            subscriber.Subscriptions.AddLast(subscription);
            _subscriptions[topic] = subscriptions;
        }

        return subscriber.SubscriptionsCount;
    }

    public IList<string> Unsubscribe(EventType eventType, string eventKey, int subscriberId)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions)) return new List<string>();

        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriberId);
        if (subscription == null) return new List<string>();
        
        subscriptions.Remove(subscription);
        _subscribers[subscriberId].Subscriptions.Remove(subscription);
        return subscription.Messages.ToList();
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