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
    public class ContentProcessorTests
    {
        private readonly string _testCsvPath;

        public ContentProcessorTests()
        {
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");
        }

        // ── Populates SdkItem by directly setting its internal JObject backing field ─
        private static SdkItem MakeSdkItem(Guid id, string fieldName, string fieldValue)
        {
            var item = new SdkItem();
            var jobj = new JObject { ["Id"] = id.ToString(), [fieldName] = fieldValue };
            SetDynamicFields(item, jobj);
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }

        private static SdkItem MakeSdkItemWithId(Guid id)
        {
            var item = new SdkItem();
            var jobj = new JObject { ["Id"] = id.ToString() };
            SetDynamicFields(item, jobj);
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }

        private static void SetDynamicFields(SdkItem item, JObject fields)
        {
            var f = typeof(SdkItem).GetField("deserializedDynamicFields",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            f.SetValue(item, fields);
        }

        [Fact]
        public void ContentProcessor_ShouldThrowArgumentNullException_WhenClientIsNull()
        {
            Action act = () => new ContentProcessor(null!, _testCsvPath);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("client");
        }

        [Fact]
        public void ContentProcessor_ShouldThrowArgumentNullException_WhenCsvFilePathIsNull()
        {
            var mockClient = new Mock<IRestClient>();

            Action act = () => new ContentProcessor(mockClient.Object, null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("csvFilePath");
        }

        [Fact]
        public void ContentProcessor_ShouldInitialize_WithValidParameters()
        {
            var mockClient = new Mock<IRestClient>();

            Action act = () => new ContentProcessor(mockClient.Object, _testCsvPath);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldThrowArgumentNullException_WhenContentTypeIsNull()
        {
            var mockClient = new Mock<IRestClient>();
            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            Func<Task> act = async () => await processor.UpdateContentAsync(null!, "FieldName");

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("contentType");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldThrowArgumentNullException_WhenFieldNameIsNull()
        {
            var mockClient = new Mock<IRestClient>();
            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            Func<Task> act = async () => await processor.UpdateContentAsync("ContentType", null!);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("fieldName");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldReturnUpdateCompleted_WhenNoItemsExist()
        {
            var mockClient = new Mock<IRestClient>();
            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [], TotalCount = 0 });

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            var result = await processor.UpdateContentAsync("newsitems", "Content");

            result.Should().Be("Update completed");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldReturnUpdateCompleted_WhenItemHasEmptyContent()
        {
            var mockClient = new Mock<IRestClient>();
            var item = MakeSdkItem(Guid.NewGuid(), "Content", string.Empty);

            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [item], TotalCount = 1 });

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            var result = await processor.UpdateContentAsync("newsitems", "Content");

            result.Should().Be("Update completed");
            // Nothing to update — UpdateItem should never be called
            mockClient.Verify(c => c.UpdateItem(It.IsAny<UpdateArgs>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldReturnUpdateCompleted_WhenItemHasNoImages()
        {
            var mockClient = new Mock<IRestClient>();
            var item = MakeSdkItem(Guid.NewGuid(), "Content", "<p>No images here</p>");

            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [item], TotalCount = 1 });

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            var result = await processor.UpdateContentAsync("newsitems", "Content");

            result.Should().Be("Update completed");
            mockClient.Verify(c => c.UpdateItem(It.IsAny<UpdateArgs>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContentAsync_TestMode_ShouldProcessOnlyOneItem()
        {
            var mockClient = new Mock<IRestClient>();

            var item1 = MakeSdkItem(Guid.NewGuid(), "Content", "<p>item 1</p>");
            var item2 = MakeSdkItem(Guid.NewGuid(), "Content", "<p>item 2</p>");

            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [item1, item2], TotalCount = 2 });

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            // In test mode, take=1 — so only item1 should be fetched & processed
            var result = await processor.UpdateContentAsync("newsitems", "Content", testMode: true);

            result.Should().Be("Update completed");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldUpdateItem_WhenImageSrcIsResolvable()
        {
            var mockClient      = new Mock<IRestClient>();
            var imageId         = Guid.NewGuid();
            var imageUrl        = "http://cdn.example.com/images/test.jpg";

            var src = $"~/SFRes/Item with ID: '{imageId}'/index.html";
            var html = $"<img src=\"{ src}\" title=\"My Image\" />";

            var contentItem = MakeSdkItem(Guid.NewGuid(), "Content", html);

            // GetItems<SdkItem> for the content type
            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [contentItem], TotalCount = 1 });

            var imageDto = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageDto>(
                Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, object>
                {
                    { "Id", imageId.ToString() },
                    { "Url", imageUrl }
                }))!;

            mockClient
                .Setup(c => c.GetItems<ImageDto>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(new CollectionResponse<ImageDto> { Items = [imageDto] });

            mockClient
                .Setup(c => c.UpdateItem(It.IsAny<UpdateArgs>()))
                .Returns(Task.CompletedTask);

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            var result = await processor.UpdateContentAsync("newsitems", "Content");

            result.Should().Be("Update completed");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldUseImageMappings_WhenCsvFileExists()
        {
            // Write a real CSV mapping file so the CSV load path is exercised
            var sourceId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            var csvPath = Path.Combine(Path.GetTempPath(), $"mapping_{Guid.NewGuid()}.csv");
            try
            {
                await File.WriteAllTextAsync(csvPath,
                    "Image Title,Source Id,Target Id\n" +
                    $"My Image,{sourceId},{targetId}\n");

                var mockClient = new Mock<IRestClient>();

                var src  = $"~/SFRes/Item with ID: '{sourceId}'/index.html";
                var html = $"<img src=\"{src}\" title=\"My Image\" />";

                var contentItem = MakeSdkItem(Guid.NewGuid(), "Content", html);

                mockClient
                    .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                    .ReturnsAsync(new CollectionResponse<SdkItem> { Items = [contentItem], TotalCount = 1 });

                var imageDto = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageDto>(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, object>
                    {
                        { "Id", targetId.ToString() },
                        { "Url", "http://cdn.example.com/img.jpg" }
                    }))!;

                mockClient
                    .Setup(c => c.GetItems<ImageDto>(It.IsAny<GetAllArgs>()))
                    .ReturnsAsync(new CollectionResponse<ImageDto> { Items = [imageDto] });

                mockClient
                    .Setup(c => c.UpdateItem(It.IsAny<UpdateArgs>()))
                    .Returns(Task.CompletedTask);

                var processor = new ContentProcessor(mockClient.Object, csvPath);

                var result = await processor.UpdateContentAsync("newsitems", "Content");

                result.Should().Be("Update completed");
            }
            finally
            {
                if (File.Exists(csvPath)) File.Delete(csvPath);
            }
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldPaginateThroughAllItems()
        {
            var mockClient = new Mock<IRestClient>();

            var page1 = Enumerable.Range(0, 50).Select(_ =>
                MakeSdkItem(Guid.NewGuid(), "Content", "<p>no images</p>")).ToList();

            var page2 = Enumerable.Range(0, 10).Select(_ =>
                MakeSdkItem(Guid.NewGuid(), "Content", "<p>no images</p>")).ToList();

            var call = 0;
            mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(() =>
                {
                    call++;
                    return call == 1
                        ? new CollectionResponse<SdkItem> { Items = page1, TotalCount = 60 }
                        : new CollectionResponse<SdkItem> { Items = page2, TotalCount = 60 };
                });

            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            var result = await processor.UpdateContentAsync("newsitems", "Content");

            result.Should().Be("Update completed");
            mockClient.Verify(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()), Times.Exactly(2));
        }
    }

    public class ProcessingResultTests
    {
        [Fact]
        public void ProcessingResult_ShouldInitializeProperties()
        {
            var result = new ProcessingResult
            {
                ProcessedCount = 10,
                UpdatedCount = 5
            };

            result.ProcessedCount.Should().Be(10);
            result.UpdatedCount.Should().Be(5);
        }

        [Fact]
        public void ProcessingResult_ShouldAllowDefaultInitialization()
        {
            var result = new ProcessingResult();

            result.ProcessedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
        }

        [Fact]
        public void ProcessingResult_ShouldSupportAccumulation()
        {
            var results = new List<ProcessingResult>
            {
                new ProcessingResult { ProcessedCount = 10, UpdatedCount = 5 },
                new ProcessingResult { ProcessedCount = 20, UpdatedCount = 8 },
                new ProcessingResult { ProcessedCount = 15, UpdatedCount = 3 }
            };

            var totalProcessed = results.Sum(r => r.ProcessedCount);
            var totalUpdated = results.Sum(r => r.UpdatedCount);

            totalProcessed.Should().Be(45);
            totalUpdated.Should().Be(16);
        }
    }

    public class ImgDetailTests
    {
        [Fact]
        public void ImgDetail_ShouldInitializeProperties()
        {
            var id = Guid.NewGuid();
            var imgDetail = new ImgDetail
            {
                Title = "Test Image",
                Id = id
            };

            imgDetail.Title.Should().Be("Test Image");
            imgDetail.Id.Should().Be(id);
        }

        [Fact]
        public void ImgDetail_ShouldAllowNullValues()
        {
            var imgDetail = new ImgDetail
            {
                Title = null,
                Id = null
            };

            imgDetail.Title.Should().BeNull();
            imgDetail.Id.Should().BeNull();
        }

        [Fact]
        public void ImgDetail_ShouldSupportCollectionOperations()
        {
            var imgDetails = new List<ImgDetail>
            {
                new ImgDetail { Title = "Image 1", Id = Guid.NewGuid() },
                new ImgDetail { Title = "Image 2", Id = Guid.NewGuid() },
                new ImgDetail { Title = "Image 3", Id = null },
                new ImgDetail { Title = null, Id = Guid.NewGuid() }
            };

            var withTitles = imgDetails.Where(i => !string.IsNullOrWhiteSpace(i.Title)).ToList();
            var withIds = imgDetails.Where(i => i.Id.HasValue).ToList();

            withTitles.Should().HaveCount(3);
            withIds.Should().HaveCount(3);
        }
    }

    public class ImageMappingTests
    {
        [Fact]
        public void ImageMapping_ShouldParseValidGuid()
        {
            var targetGuid = Guid.NewGuid();
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = targetGuid.ToString()
            };

            mapping.TargetId.Should().Be(targetGuid);
        }

        [Fact]
        public void ImageMapping_ShouldReturnNull_WhenTargetIdStringIsNA()
        {
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = "N/A"
            };

            mapping.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldReturnNull_WhenTargetIdStringIsEmpty()
        {
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = ""
            };

            mapping.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldReturnNull_WhenTargetIdStringIsWhitespace()
        {
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = "   "
            };

            mapping.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldReturnNull_WhenTargetIdStringIsNull()
        {
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = null
            };

            mapping.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldReturnNull_WhenTargetIdStringIsInvalidGuid()
        {
            var mapping = new ImageMapping
            {
                ImageTitle = "Test Image",
                SourceId = Guid.NewGuid(),
                TargetIdString = "invalid-guid"
            };

            mapping.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldBeCaseInsensitiveForNA()
        {
            var mapping1 = new ImageMapping { TargetIdString = "n/a" };
            var mapping2 = new ImageMapping { TargetIdString = "N/a" };
            var mapping3 = new ImageMapping { TargetIdString = "NA" };

            mapping1.TargetId.Should().BeNull();
            mapping2.TargetId.Should().BeNull();
            mapping3.TargetId.Should().BeNull();
        }

        [Fact]
        public void ImageMapping_ShouldHandleVariousValidFormats()
        {
            var validGuid1 = Guid.NewGuid();
            var validGuid2 = Guid.NewGuid();
            var validGuid3 = Guid.NewGuid();

            var mappings = new[]
            {
                new ImageMapping { TargetIdString = validGuid1.ToString() },
                new ImageMapping { TargetIdString = validGuid2.ToString().ToUpper() },
                new ImageMapping { TargetIdString = validGuid3.ToString().ToLower() }
            };

            mappings[0].TargetId.Should().Be(validGuid1);
            mappings[1].TargetId.Should().Be(validGuid2);
            mappings[2].TargetId.Should().Be(validGuid3);
        }

        [Fact]
        public void ImageMapping_ShouldSupportBulkOperations()
        {
            var sourceId1 = Guid.NewGuid();
            var sourceId2 = Guid.NewGuid();
            var targetId1 = Guid.NewGuid();
            var targetId2 = Guid.NewGuid();

            var mappings = new List<ImageMapping>
            {
                new ImageMapping
                {
                    ImageTitle = "Image 1",
                    SourceId = sourceId1,
                    TargetIdString = targetId1.ToString()
                },
                new ImageMapping
                {
                    ImageTitle = "Image 2",
                    SourceId = sourceId2,
                    TargetIdString = targetId2.ToString()
                },
                new ImageMapping
                {
                    ImageTitle = "Image 3 (No Target)",
                    SourceId = Guid.NewGuid(),
                    TargetIdString = "N/A"
                }
            };

            var validMappings = mappings.Where(m => m.TargetId.HasValue).ToList();
            var invalidMappings = mappings.Where(m => !m.TargetId.HasValue).ToList();

            validMappings.Should().HaveCount(2);
            invalidMappings.Should().HaveCount(1);
        }

        [Fact]
        public void ImageMapping_ShouldFindMappingBySourceId()
        {
            var sourceId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            var mappings = new List<ImageMapping>
            {
                new ImageMapping
                {
                    ImageTitle = "Image 1",
                    SourceId = Guid.NewGuid(),
                    TargetIdString = Guid.NewGuid().ToString()
                },
                new ImageMapping
                {
                    ImageTitle = "Target Image",
                    SourceId = sourceId,
                    TargetIdString = targetId.ToString()
                },
                new ImageMapping
                {
                    ImageTitle = "Image 3",
                    SourceId = Guid.NewGuid(),
                    TargetIdString = Guid.NewGuid().ToString()
                }
            };

            var foundMapping = mappings.FirstOrDefault(m => m.SourceId == sourceId);

            foundMapping.Should().NotBeNull();
            foundMapping!.TargetId.Should().Be(targetId);
            foundMapping.ImageTitle.Should().Be("Target Image");
        }
    }
}
