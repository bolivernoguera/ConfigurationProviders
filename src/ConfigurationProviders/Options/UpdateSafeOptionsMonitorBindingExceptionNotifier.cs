using System;

namespace ConfigurationProviders.Options
{
    public class UpdateSafeOptionsMonitorBindingExceptionNotifier
    {
        internal Action<object, Type, Exception> NotifyException { get; }

        internal UpdateSafeOptionsMonitorBindingExceptionNotifier(Action<object, Type, Exception> notifyExceptionAction)
        {
            NotifyException = notifyExceptionAction;
        }
    }
}
