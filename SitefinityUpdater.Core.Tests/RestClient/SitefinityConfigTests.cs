using FluentAssertions;
using SitefinityContentUpdater.Core.RestClient;

namespace SitefinityContentUpdater.Core.Tests.RestClient
{
    public class SitefinityConfigTests
    {
        [Fact]
        public void SitefinityConfig_ShouldInitializeProperties()
        {
            var config = new SitefinityConfig
            {
                Url = "http://localhost:8080/api/default/",
                AccessKey = "test-key",
                SiteId = Guid.NewGuid()
            };

            config.Url.Should().Be("http://localhost:8080/api/default/");
            config.AccessKey.Should().Be("test-key");
            config.SiteId.Should().NotBeEmpty();
        }

        [Fact]
        public void SitefinityConfig_ShouldAllowEmptyInitialization()
        {
            var config = new SitefinityConfig();

            config.Url.Should().BeNull();
            config.AccessKey.Should().BeNull();
            config.SiteId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void SitefinityConfig_ShouldAllowPropertyUpdates()
        {
            var config = new SitefinityConfig();
            var siteId = Guid.NewGuid();

            config.Url = "https://example.com/api/default/";
            config.AccessKey = "new-key";
            config.SiteId = siteId;

            config.Url.Should().Be("https://example.com/api/default/");
            config.AccessKey.Should().Be("new-key");
            config.SiteId.Should().Be(siteId);
        }
    }
}
