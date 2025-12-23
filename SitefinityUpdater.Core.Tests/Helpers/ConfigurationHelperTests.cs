using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class ConfigurationHelperTests
    {
        [Fact]
        public void LoadConfiguration_ShouldReturnIConfiguration()
        {
            var config = ConfigurationHelper.LoadConfiguration();
            
            config.Should().NotBeNull();
            config.Should().BeAssignableTo<IConfiguration>();
        }

        [Fact]
        public async Task GetSitefinityConfigAsync_ShouldReturnConfig_WhenAllValuesProvidedInConfiguration()
        {
            var originalOut = Console.Out;
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"Sitefinity:Url", "http://localhost:8080/api/default/"},
                    {"Sitefinity:AccessKey", "test-access-key"},
                    {"Sitefinity:SiteId", Guid.NewGuid().ToString()}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var result = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

                result.Should().NotBeNull();
                result.Url.Should().Be("http://localhost:8080/api/default/");
                result.AccessKey.Should().Be("test-access-key");
                result.SiteId.Should().NotBe(Guid.Empty);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task GetSitefinityConfigAsync_ShouldPromptForMissingValues()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                var inMemorySettings = new Dictionary<string, string>();

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                using var sr = new StringReader("http://test.com/api/default/\ntest-key\n" + Guid.NewGuid().ToString() + "\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

                result.Should().NotBeNull();
                result.Url.Should().NotBeNullOrEmpty();
                result.AccessKey.Should().NotBeNullOrEmpty();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public async Task GetSitefinityConfigAsync_ShouldThrowInvalidOperationException_WhenUrlAndAccessKeyAreEmpty()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                var inMemorySettings = new Dictionary<string, string>();

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                using var sr = new StringReader("\n\n\n\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                Func<Task> act = async () => await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Required configuration missing");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldReturnDefaultPath_WhenNotConfigured()
        {
            var originalOut = Console.Out;
            try
            {
                var inMemorySettings = new Dictionary<string, string>();

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);

                result.Should().NotBeNullOrEmpty();
                result.Should().EndWith("image_mappings_20251214_073902.csv");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldReturnConfiguredPath_WhenProvided()
        {
            var originalOut = Console.Out;
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"CsvFilePath", "custom_mappings.csv"}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);

                result.Should().NotBeNullOrEmpty();
                result.Should().EndWith("custom_mappings.csv");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldCombineWithBaseDirectory()
        {
            var originalOut = Console.Out;
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"CsvFilePath", "test.csv"}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                using var sw = new StringWriter();
                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);
                var baseDirectory = AppContext.BaseDirectory;

                result.Should().StartWith(baseDirectory);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
