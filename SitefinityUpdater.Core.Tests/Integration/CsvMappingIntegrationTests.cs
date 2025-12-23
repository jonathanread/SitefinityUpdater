using FluentAssertions;
using SitefinityContentUpdater.Core.Helpers;
using System.Text;

namespace SitefinityContentUpdater.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for CSV file handling and image mapping functionality.
    /// </summary>
    public class CsvMappingIntegrationTests
    {
        private readonly string _testCsvDirectory;

        public CsvMappingIntegrationTests()
        {
            _testCsvDirectory = Path.Combine(Path.GetTempPath(), $"SitefinityTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testCsvDirectory);
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
        public void ImageMapping_ShouldHandleVariousInvalidFormats()
        {
            var invalidMappings = new[]
            {
                new ImageMapping { TargetIdString = "N/A" },
                new ImageMapping { TargetIdString = "n/a" },
                new ImageMapping { TargetIdString = "" },
                new ImageMapping { TargetIdString = "   " },
                new ImageMapping { TargetIdString = null },
                new ImageMapping { TargetIdString = "invalid-guid" },
                new ImageMapping { TargetIdString = "12345" }
            };

            foreach (var mapping in invalidMappings)
            {
                mapping.TargetId.Should().BeNull($"because '{mapping.TargetIdString}' is not a valid GUID");
            }
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

        [Fact]
        public void ProcessingResult_ShouldAccumulateCorrectly()
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

        public void Dispose()
        {
            if (Directory.Exists(_testCsvDirectory))
            {
                try
                {
                    Directory.Delete(_testCsvDirectory, true);
                }
                catch
                {
                }
            }
        }
    }
}
