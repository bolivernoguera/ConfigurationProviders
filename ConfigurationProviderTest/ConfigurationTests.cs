using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TestApi;
using TestApi.Options;
using System;
using ConfigurationProviders;
using TestApi.ConfigurationProviders.EnvironmentVariables;
using System.Collections.Generic;

namespace ConfigurationProviderTest
{
    [TestClass]
    public class ConfigurationTests
    {
        private static TestServer CreateTestServer(List<string> environmentVariables = null, bool optional = false)
        {
            var webHostBuilder =
                  new WebHostBuilder()
                        .ConfigureAppConfiguration(builder => builder.AddConfiguration(GetConfiguration(environmentVariables, optional)))

                        .UseEnvironment("Test") // You can set the environment you want (development, staging, production)

                        .UseStartup<Startup>(); // Startup class of your web app project

            return new TestServer(webHostBuilder);
        }

        private const int TimeBetweenBatchesSeconds = 1;
        private static IConfigurationRoot GetConfiguration(List<string> environmentVariables, bool optional)
        {
            //Adding a simple ConfigurationSource that reloads IOptions from EnvironmentVariables 

            return new ConfigurationBuilder()
                .AddEnvironmentVariables()

                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: false)

                .AddPollingProvider<EnvironmentVariablesPoolingConfigurationSource>(options =>
                {
                    options.Optional = optional;
                    options.ReloadOnChange = true;
                    options.ReloadOnlyOnChangeValues = true;
                    options.TimeBetweenBatches = TimeSpan.FromSeconds(TimeBetweenBatchesSeconds);
                    options.EnvironmentVariables = environmentVariables;
                })
                .Build();
        }

        private static async Task<TestApiOptions> GetAsync(HttpClient client)
        {
            using HttpResponseMessage httpResponseMessage = await client.GetAsync("/api");

            Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, $"HttpStatus code is not OK recieved:{httpResponseMessage.StatusCode}");

            string result = await httpResponseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TestApiOptions>(result);
        }

        [TestMethod]
        public void TestOptional()
        {
            using var server = CreateTestServer(null, true);
            using var client = server.CreateClient();
        }

        [TestMethod]
        public void TestNotOptional()
        {
            Exception ex = null;

            const string exMsg = "Unable to load data from EnvironmentVariablesPoolingConfigurationProvider and it's not optional";

            try
            {
                using var server = CreateTestServer();
                using var client = server.CreateClient();
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.IsNotNull(ex, $"App should crash with error {exMsg}");

            Assert.AreEqual(ex.Message, exMsg);
        }

        [TestMethod]
        public async Task TestOptionsMatch()
        {
            const string keyValue1 = "this is the value1";
            const string keyValue2 = "23";

            Environment.SetEnvironmentVariable("Value1", keyValue1);
            Environment.SetEnvironmentVariable("Value2", keyValue2);

            using var server = CreateTestServer(new List<string>());
            using var client = server.CreateClient();

            var options = await GetAsync(client);

            Assert.AreEqual(options.Value1, keyValue1);
            Assert.AreEqual(options.Value2, int.Parse(keyValue2));
        }

        [TestMethod]
        public async Task TestSuccessReloadOptions()
        {
            const string key = "Value1";
            const string keyValue1 = "this is the value1";
            const string keyValue2 = "value has changed";

            await ReloadOptions(key, keyValue1, keyValue2, (options, v1, _) => Assert.AreEqual(options.Value1, v1),
                (options, _, v2) => Assert.AreEqual(options.Value1, v2));
        }

        [TestMethod]
        public async Task TestFailReloadOptions()
        {
            //Value2 is int, should fail update when KeyValue2 is setted because it's not an int, should keep old value

            const string key = "Value2";
            const string keyValue1 = "25";
            const string keyValue2 = "wrong int value";

            await ReloadOptions(key, keyValue1, keyValue2, (options, v1, _) => Assert.AreEqual(options.Value2, int.Parse(v1)),
                (options, v1, _) => Assert.AreEqual(options.Value2, int.Parse(v1)));
        }

        private static async Task ReloadOptions(string key, string value1, string value2, Action<TestApiOptions, string, string> compare1, Action<TestApiOptions, string, string> compare2)
        {
            Environment.SetEnvironmentVariable(key, value1);

            using var server = CreateTestServer(new List<string>() { key });
            using var client = server.CreateClient();

            var options = await GetAsync(client);

            compare1(options, value1, value2);

            Environment.SetEnvironmentVariable(key, value2);

            await Task.Delay(TimeSpan.FromSeconds(TimeBetweenBatchesSeconds + 1)); //1 second margin

            options = await GetAsync(client);

            compare2(options, value1, value2);
        }
    }
}
