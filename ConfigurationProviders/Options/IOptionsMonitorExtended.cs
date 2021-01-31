using Microsoft.Extensions.Options;
using System;

namespace ConfigurationProviders.Options
{
    public interface IOptionsMonitorExtended<out TOptions> : IOptionsMonitor<TOptions>
    {
        IDisposable OnChangeException(Action<TOptions, string, Exception> listener);
    }
}
