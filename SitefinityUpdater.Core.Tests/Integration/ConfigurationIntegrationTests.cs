using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SitefinityContentUpdater.Core.Helpers;
using SitefinityContentUpdater.Core.RestClient;

namespace SitefinityContentUpdater.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the interaction between multiple components.
    /// These tests don't require a live Sitefinity instance but test component integration.
    /// </summary>
    public class ConfigurationIntegrationTests
    {
        [Fact]
        public void LoadConfiguration_And_GetCsvFilePath_ShouldWorkTogether()
        {
            var originalOut = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);

                var config = ConfigurationHelper.LoadConfiguration();
                config.Should().NotBeNull();

                var csvPath = ConfigurationHelper.GetCsvFilePath(config);
                
                csvPath.Should().NotBeNullOrEmpty();
                Path.IsPathRooted(csvPath).Should().BeTrue();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task ConfigurationHelper_And_RestClientFactory_ShouldWorkTogether()
        {
            var originalOut = Console.Out;
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"Sitefinity:Url", "http://localhost:8080/api/default/"},
                    {"Sitefinity:AccessKey", "test-key"},
                    {"Sitefinity:SiteId", Guid.NewGuid().ToString()}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var config = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);
                
                config.Should().NotBeNull();
                config.Url.Should().NotBeNullOrEmpty();
                config.AccessKey.Should().NotBeNullOrEmpty();
                config.SiteId.Should().NotBe(Guid.Empty);

                try
                {
                    var client = await RestClientFactory.GetRestClient(config);
                    client.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    (ex is HttpRequestException || ex is TaskCanceledException).Should().BeTrue();
                }
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void ConfigurationHelper_ShouldHandleMultipleConfigurations()
        {
            var originalOut = Console.Out;
            try
            {
                var config1 = new Dictionary<string, string>
                {
                    {"CsvFilePath", "file1.csv"}
                };

                var config2 = new Dictionary<string, string>
                {
                    {"CsvFilePath", "file2.csv"}
                };

                IConfiguration configuration1 = new ConfigurationBuilder()
                    .AddInMemoryCollection(config1)
                    .Build();

                IConfiguration configuration2 = new ConfigurationBuilder()
                    .AddInMemoryCollection(config2)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var path1 = ConfigurationHelper.GetCsvFilePath(configuration1);
                var path2 = ConfigurationHelper.GetCsvFilePath(configuration2);

                path1.Should().EndWith("file1.csv");
                path2.Should().EndWith("file2.csv");
                path1.Should().NotBe(path2);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
