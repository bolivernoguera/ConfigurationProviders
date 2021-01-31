using Microsoft.Extensions.Configuration;
using System;

namespace ConfigurationProviders
{
    public interface IPoolingConfigurationSource : IConfigurationSource
    {
        public bool ReloadOnlyOnChangeValues {get; set;}

        public bool ReloadOnChange { get; set; }

        public TimeSpan TimeBetweenBatches { get; set; }
        public bool Optional { get; set; }
    }
}
