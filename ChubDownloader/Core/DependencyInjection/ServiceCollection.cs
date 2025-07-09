namespace ChubDownloader.Core.DependencyInjection;

public interface IServiceProvider
{
    object? GetService(Type serviceType);
    T? GetService<T>() where T : class;
    T GetRequiredService<T>() where T : class;
}

public sealed class ServiceCollection : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();

    public ServiceCollection AddSingleton<T>(T instance) where T : class
    {
        _services[typeof(T)] = instance;
        return this;
    }

    public ServiceCollection AddSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface, new()
    {
        _factories[typeof(TInterface)] = provider => 
        {
            if (!_services.ContainsKey(typeof(TInterface)))
            {
                _services[typeof(TInterface)] = new TImplementation();
            }
            return _services[typeof(TInterface)];
        };
        return this;
    }

    public ServiceCollection AddSingleton<TInterface>(Func<IServiceProvider, TInterface> factory)
        where TInterface : class
    {
        _factories[typeof(TInterface)] = provider => 
        {
            if (!_services.ContainsKey(typeof(TInterface)))
            {
                _services[typeof(TInterface)] = factory(provider);
            }
            return (TInterface)_services[typeof(TInterface)];
        };
        return this;
    }

    public ServiceCollection AddTransient<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface, new()
    {
        _factories[typeof(TInterface)] = _ => new TImplementation();
        return this;
    }

    public ServiceCollection AddTransient<TInterface>(Func<IServiceProvider, TInterface> factory)
        where TInterface : class
    {
        _factories[typeof(TInterface)] = provider => factory(provider);
        return this;
    }

    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var instance))
            return instance;

        if (_factories.TryGetValue(serviceType, out var factory))
        {
            var newInstance = factory(this);
            if (IsRegisteredAsSingleton(serviceType))
            {
                _services[serviceType] = newInstance;
            }
            return newInstance;
        }

        return null;
    }

    public T? GetService<T>() where T : class
    {
        return GetService(typeof(T)) as T;
    }

    public T GetRequiredService<T>() where T : class
    {
        return GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} not registered");
    }

    private bool IsRegisteredAsSingleton(Type serviceType)
    {
        return _services.ContainsKey(serviceType);
    }
}