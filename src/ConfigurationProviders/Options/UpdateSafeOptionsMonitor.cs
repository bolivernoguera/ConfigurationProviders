using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ConfigurationProviders.Options
{
    /// <summary>
    /// Implementation of <see cref="IOptionsMonitor{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    public class UpdateSafeOptionsMonitor<TOptions> : IOptionsMonitorExtended<TOptions>, IDisposable where TOptions : class, new()
    {
        private readonly IOptionsMonitorCache<TOptions> _cache;
        private readonly UpdateSafeOptionsMonitorBindingExceptionNotifier _optionsMonitorBindingExceptionNotifier;
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        internal event Action<TOptions, string> _onChange;

        internal event Action<TOptions, string, Exception> _onChangeException;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        /// <param name="cache">The cache used to store options.</param>
        /// <param name="optionsMonitorBindingExceptionNotifier">The action to execute in case of binding exception.</param>
        public UpdateSafeOptionsMonitor(
            IOptionsFactory<TOptions> factory,
            IEnumerable<IOptionsChangeTokenSource<TOptions>> sources,
            IOptionsMonitorCache<TOptions> cache,
            UpdateSafeOptionsMonitorBindingExceptionNotifier optionsMonitorBindingExceptionNotifier
            )
        {
            _factory = factory;
            _cache = cache;
            _optionsMonitorBindingExceptionNotifier = optionsMonitorBindingExceptionNotifier;
            foreach (var source in sources)
            {
                var registration = ChangeToken.OnChange(
                      () => source.GetChangeToken(),
                      InvokeChanged,
                      source.Name);

                _registrations.Add(registration);
            }
        }

        private void InvokeChanged(string name)
        {
            name ??= Microsoft.Extensions.Options.Options.DefaultName;
            var currentOptions = Get(name);
            _cache.TryRemove(name);
            TOptions options;
            try
            {
                options = _factory.Create(name);
                _cache.TryAdd(name, options);
                _onChange?.Invoke(options, name);
            }
            catch (Exception ex)
            {
                _cache.TryAdd(name, currentOptions);
                options = currentOptions;
                _optionsMonitorBindingExceptionNotifier.NotifyException?.Invoke(options, options.GetType(), ex);
                _onChangeException?.Invoke(options, name, ex);
            }
        }

        /// <summary>
        /// The present value of the options.
        /// </summary>
        public TOptions CurrentValue
        {
            get
            {
                try
                {
                    return Get(Microsoft.Extensions.Options.Options.DefaultName);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        public virtual TOptions Get(string name)
        {
            name ??= Microsoft.Extensions.Options.Options.DefaultName;
            return _cache.GetOrAdd(name, () => _factory.Create(name));
        }

        /// <summary>
        /// Registers a listener to be called whenever <typeparamref name="TOptions"/> changes.
        /// </summary>
        /// <param name="listener">The action to be invoked when <typeparamref name="TOptions"/> has changed.</param>
        /// <returns>An <see cref="IDisposable"/> which should be disposed to stop listening for changes.</returns>
        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            var disposable = new ChangeTrackerOnChangeDisposable(this, listener);
            _onChange += disposable.OnChange;
            return disposable;
        }

        public IDisposable OnChangeException(Action<TOptions, string, Exception> listener)
        {
            var disposable = new ChangeTrackerOnChangeExceptionDisposable(this, listener);
            _onChangeException += disposable.OnChangeException;
            return disposable;
        }

        /// <summary>
        /// Removes all change registration subscriptions.
        /// </summary>
        public void Dispose()
        {
            // Remove all subscriptions to the change tokens
            foreach (var registration in _registrations)
            {
                registration.Dispose();
            }

            _registrations.Clear();
        }

        internal class ChangeTrackerOnChangeDisposable : IDisposable
        {
            private readonly Action<TOptions, string> _listener;

            private readonly UpdateSafeOptionsMonitor<TOptions> _monitor;

            public ChangeTrackerOnChangeDisposable(UpdateSafeOptionsMonitor<TOptions> monitor, Action<TOptions, string> listener)
            {
                _listener = listener;
                _monitor = monitor;
            }

            public void OnChange(TOptions options, string name) => _listener.Invoke(options, name);

            public void Dispose() => _monitor._onChange -= OnChange;
        }

        internal class ChangeTrackerOnChangeExceptionDisposable : IDisposable
        {
            private readonly Action<TOptions, string, Exception> _listener;

            private readonly UpdateSafeOptionsMonitor<TOptions> _monitor;

            public ChangeTrackerOnChangeExceptionDisposable(UpdateSafeOptionsMonitor<TOptions> monitor, Action<TOptions, string, Exception> listener)
            {
                _listener = listener;
                _monitor = monitor;
            }

            public void OnChangeException(TOptions options, string name, Exception e) => _listener.Invoke(options, name, e);

            public void Dispose() => _monitor._onChangeException -= OnChangeException;
        }
    }
}
