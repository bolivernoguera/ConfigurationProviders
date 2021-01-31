using ConfigurationProviders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestApi.ConfigurationProviders.EnvironmentVariables
{
    public class EnvironmentVariablesPoolingConfigurationProvider : PoolingConfigurationProvider<EnvironmentVariablesPoolingConfigurationSource, Dictionary<string, string>>
    {
        public EnvironmentVariablesPoolingConfigurationProvider(EnvironmentVariablesPoolingConfigurationSource poolingConfigurationSource) : base(poolingConfigurationSource)
        {
        }

        protected async override Task<Dictionary<string, string>> LoadValuesAsync(CancellationToken cancellationToken)
        {
            if (poolingConfigurationSource.EnvironmentVariables == null)
            {
                return null;
            }

            Dictionary<string, string> ret = new Dictionary<string, string>();

            foreach(var environmentVariable in poolingConfigurationSource.EnvironmentVariables)
            {
                ret.Add(environmentVariable, Environment.GetEnvironmentVariable(environmentVariable));
            }
            return await Task.FromResult(ret);
        }

        protected override void OnLoadException(Exception e)
        {
        }

        protected override void OnUpdatedData()
        {
        }

        protected override IDictionary<string, string> ParseValues(Dictionary<string, string> values)
        {
            return values;
        }
    }
}
