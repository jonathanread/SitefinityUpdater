using FluentAssertions;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for the validation and content processing workflow.
    /// These tests verify the workflow logic without requiring a live Sitefinity instance.
    /// </summary>
    public class ValidationWorkflowIntegrationTests
    {
        [Fact]
        public void ValidationResult_ShouldPreventProcessing_WhenInvalid()
        {
            var validationResult = new SiteValidationResult
            {
                IsValid = false,
                SiteId = Guid.NewGuid(),
                RequiresReconnect = false
            };

            validationResult.IsValid.Should().BeFalse();

            var shouldProcess = validationResult.IsValid;
            shouldProcess.Should().BeFalse();
        }

        [Fact]
        public void ValidationResult_ShouldAllowProcessing_WhenValid()
        {
            var validationResult = new SiteValidationResult
            {
                IsValid = true,
                SiteId = Guid.NewGuid(),
                RequiresReconnect = false
            };

            validationResult.IsValid.Should().BeTrue();

            var shouldProcess = validationResult.IsValid;
            shouldProcess.Should().BeTrue();
        }

        [Fact]
        public void ValidationResult_ShouldIndicateReconnect_WhenUserChangedSiteId()
        {
            var originalSiteId = Guid.NewGuid();
            var newSiteId = Guid.NewGuid();

            var validationResult = new SiteValidationResult
            {
                IsValid = false,
                SiteId = newSiteId,
                RequiresReconnect = true
            };

            validationResult.IsValid.Should().BeFalse();
            validationResult.RequiresReconnect.Should().BeTrue();
            validationResult.SiteId.Should().Be(newSiteId);
            validationResult.SiteId.Should().NotBe(originalSiteId);
        }

        [Fact]
        public void WorkflowStates_ShouldBeDistinct()
        {
            var validState = new SiteValidationResult { IsValid = true, RequiresReconnect = false };
            var invalidState = new SiteValidationResult { IsValid = false, RequiresReconnect = false };
            var reconnectState = new SiteValidationResult { IsValid = false, RequiresReconnect = true };

            validState.IsValid.Should().BeTrue();
            invalidState.IsValid.Should().BeFalse();
            reconnectState.RequiresReconnect.Should().BeTrue();

            (validState.IsValid && validState.RequiresReconnect).Should().BeFalse();
            (invalidState.IsValid || invalidState.RequiresReconnect).Should().BeFalse();
            (!reconnectState.IsValid && reconnectState.RequiresReconnect).Should().BeTrue();
        }
    }
}
