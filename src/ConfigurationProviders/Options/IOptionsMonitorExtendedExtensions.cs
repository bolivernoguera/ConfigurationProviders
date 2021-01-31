using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace ConfigurationProviders.Options
{
    public static class IOptionsMonitorExtendedExtensions
    {
        public static IServiceCollection AddSafeOptions(this IServiceCollection serviceCollection, Action<object, Type, Exception> OnOptionsMonitorUpdateException)
        {
            serviceCollection.AddSingleton(_ => new UpdateSafeOptionsMonitorBindingExceptionNotifier(OnOptionsMonitorUpdateException));
            serviceCollection.Add(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(UpdateSafeOptionsMonitor<>)));
            serviceCollection.Add(ServiceDescriptor.Singleton(typeof(IOptionsMonitorExtended<>), typeof(UpdateSafeOptionsMonitor<>)));

            return serviceCollection;
        }

        public static IServiceCollection AddSafeOptions(this IServiceCollection serviceCollection) => serviceCollection.AddSafeOptions((_, __, ___) => { });

        public static IDisposable OnChangeException<TOptions>(this IOptionsMonitorExtended<TOptions> monitor, Action<TOptions, Exception> listener) => monitor.OnChangeException((o, _, e) => listener(o, e));
    }
}
