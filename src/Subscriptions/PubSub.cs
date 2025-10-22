using System.Collections.Concurrent;

namespace codecrafters_redis.Subscriptions;

internal class PubSub
{
    private record Subscription(int SubscriberId, ConcurrentQueue<PubSubMessage> MessagesQueue);

    private class Subscriber(int id)
    {
        public readonly int Id = id;
        public readonly LinkedList<Subscription> Subscriptions = new();

        public int SubscriptionsCount => Subscriptions.Count;
    }

    private readonly Dictionary<string, LinkedList<Subscription>> _subscriptions = new();
    private readonly Dictionary<int, Subscriber> _subscribers = new();
    
    public int Subscribe(EventType eventType, string topicKey, int subscriberId, ConcurrentQueue<PubSubMessage> pipe)
    {
        var topic = GetTopicName(eventType, topicKey);
        if (!_subscriptions.TryGetValue(topic, out var subscriptions))
        {
            subscriptions = new LinkedList<Subscription>();
        }
        var subscriber = _subscribers.ContainsKey(subscriberId) 
            ? _subscribers[subscriberId] 
            : new Subscriber(subscriberId);
        _subscribers[subscriberId] = subscriber;

        if (subscriptions.All(x => x.SubscriberId != subscriberId))
        {
            var subscription = new Subscription(subscriberId, pipe);
            subscriptions.AddLast(subscription);
            subscriber.Subscriptions.AddLast(subscription);
            _subscriptions[topic] = subscriptions;
        }

        return subscriber.SubscriptionsCount;
    }

    public int Unsubscribe(EventType eventType, string eventKey, int subscriberId)
    {
        var topic = GetTopicName(eventType, eventKey);
        if (!_subscribers.TryGetValue(subscriberId, out var subscriber)) return 0;
        if (!_subscriptions.TryGetValue(topic, out var subscriptions)) return subscriber.SubscriptionsCount;
        
        var subscription = subscriptions.FirstOrDefault(x => x.SubscriberId == subscriber.Id);
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
            EventType.ListPushed => PublishListPushedMessage(eventKey, subscriptions, eventPayload),
            EventType.Subscription => PublishSubscriptionMessage(eventKey, subscriptions, eventPayload),
            _ => throw new Exception($"Unknown event type {eventType}")
        };
    }

    private int PublishSubscriptionMessage(string eventKey, LinkedList<Subscription> subscriptions, string eventPayload)
    {
        var message = new PubSubMessage(EventType.Subscription, eventKey, eventPayload);
        foreach (var subscription in subscriptions)
        {
            subscription.MessagesQueue.Enqueue(message);
        }
        return subscriptions.Count;
    }

    private int PublishListPushedMessage(string eventKey, LinkedList<Subscription> subscriptions, string eventPayload)
    {
        var message = new PubSubMessage(EventType.ListPushed, eventKey, eventPayload);
        var subscription = subscriptions.FirstOrDefault(s => s.MessagesQueue.Count == 0);
        if (subscription == null) return 0;

        subscription.MessagesQueue.Enqueue(message);
        return 1;
    }

    private string GetTopicName(EventType eventType, string eventKey) => $"{eventType}:{eventKey}";
}