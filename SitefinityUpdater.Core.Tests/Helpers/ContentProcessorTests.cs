using FluentAssertions;
using Moq;
using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class ContentProcessorTests
    {
        private readonly string _testCsvPath;

        public ContentProcessorTests()
        {
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");
        }

        [Fact]
        public void ContentProcessor_ShouldThrowArgumentNullException_WhenClientIsNull()
        {
            Action act = () => new ContentProcessor(null, _testCsvPath);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("client");
        }

        [Fact]
        public void ContentProcessor_ShouldThrowArgumentNullException_WhenCsvFilePathIsNull()
        {
            var mockClient = new Mock<IRestClient>();

            Action act = () => new ContentProcessor(mockClient.Object, null);

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

            Func<Task> act = async () => await processor.UpdateContentAsync(null, "FieldName");

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("contentType");
        }

        [Fact]
        public async Task UpdateContentAsync_ShouldThrowArgumentNullException_WhenFieldNameIsNull()
        {
            var mockClient = new Mock<IRestClient>();
            var processor = new ContentProcessor(mockClient.Object, _testCsvPath);

            Func<Task> act = async () => await processor.UpdateContentAsync("ContentType", null);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("fieldName");
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
            foundMapping.TargetId.Should().Be(targetId);
            foundMapping.ImageTitle.Should().Be("Target Image");
        }
    }
}
