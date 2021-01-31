# ConfigurationProviders

* Using standard [.NET 5.0](https://docs.microsoft.com/en-us/dotnet/core/dotnet-five)
* Download [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)

# Safe Options

To use safe options update add in StartUp ConfigureServices the following code:

services.AddSafeOptions();

When update options crash or fail will remain the previous value.

# PoolingConfigurationProviders

Project defines a abstract class PoolingConfigurationProvider that can be inherited to get key values from any source.
This class must inherit from a IPoolingConfigurationSource.

Also a ConfigurationBuilderExtensions is defined to add the ConfigurationProvider in Startup ConfigureServices.


### Tests

To test the application a TestServer(TestApi) is executed with a test ConfigurationProvider(EnvironmentVariablesPoolingConfigurationProvider).

The following tests are available in ConfigurationProvidersTest:

*TestOptional: Test that checks that the application should not crash when ConfigurationProvider is optional.
*TestNotOptional: Test that checks that the application should crash when ConfigurationProvider is not optional.
*TestOptionsMatch: Test that checks that mapped IOptionsMonitor matches with ConfigurationProvider.
*TestSuccessReloadOptions: Test that checks that options are reloaded when ConfigurationProvider change the values.
*TestFailReloadOptions: Test that checks that options are not reloaded when ConfigurationProvider change the values and crash.