using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurationProviders
{
    public abstract class PoolingConfigurationProvider<T,P> : ConfigurationProvider, IDisposable where T: IPoolingConfigurationSource
    {
        protected readonly CancellationTokenSource cancellationTokenSource;

        protected readonly T poolingConfigurationSource;

        private Thread backgroundThread;

        protected PoolingConfigurationProvider(T poolingConfigurationSource)
        {
            cancellationTokenSource = new CancellationTokenSource();

            this.poolingConfigurationSource = poolingConfigurationSource;

            disposed = false;
        }

        public override void Load()
        {
            if (backgroundThread != null)
            {
                return;
            }

            if (!DoLoadAsync(cancellationTokenSource.Token).GetAwaiter().GetResult() && !poolingConfigurationSource.Optional)
            {
                throw new Exception($"Unable to load data from {GetType().Name} and it's not optional");
            }

            // Polling starts after the initial load to ensure no concurrent access to the key from this instance
            if (poolingConfigurationSource.ReloadOnChange)
            {
                backgroundThread = new Thread(async () => await PollingLoop())
                {
                    Name = $"Background thread for {GetType().Name}",
                    Priority = ThreadPriority.BelowNormal,
                    IsBackground = true
                };

                backgroundThread.Start();
            }
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            disposed = true;
        }

        private async Task PollingLoop()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await DoLoadAsync(cancellationTokenSource.Token);

                TimeSpan wait = poolingConfigurationSource.TimeBetweenBatches != default ? poolingConfigurationSource.TimeBetweenBatches : TimeSpan.FromMinutes(5);
                await Task.Delay(wait);
            }
        }

        protected abstract IDictionary<string, string> ParseValues(P values);

        protected abstract Task<P> LoadValuesAsync(CancellationToken cancellationToken);

        protected abstract void OnLoadException(Exception e);

        protected abstract void OnUpdatedData();

        protected virtual void UpdateData(IDictionary<string, string> newData)
        {
            Data = newData;
            OnReload();
            OnUpdatedData();
        }

        private async Task<bool> DoLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var values = await LoadValuesAsync(cancellationToken);

                IDictionary<string, string> newData = ParseValues(values);

                if (!poolingConfigurationSource.ReloadOnlyOnChangeValues || !DictionaryExtensions.Equals(newData, this.Data))
                {
                    UpdateData(newData);
                }
            }
            catch (Exception exception)
            {
                OnLoadException(exception);
                return false;
            }

            return true;
        }
    }

    public static class DictionaryExtensions
    {
        public static bool Equals(this IDictionary<string, string> d1, IDictionary<string, string> d2)
        {
            return Compare(d1, d2) && Compare(d2, d1);
        }

        private static bool Compare(this IDictionary<string, string> d1, IDictionary<string, string> d2)
        {
            foreach (var kv in d1)
            {
                if (!d2.TryGetValue(kv.Key, out var value))
                {
                    return false;
                }

                if (value == null && kv.Value == null)
                {
                    continue;
                }
                else if ((value == null && kv.Value != null) || (kv.Value == null && value != null))
                {
                    return false;
                }

                if (!value.Equals(kv.Value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
