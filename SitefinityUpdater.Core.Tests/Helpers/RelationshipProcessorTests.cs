using FluentAssertions;
using Moq;
using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class RelationshipProcessorTests
    {
        private readonly Mock<IRestClient> _mockSourceClient;
        private readonly Mock<IRestClient> _mockTargetClient;

        public RelationshipProcessorTests()
        {
            _mockSourceClient = new Mock<IRestClient>();
            _mockTargetClient = new Mock<IRestClient>();
        }

        [Fact]
        public void RelationshipProcessor_ShouldThrowArgumentNullException_WhenSourceClientIsNull()
        {
            Action act = () => new RelationshipProcessor(null!, _mockTargetClient.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("sourceClient");
        }

        [Fact]
        public void RelationshipProcessor_ShouldThrowArgumentNullException_WhenTargetClientIsNull()
        {
            Action act = () => new RelationshipProcessor(_mockSourceClient.Object, null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("targetClient");
        }

        [Fact]
        public void RelationshipProcessor_ShouldInitialize_WithValidParameters()
        {
            Action act = () => new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldThrowArgumentNullException_WhenContentTypeIsNull()
        {
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            Func<Task> act = async () => await processor.BuildRelationshipsAsync(null!, new[] { "RelatedNews" });

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("contentType");
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldThrowArgumentException_WhenRelationshipFieldNamesIsEmpty()
        {
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            Func<Task> act = async () => await processor.BuildRelationshipsAsync("newsitems", Array.Empty<string>());

            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("relationshipFieldNames");
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldThrowArgumentException_WhenRelationshipFieldNamesIsNull()
        {
            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            Func<Task> act = async () => await processor.BuildRelationshipsAsync("newsitems", null!);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("relationshipFieldNames");
        }
    }
}
