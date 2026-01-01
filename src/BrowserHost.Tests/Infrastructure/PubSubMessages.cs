using BrowserHost.Utilities;
using System.Collections.Concurrent;
using System.Reflection;

namespace BrowserHost.Tests.Infrastructure;

/// <summary>
/// Provides utilities for capturing and retrieving published messages for the current test.
/// </summary>
public static class PubSubMessages
{
    private static readonly AsyncLocal<ConcurrentQueue<object?>?> _messages = new();
    private static readonly Lazy<IReadOnlyList<Type>> _cachedMessageTypes = new(() => [.. GetCandidateMessageTypes()]);
    private static readonly Lazy<MethodInfo> _cachedSubscribeMethod = new(GetSubscribeMethod);

    public static IEnumerable<T> OfType<T>() => MessagesSnapshot().OfType<T>();

    internal static void AttachTo(PubSub pubSub)
    {
        _messages.Value = new ConcurrentQueue<object?>();

        foreach (var type in _cachedMessageTypes.Value)
            SubscribeRecorder(pubSub, type);
    }

    internal static void Detach()
    {
        _messages.Value = null;
    }

    private static IReadOnlyCollection<object?> MessagesSnapshot() =>
        _messages.Value?.ToArray() ?? [];

    private static void SubscribeRecorder(PubSub pubSub, Type messageType)
    {
        var subscribeMethod = _cachedSubscribeMethod.Value;

        var recordMethod = typeof(PubSubMessages)
            .GetMethod(nameof(RecordGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(messageType);

        var actionType = typeof(Action<>).MakeGenericType(messageType);
        var action = Delegate.CreateDelegate(actionType, recordMethod);

        var genericSubscribe = subscribeMethod.MakeGenericMethod(messageType);
        genericSubscribe.Invoke(pubSub, [action]);
    }

    private static IEnumerable<Type> GetCandidateMessageTypes() =>
        typeof(PubSub).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("BrowserHost.", StringComparison.Ordinal) == true)
            .Where(t => t.Name.EndsWith("Event", StringComparison.Ordinal))
            .Where(t => !t.IsAbstract)
            .Where(t => !t.IsGenericTypeDefinition);

    private static MethodInfo GetSubscribeMethod() =>
        typeof(PubSub)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(m =>
                m.Name == nameof(PubSub.Subscribe) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.IsGenericType &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Action<>)
            );

    private static void RecordGeneric<T>(T message) =>
        _messages.Value?.Enqueue(message);
}