using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserHost.Utilities;

public sealed class PubSub
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = [];
    private readonly Dictionary<Type, Delegate> _commandHandlers = [];
    private readonly Lock _lock = new();
    private IPubSubDispatchStrategy _dispatchStrategy;

    public PubSub() : this(CreateDefaultDispatchStrategy())
    {
    }

    public PubSub(IPubSubDispatchStrategy dispatchStrategy)
    {
        _dispatchStrategy = dispatchStrategy;
    }

    public IPubSubDispatchStrategy DispatchStrategy
    {
        get => _dispatchStrategy;
        set => _dispatchStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Subscribe<T>(Action<T> action) where T : IEvent
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

    public void Subscribe<T>(Func<T, Task> action) where T : IEvent
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

    public void Publish<T>(T message) where T : IEvent
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out List<Delegate>? value))
        {
            var actions = value.ToArray();
            foreach (var action in actions)
            {
                if (action is Action<T> typedAction)
                {
                    _dispatchStrategy.Invoke(typedAction, message);
                }
                else if (action is Func<T, Task> asyncAction)
                {
                    _dispatchStrategy.InvokeAsync(asyncAction, message);
                }
            }
        }
    }

    public void Handle<T>(Action<T> action) where T : ICommand
    {
        var type = typeof(T);
        lock (_lock)
        {
            if (_commandHandlers.ContainsKey(type))
                throw new InvalidOperationException($"A handler for command '{type.Name}' is already registered.");

            _commandHandlers[type] = action;
        }
    }

    public void Handle<T>(Func<T, Task> action) where T : ICommand
    {
        var type = typeof(T);
        lock (_lock)
        {
            if (_commandHandlers.ContainsKey(type))
                throw new InvalidOperationException($"A handler for command '{type.Name}' is already registered.");

            _commandHandlers[type] = action;
        }
    }

    public void Send<T>(T command) where T : ICommand
    {
        var type = typeof(T);
        if (!_commandHandlers.TryGetValue(type, out var handler))
            throw new InvalidOperationException($"No handler registered for command '{type.Name}'.");

        if (handler is Action<T> typedAction)
        {
            _dispatchStrategy.Invoke(typedAction, command);
        }
        else if (handler is Func<T, Task> asyncAction)
        {
            _dispatchStrategy.InvokeAsync(asyncAction, command);
        }
        else
        {
            throw new InvalidOperationException($"Handler for command '{type.Name}' has invalid type '{handler.GetType().Name}'.");
        }
    }

    public void Unsubscribe<T>(Action<T> action) where T : IEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out List<Delegate>? value))
                value.Remove(action);
        }
    }

    public void Unsubscribe<T>(Func<T, Task> action) where T : IEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out List<Delegate>? value))
                value.Remove(action);
        }
    }

    internal static IPubSubDispatchStrategy CreateDefaultDispatchStrategy() => new MainWindowPubSubDispatchStrategy();

    public interface IPubSubDispatchStrategy
    {
        void Invoke<T>(Action<T> action, T message);
        Task InvokeAsync<T>(Func<T, Task> action, T message);
    }

    private sealed class MainWindowPubSubDispatchStrategy : IPubSubDispatchStrategy
    {
        public void Invoke<T>(Action<T> action, T message)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                try
                {
                    action(message);
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    Console.WriteLine($"Error in subscriber action: {ex.Message}");
                }
            });
        }

        public Task InvokeAsync<T>(Func<T, Task> action, T message)
        {
            return MainWindow.Instance.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await action(message);
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    Console.WriteLine($"Error in subscriber async action: {ex.Message}");
                }
            }).Task;
        }
    }
}
