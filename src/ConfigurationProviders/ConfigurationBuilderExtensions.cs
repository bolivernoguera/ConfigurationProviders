using Microsoft.Extensions.Configuration;
using System;

namespace ConfigurationProviders
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddPollingProvider<T>(this IConfigurationBuilder builder, Action<T> options) where T : IPoolingConfigurationSource, new()
        {
            var source = new T();
            options(source);
            return builder.Add(source);
        }
    }
}
