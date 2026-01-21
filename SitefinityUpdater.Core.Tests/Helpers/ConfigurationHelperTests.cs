using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    [Collection("ConsoleTests")]
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
            var sw = new StringWriter();
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
                sw.Dispose();
            }
        }

        [Fact]
        public async Task GetSitefinityConfigAsync_ShouldPromptForMissingValues()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("http://test.com/api/default/\ntest-key\n" + Guid.NewGuid().ToString() + "\n");
            try
            {
                var inMemorySettings = new Dictionary<string, string>();

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

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
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public async Task GetSitefinityConfigAsync_ShouldThrowInvalidOperationException_WhenUrlAndAccessKeyAreEmpty()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("\n\n\n\n");
            
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                Func<Task> act = async () => await ConfigurationHelper.GetSitefinityConfigAsync(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>())
                        .Build());

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*configuration missing*");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public async Task GetSourceSitefinityConfigAsync_ShouldReturnConfig_WhenAllValuesProvidedInConfiguration()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                var siteId = Guid.NewGuid();
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"SourceSite:Url", "http://source-site.com/api/default/"},
                    {"SourceSite:AccessKey", "source-access-key"},
                    {"SourceSite:SiteId", siteId.ToString()}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                Console.SetOut(sw);

                var result = await ConfigurationHelper.GetSourceSitefinityConfigAsync(configuration);

                result.Should().NotBeNull();
                result.Url.Should().Be("http://source-site.com/api/default/");
                result.AccessKey.Should().Be("source-access-key");
                result.SiteId.Should().Be(siteId);
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public async Task GetTargetSitefinityConfigAsync_ShouldReturnConfig_WhenAllValuesProvidedInConfiguration()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                var siteId = Guid.NewGuid();
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"TargetSite:Url", "http://target-site.com/api/default/"},
                    {"TargetSite:AccessKey", "target-access-key"},
                    {"TargetSite:SiteId", siteId.ToString()}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                Console.SetOut(sw);

                var result = await ConfigurationHelper.GetTargetSitefinityConfigAsync(configuration);

                result.Should().NotBeNull();
                result.Url.Should().Be("http://target-site.com/api/default/");
                result.AccessKey.Should().Be("target-access-key");
                result.SiteId.Should().Be(siteId);
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public async Task GetSourceSitefinityConfigAsync_ShouldThrowInvalidOperationException_WhenConfigMissing()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("\n\n\n\n");
            
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                Func<Task> act = async () => await ConfigurationHelper.GetSourceSitefinityConfigAsync(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>())
                        .Build());

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*configuration missing*");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public async Task GetTargetSitefinityConfigAsync_ShouldThrowInvalidOperationException_WhenConfigMissing()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("\n\n\n\n");
            
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                Func<Task> act = async () => await ConfigurationHelper.GetTargetSitefinityConfigAsync(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>())
                        .Build());

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*configuration missing*");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldReturnDefaultPath_WhenNotConfigured()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                var inMemorySettings = new Dictionary<string, string>();

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);

                result.Should().NotBeNullOrEmpty();
                result.Should().EndWith("image_mappings_20251214_073902.csv");
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldReturnConfiguredPath_WhenProvided()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"CsvFilePath", "custom_mappings.csv"}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);

                result.Should().NotBeNullOrEmpty();
                result.Should().EndWith("custom_mappings.csv");
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void GetCsvFilePath_ShouldCombineWithBaseDirectory()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"CsvFilePath", "test.csv"}
                };

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    .Build();

                Console.SetOut(sw);

                var result = ConfigurationHelper.GetCsvFilePath(configuration);
                var baseDirectory = AppContext.BaseDirectory;

                result.Should().StartWith(baseDirectory);
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }
    }
}
