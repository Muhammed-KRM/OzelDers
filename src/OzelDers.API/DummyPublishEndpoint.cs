using MassTransit;

namespace OzelDers.API;

public class DummyPublishEndpoint : IPublishEndpoint
{
    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Console.WriteLine($"[DUMMY EVENT PUBLISHED]: {typeof(T).Name}");
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

    public Task Publish(object message, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new DummyConnectHandle();
}

public class DummyConnectHandle : ConnectHandle
{
    public void Dispose() { }
    public void Disconnect() { }
}
