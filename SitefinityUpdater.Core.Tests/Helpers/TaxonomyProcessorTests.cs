using FluentAssertions;
using Moq;
using Progress.Sitefinity.Clients.Taxa;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Client;
using Progress.Sitefinity.RestSdk.Dto;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class TaxonomyProcessorTests
    {
        private readonly Mock<IRestClient> _mockClient;
        private readonly Mock<ITaxaClient> _mockTaxaClient;

        public TaxonomyProcessorTests()
        {
            _mockClient = new Mock<IRestClient>();
            _mockTaxaClient = new Mock<ITaxaClient>();
        }

        // ── constructor ──────────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_ShouldThrow_WhenClientIsNull()
        {
            Action act = () => new TaxonomyProcessor(null!, _mockTaxaClient.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("client");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenTaxaClientIsNull()
        {
            Action act = () => new TaxonomyProcessor(_mockClient.Object, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("taxaClient");
        }

        [Fact]
        public void Constructor_ShouldInitialize_WithValidParameters()
        {
            Action act = () => new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);
            act.Should().NotThrow();
        }

        // ── ResolveOrCreateTaxaAsync argument validation ─────────────────────────────

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldThrow_WhenTaxonomyNameIsNull()
        {
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            Func<Task> act = async () => await processor.ResolveOrCreateTaxaAsync(null!, ["Tag A"]);

            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("taxonomyName");
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldReturnEmpty_WhenTaxonTitlesIsEmpty()
        {
            SetupEmptyTaxonomy("Tags");
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", []);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldReturnEmpty_WhenTaxonTitlesIsNull()
        {
            SetupEmptyTaxonomy("Tags");
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", null!);

            result.Should().BeEmpty();
        }

        // ── resolve from existing ────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldReturnExistingId_WhenTaxonAlreadyExists()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "C#", Id = existingId.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["C#"]);

            result.Should().ContainSingle().Which.Should().Be(existingId);
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldBeCaseInsensitive_WhenResolvingExistingTaxon()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "Technology", Id = existingId.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["technology"]);

            result.Should().ContainSingle().Which.Should().Be(existingId);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldReturnMultipleIds_WhenMultipleTaxaExist()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags",
            [
                new TaxonDto { Title = "Alpha", Id = id1.ToString() },
                new TaxonDto { Title = "Beta",  Id = id2.ToString() }
            ]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["Alpha", "Beta"]);

            result.Should().HaveCount(2).And.Contain([id1, id2]);
        }

        // ── create missing ───────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldCreateTaxon_WhenTaxonDoesNotExist()
        {
            var newId = Guid.NewGuid();
            SetupEmptyTaxonomy("Tags");
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.Is<TaxonDto>(d => d.Title == "NewTag"), "Tags"))
                .ReturnsAsync(new TaxonDto { Title = "NewTag", Id = newId.ToString() });

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["NewTag"]);

            result.Should().ContainSingle().Which.Should().Be(newId);
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldNotCreateTaxon_WhenCalledTwiceForSameTitle()
        {
            var newId = Guid.NewGuid();
            SetupEmptyTaxonomy("Tags");
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"))
                .ReturnsAsync(new TaxonDto { Title = "NewTag", Id = newId.ToString() });

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.ResolveOrCreateTaxaAsync("Tags", ["NewTag"]);
            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["NewTag"]);

            result.Should().ContainSingle().Which.Should().Be(newId);
            // Taxonomy was fetched once; CreateTaxon was only called once (second call hits cache)
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldOnlyFetchTaxonomy_Once_AcrossMultipleCalls()
        {
            var id1 = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "Existing", Id = id1.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.ResolveOrCreateTaxaAsync("Tags", ["Existing"]);
            await processor.ResolveOrCreateTaxaAsync("Tags", ["Existing"]);

            // GetItems should only be called once for the taxonomy load
            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.IsAny<GetAllArgs>()), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldThrow_WhenCreateTaxonReturnsNull()
        {
            SetupEmptyTaxonomy("Tags");
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"))
                .ReturnsAsync((TaxonDto)null!);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            Func<Task> act = async () => await processor.ResolveOrCreateTaxaAsync("Tags", ["MissingTag"]);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*null or empty ID*");
        }

        // ── helper setup methods ─────────────────────────────────────────────────────

        private void SetupEmptyTaxonomy(string taxonomyName)
        {
            SetupTaxonomyWithTaxa(taxonomyName, []);
        }

        private void SetupTaxonomyWithTaxa(string taxonomyName, List<TaxonDto> taxa)
        {
            var response = new CollectionResponse<TaxonDto>
            {
                Items = taxa,
                TotalCount = taxa.Count
            };

            _mockClient
                .Setup(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == taxonomyName)))
                .ReturnsAsync(response);
        }

        // ── PreWarmAsync ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task PreWarmAsync_ShouldDoNothing_WhenNamesIsEmpty()
        {
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync([]);

            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.IsAny<GetAllArgs>()), Times.Never);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldDoNothing_WhenNamesIsNull()
        {
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync(null!);

            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.IsAny<GetAllArgs>()), Times.Never);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldLoadEachTaxonomy_Once()
        {
            SetupEmptyTaxonomy("Tags");
            SetupEmptyTaxonomy("Categories");

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync(["Tags", "Categories"]);

            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == "Tags")), Times.Once);
            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == "Categories")), Times.Once);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldDeduplicate_CaseInsensitive()
        {
            SetupEmptyTaxonomy("Tags");

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync(["Tags", "tags", "TAGS"]);

            // Even though the same name appeared three times, only one fetch should occur
            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a =>
                a.Type.Equals("Tags", StringComparison.OrdinalIgnoreCase))), Times.Once);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldPopulateCache_SoSubsequentResolveHitsNoNetwork()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "Technology", Id = existingId.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync(["Tags"]);

            // Resolve should hit the cache — no additional GetItems call
            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["Technology"]);

            result.Should().ContainSingle().Which.Should().Be(existingId);
            // GetItems called exactly once (during PreWarm), not again during resolve
            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.IsAny<GetAllArgs>()), Times.Once);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldNotRefetch_WhenTaxonomyAlreadyInCache()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "Technology", Id = existingId.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            // First call loads it
            await processor.PreWarmAsync(["Tags"]);
            // Second call should be a no-op for Tags
            await processor.PreWarmAsync(["Tags"]);

            _mockClient.Verify(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == "Tags")), Times.Once);
        }

        // ── TaxonomyProcessor error paths ────────────────────────────────────────────

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldFilterOutBlankTitles()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "RealTag", Id = existingId.ToString() }]);
            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            // " " and "" should be stripped, only "RealTag" resolved
            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["RealTag", " ", ""]);

            result.Should().ContainSingle().Which.Should().Be(existingId);
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldThrow_WhenCreateTaxonReturnsNonGuidId()
        {
            SetupEmptyTaxonomy("Tags");
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"))
                .ReturnsAsync(new TaxonDto { Title = "NewTag", Id = "not-a-guid" });

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            Func<Task> act = async () => await processor.ResolveOrCreateTaxaAsync("Tags", ["NewTag"]);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*non-GUID ID*");
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldThrow_WhenCreateTaxonThrows()
        {
            SetupEmptyTaxonomy("Tags");
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "Tags"))
                .ThrowsAsync(new HttpRequestException("network error"));

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            Func<Task> act = async () => await processor.ResolveOrCreateTaxaAsync("Tags", ["NewTag"]);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Failed to create taxon*network error*");
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldSkipTaxonsWithInvalidIdDuringLoad()
        {
            // Items with blank title or non-GUID ID should be silently skipped during page processing
            var validId = Guid.NewGuid();
            var response = new CollectionResponse<TaxonDto>
            {
                Items =
                [
                    new TaxonDto { Title = "Valid", Id = validId.ToString() },
                    new TaxonDto { Title = "",      Id = Guid.NewGuid().ToString() },  // blank title → skipped
                    new TaxonDto { Title = "Bad",   Id = "not-a-guid" },               // invalid ID  → skipped
                    new TaxonDto { Title = null,    Id = Guid.NewGuid().ToString() }   // null title  → skipped
                ],
                TotalCount = 4
            };
            _mockClient
                .Setup(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == "Tags")))
                .ReturnsAsync(response);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            // "Valid" is cached; "Bad" is not → must be created
            var newId = Guid.NewGuid();
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.Is<TaxonDto>(d => d.Title == "Bad"), "Tags"))
                .ReturnsAsync(new TaxonDto { Title = "Bad", Id = newId.ToString() });

            var resultValid = await processor.ResolveOrCreateTaxaAsync("Tags", ["Valid"]);
            resultValid.Should().ContainSingle().Which.Should().Be(validId);

            var resultBad = await processor.ResolveOrCreateTaxaAsync("Tags", ["Bad"]);
            resultBad.Should().ContainSingle().Which.Should().Be(newId);
        }

        [Fact]
        public async Task LoadTaxonomyAsync_ShouldContinueGracefully_WhenGetItemsThrows()
        {
            // When GetItems throws, the cache entry should still be created (empty) and no exception propagated
            _mockClient
                .Setup(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a => a.Type == "BrokenTaxonomy")))
                .ThrowsAsync(new HttpRequestException("connection refused"));

            // CreateTaxon is called for any title because the cache is empty
            var newId = Guid.NewGuid();
            _mockTaxaClient
                .Setup(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), "BrokenTaxonomy"))
                .ReturnsAsync(new TaxonDto { Title = "AnyTag", Id = newId.ToString() });

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            Func<Task> act = async () => await processor.ResolveOrCreateTaxaAsync("BrokenTaxonomy", ["AnyTag"]);

            // Should NOT propagate the HttpRequestException from GetItems
            await act.Should().NotThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldPreserveOrder_OfResolvedIds()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags",
            [
                new TaxonDto { Title = "First",  Id = id1.ToString() },
                new TaxonDto { Title = "Second", Id = id2.ToString() },
                new TaxonDto { Title = "Third",  Id = id3.ToString() }
            ]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["Third", "First", "Second"]);

            result.Should().HaveCount(3);
            result[0].Should().Be(id3);
            result[1].Should().Be(id1);
            result[2].Should().Be(id2);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldTrimTitles_BeforeResolving()
        {
            var existingId = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags", [new TaxonDto { Title = "C#", Id = existingId.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            // "  C#  " should be trimmed to "C#" and resolved from cache
            var result = await processor.ResolveOrCreateTaxaAsync("Tags", ["  C#  "]);

            result.Should().ContainSingle().Which.Should().Be(existingId);
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveOrCreateTaxaAsync_ShouldResolveAcrossMultipleTaxonomies()
        {
            var tagId1  = Guid.NewGuid();
            var catId1  = Guid.NewGuid();
            SetupTaxonomyWithTaxa("Tags",       [new TaxonDto { Title = "Tag1",      Id = tagId1.ToString() }]);
            SetupTaxonomyWithTaxa("Categories", [new TaxonDto { Title = "Category1", Id = catId1.ToString() }]);

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            var tagResult = await processor.ResolveOrCreateTaxaAsync("Tags",       ["Tag1"]);
            var catResult = await processor.ResolveOrCreateTaxaAsync("Categories", ["Category1"]);

            tagResult.Should().ContainSingle().Which.Should().Be(tagId1);
            catResult.Should().ContainSingle().Which.Should().Be(catId1);
        }

        [Fact]
        public async Task PreWarmAsync_ShouldHandleMultiplePages()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();

            // Page 1 (skip=0, take=200, count=true): TotalCount=201 forces a second page
            _mockClient
                .Setup(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a =>
                    a.Type == "Tags" && a.Skip == 0 && a.Count == true)))
                .ReturnsAsync(new CollectionResponse<TaxonDto>
                {
                    Items = [new TaxonDto { Title = "A", Id = id1.ToString() },
                             new TaxonDto { Title = "B", Id = id2.ToString() }],
                    TotalCount = 201
                });

            // Page 2 (skip=200, take=200, count=false): remaining items
            _mockClient
                .Setup(c => c.GetItems<TaxonDto>(It.Is<GetAllArgs>(a =>
                    a.Type == "Tags" && a.Skip == 200 && a.Count == false)))
                .ReturnsAsync(new CollectionResponse<TaxonDto>
                {
                    Items = [new TaxonDto { Title = "C", Id = id3.ToString() },
                             new TaxonDto { Title = "D", Id = id4.ToString() }],
                    TotalCount = null
                });

            var processor = new TaxonomyProcessor(_mockClient.Object, _mockTaxaClient.Object);

            await processor.PreWarmAsync(["Tags"]);

            // All four titles should resolve from cache without any CreateTaxon call
            var resultA = await processor.ResolveOrCreateTaxaAsync("Tags", ["A"]);
            var resultD = await processor.ResolveOrCreateTaxaAsync("Tags", ["D"]);

            resultA.Should().ContainSingle().Which.Should().Be(id1);
            resultD.Should().ContainSingle().Which.Should().Be(id4);
            _mockTaxaClient.Verify(t => t.CreateTaxon<TaxonDto>(It.IsAny<TaxonDto>(), It.IsAny<string>()), Times.Never);
        }
    }
}

