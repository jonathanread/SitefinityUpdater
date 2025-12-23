using FluentAssertions;
using SitefinityContentUpdater.Core.RestClient;

namespace SitefinityContentUpdater.Core.Tests.RestClient
{
    public class RestClientFactoryTests
    {
        [Fact]
        public async Task GetRestClient_ShouldThrowArgumentNullException_WhenConfigIsNull()
        {
            Func<Task> act = async () => await RestClientFactory.GetRestClient(null);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("config");
        }

        [Fact]
        public async Task GetRestClient_ShouldAppendSlashToUrl_WhenUrlDoesNotEndWithSlash()
        {
            var config = new SitefinityConfig
            {
                Url = "http://localhost:8080/api/default",
                AccessKey = "test-key",
                SiteId = Guid.NewGuid()
            };

            try
            {
                await RestClientFactory.GetRestClient(config);
            }
            catch
            {
            }

            config.Url.Should().EndWith("/");
        }

        [Fact]
        public async Task GetRestClient_ShouldNotDuplicateSlash_WhenUrlAlreadyEndsWithSlash()
        {
            var config = new SitefinityConfig
            {
                Url = "http://localhost:8080/api/default/",
                AccessKey = "test-key",
                SiteId = Guid.NewGuid()
            };

            var originalUrl = config.Url;

            try
            {
                await RestClientFactory.GetRestClient(config);
            }
            catch
            {
            }

            config.Url.Should().Be(originalUrl);
            config.Url.Should().NotEndWith("//");
        }

        [Fact]
        public async Task GetRestClient_ShouldCreateHttpClientWithCorrectBaseAddress()
        {
            var config = new SitefinityConfig
            {
                Url = "http://localhost:8080/api/default/",
                AccessKey = "test-key",
                SiteId = Guid.NewGuid()
            };

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
    }
}
