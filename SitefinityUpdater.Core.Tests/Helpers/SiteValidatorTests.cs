using FluentAssertions;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class SiteValidatorTests
    {
        [Fact]
        public void SiteValidationResult_ShouldInitializeProperties()
        {
            var siteId = Guid.NewGuid();
            var result = new SiteValidationResult
            {
                IsValid = true,
                SiteId = siteId,
                RequiresReconnect = false
            };

            result.IsValid.Should().BeTrue();
            result.SiteId.Should().Be(siteId);
            result.RequiresReconnect.Should().BeFalse();
        }

        [Fact]
        public void SiteValidationResult_ShouldAllowDefaultInitialization()
        {
            var result = new SiteValidationResult();

            result.IsValid.Should().BeFalse();
            result.SiteId.Should().Be(Guid.Empty);
            result.RequiresReconnect.Should().BeFalse();
        }

        [Fact]
        public void SiteValidationResult_ShouldAllowPropertyUpdates()
        {
            var result = new SiteValidationResult();
            var newSiteId = Guid.NewGuid();

            result.IsValid = true;
            result.SiteId = newSiteId;
            result.RequiresReconnect = true;

            result.IsValid.Should().BeTrue();
            result.SiteId.Should().Be(newSiteId);
            result.RequiresReconnect.Should().BeTrue();
        }

        [Fact]
        public void SiteValidationResult_ShouldSupportMultipleScenarios()
        {
            var validResult = new SiteValidationResult { IsValid = true, SiteId = Guid.NewGuid() };
            var invalidResult = new SiteValidationResult { IsValid = false, SiteId = Guid.NewGuid() };
            var reconnectResult = new SiteValidationResult { IsValid = false, SiteId = Guid.NewGuid(), RequiresReconnect = true };

            validResult.IsValid.Should().BeTrue();
            invalidResult.IsValid.Should().BeFalse();
            reconnectResult.RequiresReconnect.Should().BeTrue();
        }
    }
}
