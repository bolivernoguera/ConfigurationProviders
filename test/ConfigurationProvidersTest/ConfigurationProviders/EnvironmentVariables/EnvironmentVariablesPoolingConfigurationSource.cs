using ConfigurationProviders;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace TestApi.ConfigurationProviders.EnvironmentVariables
{
    public class EnvironmentVariablesPoolingConfigurationSource : IPoolingConfigurationSource
    {
        public bool ReloadOnlyOnChangeValues { get; set; }
        public bool ReloadOnChange { get; set; }
        public TimeSpan TimeBetweenBatches { get; set; }
        public bool Optional { get; set; }

        public List<string> EnvironmentVariables { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentVariablesPoolingConfigurationProvider(this);
        }
    }
}
