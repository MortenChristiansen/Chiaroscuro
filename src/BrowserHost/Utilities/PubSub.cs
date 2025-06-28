using System;
using System.Collections.Generic;
using System.Threading;

namespace BrowserHost.Utilities;

public static class PubSub
{
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = [];
    private static readonly Lock _lock = new();

    public static void Subscribe<T>(Action<T> action)
    {
        var type = typeof(T);
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(type, out List<Delegate>? value))
            {
                value = [];
                _subscribers[type] = value;
            }

            value.Add(action);
        }
    }

    public static void Publish<T>(T message)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out List<Delegate>? value))
        {
            var actions = value.ToArray();
            foreach (var action in actions)
            {
                if (action is Action<T> typedAction)
                {
                    MainWindow.Instance.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            typedAction(message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in subscriber action: {ex.Message}");
                        }
                    });
                }
            }
        }
    }

    public static void Unsubscribe<T>(Action<T> action)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out List<Delegate>? value))
                value.Remove(action);
        }
    }
}
