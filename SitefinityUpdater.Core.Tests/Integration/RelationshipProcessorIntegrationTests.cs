using FluentAssertions;
using Moq;
using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for the RelationshipProcessor.
    /// Note: These tests require actual Sitefinity SDK types which are difficult to mock.
    /// For full integration testing, use a test Sitefinity instance.
    /// </summary>
    public class RelationshipProcessorIntegrationTests : IDisposable
    {
        private readonly Mock<IRestClient> _mockSourceClient;
        private readonly Mock<IRestClient> _mockTargetClient;

        public RelationshipProcessorIntegrationTests()
        {
            _mockSourceClient = new Mock<IRestClient>();
            _mockTargetClient = new Mock<IRestClient>();
        }

        [Fact]
        public void RelationshipProcessor_ShouldAcceptSourceAndTargetClients()
        {
            // Arrange & Act
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            // Assert
            processor.Should().NotBeNull();
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldValidateContentType()
        {
            // Arrange
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => processor.BuildRelationshipsAsync(null!, new[] { "RelatedNews" }));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldValidateFieldNames()
        {
            // Arrange
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            // Act & Assert - Empty array
            await Assert.ThrowsAsync<ArgumentException>(
                () => processor.BuildRelationshipsAsync("newsitems", Array.Empty<string>()));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldValidateFieldNamesNotNull()
        {
            // Arrange
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            // Act & Assert - Null
            await Assert.ThrowsAsync<ArgumentException>(
                () => processor.BuildRelationshipsAsync("newsitems", null!));
        }

        [Fact]
        public void RelationshipProcessor_ShouldThrowOnNullSourceClient()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new RelationshipProcessor(null!, _mockTargetClient.Object));
        }

        [Fact]
        public void RelationshipProcessor_ShouldThrowOnNullTargetClient()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new RelationshipProcessor(_mockSourceClient.Object, null!));
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
