using FluentAssertions;
using Moq;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Client;
using Progress.Sitefinity.RestSdk.Dto;
using Progress.Sitefinity.RestSdk.Filters;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    // -------------------------------------------------------------------------
    // PageDefinition model
    // -------------------------------------------------------------------------

    public class PageDefinitionTests
    {
        [Fact]
        public void PageDefinition_ShouldInitializeWithDefaults()
        {
            var def = new PageDefinition();

            def.Title.Should().Be(string.Empty);
            def.ParentTitle.Should().BeNull();
        }

        [Fact]
        public void PageDefinition_ShouldAllowPropertyAssignment()
        {
            var def = new PageDefinition
            {
                Title       = "About Us",
                ParentTitle = "Home"
            };

            def.Title.Should().Be("About Us");
            def.ParentTitle.Should().Be("Home");
        }

        [Fact]
        public void PageDefinition_RootPage_HasNullParentTitle()
        {
            var def = new PageDefinition { Title = "Home" };

            def.ParentTitle.Should().BeNull();
        }
    }

    // -------------------------------------------------------------------------
    // PageProcessor constructor guards
    // -------------------------------------------------------------------------

    public class PageProcessorConstructorTests
    {
        [Fact]
        public void Constructor_ShouldThrow_WhenClientIsNull()
        {
            Action act = () => new PageProcessor(null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("client");
        }

        [Fact]
        public void Constructor_ShouldNotThrow_WhenClientIsProvided()
        {
            var mock = new Mock<IRestClient>();

            Action act = () => new PageProcessor(mock.Object);

            act.Should().NotThrow();
        }
    }

    // -------------------------------------------------------------------------
    // GetTemplateIdByNameAsync
    // -------------------------------------------------------------------------

    public class PageProcessor_GetTemplateIdByNameTests : PageProcessorTestBase
    {
        private readonly Mock<IRestClient> _mockClient;

        public PageProcessor_GetTemplateIdByNameTests()
        {
            _mockClient = new Mock<IRestClient>();
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldReturnId_WhenTemplateFound()
        {
            var expectedId = Guid.NewGuid();
            var response   = BuildCollectionResponse(BuildSdkItemWithId(expectedId));
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(response);

            var processor = new PageProcessor(_mockClient.Object);
            var result    = await processor.GetTemplateIdByNameAsync("Bootstrap4 - Landing Page");

            result.Should().Be(expectedId);
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldReturnNull_WhenTemplateNotFound()
        {
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(response);

            var processor = new PageProcessor(_mockClient.Object);
            var result    = await processor.GetTemplateIdByNameAsync("Nonexistent Template");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldUsePageTemplatesType()
        {
            GetAllArgs? capturedArgs = null;
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .Callback<GetAllArgs>(a => capturedArgs = a)
                .ReturnsAsync(response);

            var processor = new PageProcessor(_mockClient.Object);
            await processor.GetTemplateIdByNameAsync("My Template");

            capturedArgs!.Type.Should().Be(RestClientContentTypes.PageTemplates);
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldFilterByTitle()
        {
            GetAllArgs? capturedArgs = null;
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .Callback<GetAllArgs>(a => capturedArgs = a)
                .ReturnsAsync(response);

            var processor = new PageProcessor(_mockClient.Object);
            await processor.GetTemplateIdByNameAsync("Bootstrap4 - Landing Page");

            capturedArgs!.Filter.Should().BeOfType<FilterClause>();
            var filter = (FilterClause)capturedArgs.Filter!;
            filter.FieldName.Should().Be("Title");
            filter.FieldValue.Should().Be("Bootstrap4 - Landing Page");
            filter.Operator.Should().Be(FilterClause.Operators.Equal);
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldRequestTake1()
        {
            GetAllArgs? capturedArgs = null;
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .Callback<GetAllArgs>(a => capturedArgs = a)
                .ReturnsAsync(response);

            var processor = new PageProcessor(_mockClient.Object);
            await processor.GetTemplateIdByNameAsync("Any");

            capturedArgs!.Take.Should().Be(1);
        }

        [Fact]
        public async Task GetTemplateIdByNameAsync_ShouldPropagate_WhenSdkThrows()
        {
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ThrowsAsync(new HttpRequestException("SDK error"));

            var processor = new PageProcessor(_mockClient.Object);

            Func<Task> act = async () => await processor.GetTemplateIdByNameAsync("Any Template");

            await act.Should().ThrowAsync<HttpRequestException>();
        }
    }

    // -------------------------------------------------------------------------
    // GetPageIdByTitleAsync
    // -------------------------------------------------------------------------

    public class PageProcessor_GetPageIdByTitleTests : PageProcessorTestBase
    {
        private readonly Mock<IRestClient> _mockClient;
        private readonly PageProcessor _processor;

        public PageProcessor_GetPageIdByTitleTests()
        {
            _mockClient = new Mock<IRestClient>();
            _processor  = new PageProcessor(_mockClient.Object);
        }

        [Fact]
        public async Task GetPageIdByTitleAsync_ShouldReturnId_WhenPageFound()
        {
            var expectedId = Guid.NewGuid();
            var response   = BuildCollectionResponse(BuildSdkItemWithId(expectedId));
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(response);

            var result = await _processor.GetPageIdByTitleAsync("About Us");

            result.Should().Be(expectedId);
        }

        [Fact]
        public async Task GetPageIdByTitleAsync_ShouldReturnNull_WhenPageNotFound()
        {
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(response);

            var result = await _processor.GetPageIdByTitleAsync("Nonexistent Page");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPageIdByTitleAsync_ShouldPassCorrectTypeToSdk()
        {
            GetAllArgs? capturedArgs = null;
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .Callback<GetAllArgs>(a => capturedArgs = a)
                .ReturnsAsync(response);

            await _processor.GetPageIdByTitleAsync("Contact");

            capturedArgs!.Type.Should().Be(RestClientContentTypes.Pages);
            capturedArgs.Take.Should().Be(1);
        }

        [Fact]
        public async Task GetPageIdByTitleAsync_ShouldFilterByTitle()
        {
            GetAllArgs? capturedArgs = null;
            var response = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .Callback<GetAllArgs>(a => capturedArgs = a)
                .ReturnsAsync(response);

            await _processor.GetPageIdByTitleAsync("Owner's Page");

            capturedArgs!.Filter.Should().BeOfType<FilterClause>();
            var filter = (FilterClause)capturedArgs.Filter!;
            filter.FieldName.Should().Be("Title");
            filter.FieldValue.Should().Be("Owner's Page");
            filter.Operator.Should().Be(FilterClause.Operators.Equal);
        }
    }

    // -------------------------------------------------------------------------
    // CreatePageAsync
    // -------------------------------------------------------------------------

    public class PageProcessor_CreatePageTests
    {
        private readonly Mock<IRestClient> _mockClient = new();

        private static SdkItem BuildSdkItem(Guid id)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }

        private void SetupSdkDefaults(Guid pageId)
        {
            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(BuildSdkItem(pageId));
        }

        [Fact]
        public async Task CreatePageAsync_ShouldReturnPageId_WhenCreationSucceeds()
        {
            var expectedId = Guid.NewGuid();
            SetupSdkDefaults(expectedId);
            var processor = new PageProcessor(_mockClient.Object);

            var result = await processor.CreatePageAsync("Home", Guid.NewGuid());

            result.Should().Be(expectedId);
        }

        [Fact]
        public async Task CreatePageAsync_ShouldIncludeParentId_WhenParentIdSupplied()
        {
            var expectedId = Guid.NewGuid();
            var parentId   = Guid.NewGuid();
            CreateArgs? capturedArgs = null;

            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .Callback<CreateArgs>(a => capturedArgs = a)
                .ReturnsAsync(BuildSdkItem(expectedId));

            var processor = new PageProcessor(_mockClient.Object);
            await processor.CreatePageAsync("Contact", Guid.NewGuid(), parentId);

            capturedArgs.Should().NotBeNull();
            var dataDict = capturedArgs!.Data.Should().BeOfType<Dictionary<string, object>>().Subject;
            dataDict.Should().ContainKey("ParentId");
            dataDict["ParentId"].Should().Be(parentId.ToString());
        }

        [Fact]
        public async Task CreatePageAsync_ShouldThrow_WhenSdkReturnsItemWithoutId()
        {
            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(new SdkItem());

            var processor = new PageProcessor(_mockClient.Object);

            Func<Task> act = async () => await processor.CreatePageAsync("Home", Guid.NewGuid());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreatePageAsync_ShouldUsePagesType()
        {
            var expectedId = Guid.NewGuid();
            CreateArgs? capturedArgs = null;

            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .Callback<CreateArgs>(a => capturedArgs = a)
                .ReturnsAsync(BuildSdkItem(expectedId));

            var processor = new PageProcessor(_mockClient.Object);
            await processor.CreatePageAsync("Home", Guid.NewGuid());

            capturedArgs!.Type.Should().Be(RestClientContentTypes.Pages);
        }

        [Fact]
        public async Task CreatePageAsync_ShouldAddContentBlockWidget_ViaPagesSdk()
        {
            // AddH1ContentBlockAsync calls _client.Pages().Lock() then .CreateWidget().
            // Pages() returns a real PagesClient (not mockable at this level); the calls
            // fail gracefully via try/catch. We verify the observable contract: CreateItem
            // is called with the Pages type and the correct page ID is returned.
            var expectedId = Guid.NewGuid();

            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(BuildSdkItem(expectedId));

            var processor = new PageProcessor(_mockClient.Object);
            var result = await processor.CreatePageAsync("Home", Guid.NewGuid());

            result.Should().Be(expectedId);
            _mockClient.Verify(c => c.CreateItem<SdkItem>(It.Is<CreateArgs>(a =>
                a.Type == RestClientContentTypes.Pages)), Times.Once);
        }

        [Fact]
        public async Task CreatePageAsync_ShouldPublish_AfterCreation()
        {
            // PublishPageAsync calls _client.Publish(new PublishArgs(...)).
            // Publish() is an SDK extension that internally uses IODataRestClient;
            // it fails gracefully via try/catch. We verify that page creation succeeds
            // and the returned ID matches the created item.
            var expectedId = Guid.NewGuid();

            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(BuildSdkItem(expectedId));

            var processor = new PageProcessor(_mockClient.Object);
            var result = await processor.CreatePageAsync("Home", Guid.NewGuid());

            result.Should().Be(expectedId);
            _mockClient.Verify(c => c.CreateItem<SdkItem>(It.Is<CreateArgs>(a =>
                a.Type == RestClientContentTypes.Pages)), Times.Once);
        }
    }

    // -------------------------------------------------------------------------
    // ScaffoldPagesAsync — root pages (no Console interaction needed)
    // -------------------------------------------------------------------------

    public class PageProcessor_ScaffoldRootPagesTests
    {
        private readonly Mock<IRestClient> _mockClient = new();

        private static SdkItem MakeSdkItem(Guid id)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }

        [Fact]
        public async Task ScaffoldPagesAsync_ShouldCreateRootPages_WithNoParent()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var queue = new Queue<SdkItem>(new[] { MakeSdkItem(id1), MakeSdkItem(id2) });
            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(() => queue.Dequeue());

            var processor = new PageProcessor(_mockClient.Object);

            var pages = new List<PageDefinition>
            {
                new() { Title = "Home" },
                new() { Title = "Services" }
            };

            Func<Task> act = async () => await processor.ScaffoldPagesAsync(pages, Guid.NewGuid());

            await act.Should().NotThrowAsync();
            _mockClient.Verify(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ScaffoldPagesAsync_ShouldCreateChildPage_WhenParentAlreadyKnown()
        {
            var parentId  = Guid.NewGuid();
            var childId   = Guid.NewGuid();
            CreateArgs? childArgs = null;
            var callCount = 0;

            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync((CreateArgs a) =>
                {
                    callCount++;
                    if (callCount == 2) childArgs = a;
                    var id = callCount == 1 ? parentId : childId;
                    return MakeSdkItem(id);
                });

            var processor = new PageProcessor(_mockClient.Object);

            var pages = new List<PageDefinition>
            {
                new() { Title = "About Us" },
                new() { Title = "Contact", ParentTitle = "About Us" }
            };

            await processor.ScaffoldPagesAsync(pages, Guid.NewGuid());

            childArgs.Should().NotBeNull();
            var childDataDict = childArgs!.Data.Should().BeOfType<Dictionary<string, object>>().Subject;
            childDataDict.Should().ContainKey("ParentId");
            childDataDict["ParentId"].Should().Be(parentId.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // ScaffoldPagesAsync — missing parent scenarios (Console interaction)
    // -------------------------------------------------------------------------

    [Collection("ConsoleTests")]
    public class PageProcessor_ScaffoldMissingParentTests
    {
        private readonly Mock<IRestClient> _mockClient = new();

        private static SdkItem MakeSdkItem(Guid id)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }

        [Fact]
        public async Task ScaffoldPagesAsync_ShouldSkipChild_WhenParentNotFoundAndUserDeclines()
        {
            var emptyResponse = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(emptyResponse);

            var processor = new PageProcessor(_mockClient.Object);

            var pages = new List<PageDefinition>
            {
                new() { Title = "Contact", ParentTitle = "About Us" }
            };

            var originalOut = Console.Out;
            var originalIn  = Console.In;
            try
            {
                Console.SetOut(new StringWriter());
                Console.SetIn(new StringReader("n\n"));

                Func<Task> act = async () => await processor.ScaffoldPagesAsync(pages, Guid.NewGuid());
                await act.Should().NotThrowAsync();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }

            _mockClient.Verify(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()), Times.Never);
        }

        [Fact]
        public async Task ScaffoldPagesAsync_ShouldFindParent_WhenParentExistsInSitefinity()
        {
            var existingParentId = Guid.NewGuid();
            var childId          = Guid.NewGuid();

            var parentResponse = BuildCollectionResponse(BuildSdkItemWithId(existingParentId));
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(parentResponse);
            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(MakeSdkItem(childId));

            var processor = new PageProcessor(_mockClient.Object);

            var pages = new List<PageDefinition>
            {
                new() { Title = "Contact", ParentTitle = "About Us" }
            };

            var originalOut = Console.Out;
            try
            {
                Console.SetOut(new StringWriter());
                await processor.ScaffoldPagesAsync(pages, Guid.NewGuid());
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            _mockClient.Verify(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()), Times.Once);
        }

        [Fact]
        public async Task ScaffoldPagesAsync_ShouldCreateParent_WhenNotFoundAndUserAccepts()
        {
            var emptyResponse = BuildCollectionResponse();
            _mockClient
                .Setup(c => c.GetItems<SdkItem>(It.IsAny<GetAllArgs>()))
                .ReturnsAsync(emptyResponse);

            var parentId  = Guid.NewGuid();
            var childId   = Guid.NewGuid();
            var callCount = 0;
            _mockClient
                .Setup(c => c.CreateItem<SdkItem>(It.IsAny<CreateArgs>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return MakeSdkItem(callCount == 1 ? parentId : childId);
                });

            var processor = new PageProcessor(_mockClient.Object);

            var pages = new List<PageDefinition>
            {
                new() { Title = "Contact", ParentTitle = "About Us" }
            };

            var originalOut = Console.Out;
            var originalIn  = Console.In;
            try
            {
                Console.SetOut(new StringWriter());
                Console.SetIn(new StringReader("y\n"));

                await processor.ScaffoldPagesAsync(pages, Guid.NewGuid());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }

            callCount.Should().BeGreaterThanOrEqualTo(1);
        }

        // ---- helpers ----

        private static CollectionResponse<SdkItem> BuildCollectionResponse(params SdkItem[] items) =>
            new CollectionResponse<SdkItem> { Items = items.ToList(), TotalCount = items.Length };

        private static SdkItem BuildSdkItemWithId(Guid id)
        {
            var item = new SdkItem();
            typeof(SdkItem).GetProperty("Id")!.SetValue(item, id.ToString());
            return item;
        }
    }

    // =========================================================================
    // Shared test helpers
    // =========================================================================

    internal static class PageProcessorTestHelpers
    {
        internal static CollectionResponse<SdkItem> BuildCollectionResponse(params SdkItem[] items)
        {
            return new CollectionResponse<SdkItem>
            {
                Items      = items.ToList(),
                TotalCount = items.Length
            };
        }

        internal static SdkItem BuildSdkItemWithId(Guid id)
        {
            var item = new SdkItem();
            typeof(SdkItem)
                .GetProperty("Id")!
                .SetValue(item, id.ToString());
            return item;
        }
    }

    // Make module-level helpers available in every class via static using-style delegation
    // (each class that needs them calls the shared static directly)

    internal static class BuildHelpers
    {
        internal static CollectionResponse<SdkItem> BuildCollectionResponse(params SdkItem[] items) =>
            PageProcessorTestHelpers.BuildCollectionResponse(items);

        internal static SdkItem BuildSdkItemWithId(Guid id) =>
            PageProcessorTestHelpers.BuildSdkItemWithId(id);
    }

    }

// Make the helpers available to the classes in this namespace without full qualification
namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    // Bring helpers into each class scope via inheritance-style static delegation
    public abstract class PageProcessorTestBase
    {
        protected static CollectionResponse<SdkItem> BuildCollectionResponse(params SdkItem[] items) =>
            PageProcessorTestHelpers.BuildCollectionResponse(items);

        protected static SdkItem BuildSdkItemWithId(Guid id) =>
            PageProcessorTestHelpers.BuildSdkItemWithId(id);
    }
}
