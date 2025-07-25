namespace FileCraft.Core.DependencyInjection
{
    public class ServiceProvider
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new();
        private readonly Dictionary<Type, object> _singletons = new();

        public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService
        {
            _registrations[typeof(TService)] = () =>
            {
                if (!_singletons.TryGetValue(typeof(TService), out var instance))
                {
                    instance = CreateInstance(typeof(TImplementation));
                    _singletons[typeof(TService)] = instance;
                }
                return instance;
            };
        }

        public void RegisterTransient<TService, TImplementation>() where TImplementation : TService
        {
            _registrations[typeof(TService)] = () => CreateInstance(typeof(TImplementation));
        }

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        private object GetService(Type serviceType)
        {
            if (!_registrations.TryGetValue(serviceType, out var factory))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
            }
            return factory();
        }

        private object CreateInstance(Type implementationType)
        {
            var constructor = implementationType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = constructor.GetParameters()
                .Select(p => GetService(p.ParameterType))
                .ToArray();

            return constructor.Invoke(parameters);
        }
    }
}
