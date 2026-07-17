using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Client;
using Progress.Sitefinity.RestSdk.Dto;
using SitefinityContentUpdater.Core.Helpers;
using System.Reflection;

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

        // ── BuildRelationshipsAsync argument validation ──────────────────────────

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

        // ── BuildRelationshipsAsync happy path ───────────────────────────────────

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldReturnCompleted_WhenNoItemsExistInSource()
        {
            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [], TotalCount = 0 });

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" });

            result.Should().Be("Completed");
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Never);
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldCallRelateItem_ForEachRelatedId()
        {
            var relatedId1 = Guid.NewGuid();
            var relatedId2 = Guid.NewGuid();

            var sourceItem = BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems",
                new[] { relatedId1.ToString(), relatedId2.ToString() });

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [sourceItem], TotalCount = 1 });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" });

            result.Should().Be("Completed");
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldPassCorrectArgs_ToRelateItem()
        {
            var parentId   = Guid.NewGuid();
            var relatedId  = Guid.NewGuid();
            RelateArgs? capturedArgs = null;

            var sourceItem = BuildSdkItemWithRelatedIds(parentId, "RelatedField", new[] { relatedId.ToString() });

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [sourceItem], TotalCount = 1 });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Callback<RelateArgs>(a => capturedArgs = a)
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedField" });

            capturedArgs.Should().NotBeNull();
            capturedArgs!.Type.Should().Be("newsitems");
            capturedArgs.Id.Should().Be(parentId.ToString());
            capturedArgs.RelationName.Should().Be("RelatedField");
            capturedArgs.RelatedItemId.Should().Be(relatedId.ToString());
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldProcessMultipleFields_ForSameItem()
        {
            var parentId  = Guid.NewGuid();
            var related1  = Guid.NewGuid();
            var related2  = Guid.NewGuid();

            // Build item with both relationship fields populated via JSON deserialization
            var sourceItem = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(sourceItem, parentId.ToString());
            var jobj = new JObject
            {
                ["Id"]    = parentId.ToString(),
                ["FieldA"] = new JArray(related1.ToString()),
                ["FieldB"] = new JArray(related2.ToString())
            };
            SetDynamicFields(sourceItem, jobj);

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [sourceItem], TotalCount = 1 });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "FieldA", "FieldB" });

            result.Should().Be("Completed");
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_TestMode_ShouldProcessOnlyOneItem()
        {
            var item1 = BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { Guid.NewGuid().ToString() });
            var item2 = BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { Guid.NewGuid().ToString() });

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [item1, item2], TotalCount = 2 });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" }, testMode: true);

            result.Should().Be("Completed");
            // Test mode stops after first item, so only 1 RelateItem call
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Once);
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldContinue_WhenOneItemFails()
        {
            var relatedId1 = Guid.NewGuid();
            var relatedId2 = Guid.NewGuid();

            var item1 = BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { relatedId1.ToString() });
            var item2 = BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { relatedId2.ToString() });

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [item1, item2], TotalCount = 2 });

            var callCount = 0;
            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new HttpRequestException("relate failed");
                    return Task.CompletedTask;
                });

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            // Should not throw even if one item fails
            Func<Task> act = async () => await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" });

            await act.Should().NotThrowAsync();
            // Both items were attempted
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldFetchMultiplePages_WhenTotalCountExceedsBatchSize()
        {
            // Batch size is 50 — supply 51 items to force a second fetch
            var items1 = Enumerable.Range(0, 50).Select(_ =>
                BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { Guid.NewGuid().ToString() })).ToList();
            var items2 = new List<SdkItem>
            {
                BuildSdkItemWithRelatedIds(Guid.NewGuid(), "RelatedItems", new[] { Guid.NewGuid().ToString() })
            };

            var callCount = 0;
            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1
                        ? new CollectionResponse<SdkItem> { Items = items1, TotalCount = 51 }
                        : new CollectionResponse<SdkItem> { Items = items2, TotalCount = 51 };
                });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" });

            result.Should().Be("Completed");
            _mockSourceClient.Verify(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()), Times.Exactly(2));
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Exactly(51));
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldSkipField_WhenNoRelatedIdsFound()
        {
            var sourceItem = BuildSdkItemWithIdOnly(Guid.NewGuid());

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [sourceItem], TotalCount = 1 });

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedItems" });

            result.Should().Be("Completed");
            _mockTargetClient.Verify(c => c.RelateItem(It.IsAny<RelateArgs>()), Times.Never);
        }

        [Fact]
        public async Task BuildRelationshipsAsync_ShouldExtractId_FromStringFieldValue()
        {
            var parentId  = Guid.NewGuid();
            var relatedId = Guid.NewGuid();

            // Single GUID string directly as field value (not array)
            var sourceItem = BuildSdkItemWithStringId(parentId, "RelatedField", relatedId.ToString());

            _mockSourceClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [sourceItem], TotalCount = 1 });

            _mockTargetClient
                .Setup(c => c.RelateItem(It.IsAny<RelateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new RelationshipProcessor(_mockSourceClient.Object, _mockTargetClient.Object);

            var result = await processor.BuildRelationshipsAsync("newsitems", new[] { "RelatedField" });

            result.Should().Be("Completed");
            _mockTargetClient.Verify(c => c.RelateItem(It.Is<RelateArgs>(a =>
                a.RelatedItemId == relatedId.ToString())), Times.Once);
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an SdkItem via JSON deserialization so that GetValue&lt;T&gt; works correctly.
        /// The relationship field is stored as a JSON array of ID strings.
        /// </summary>
        private static void SetDynamicFields(SdkItem item, JObject fields)
        {
            var f = typeof(SdkItem).GetField("deserializedDynamicFields",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            f.SetValue(item, fields);
        }

        private static SdkItem BuildSdkItemWithRelatedIds(Guid itemId, string fieldName, IEnumerable<string> relatedIds)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, itemId.ToString());
            var jobj = new JObject { ["Id"] = itemId.ToString(), [fieldName] = new JArray(relatedIds.ToArray()) };
            SetDynamicFields(item, jobj);
            return item;
        }

        private static SdkItem BuildSdkItemWithStringId(Guid itemId, string fieldName, string relatedId)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, itemId.ToString());
            var jobj = new JObject { ["Id"] = itemId.ToString(), [fieldName] = relatedId };
            SetDynamicFields(item, jobj);
            return item;
        }

        private static SdkItem BuildSdkItemWithIdOnly(Guid itemId)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, itemId.ToString());
            var jobj = new JObject { ["Id"] = itemId.ToString() };
            SetDynamicFields(item, jobj);
            return item;
        }
    }
}
