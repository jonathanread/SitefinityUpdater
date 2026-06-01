using FluentAssertions;
using Moq;
using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Extensions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SitefinityContentUpdater.Core.Tests.Extensions
{
    // -------------------------------------------------------------------------
    // Shared fake handler
    // -------------------------------------------------------------------------

    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;
        private HttpRequestMessage? _lastRequest;
        private string? _lastRequestBody;

        public HttpRequestMessage? LastRequest => _lastRequest;
        public string? LastRequestBody        => _lastRequestBody;

        public StubHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "{}")
        {
            _statusCode   = statusCode;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _lastRequest = request;
            if (request.Content != null)
            {
                _lastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    // -------------------------------------------------------------------------
    // RelateItemAsync
    // -------------------------------------------------------------------------

    public class RestClientExtensions_RelateItemTests
    {
        private readonly Mock<IRestClient> _mockClient = new();

        [Fact]
        public async Task RelateItemAsync_ShouldSucceed_WhenApiReturnsOk()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            Func<Task> act = async () => await _mockClient.Object.RelateItemAsync(
                httpClient,
                "http://localhost/sf/system/",
                "Telerik.Sitefinity.News.Model.NewsItem",
                Guid.NewGuid(),
                "RelatedNews",
                "Telerik.Sitefinity.News.Model.NewsItem",
                Guid.NewGuid());

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RelateItemAsync_ShouldThrow_WhenApiReturnsError()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.BadRequest, "Bad Request");
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            Func<Task> act = async () => await _mockClient.Object.RelateItemAsync(
                httpClient,
                "http://localhost/sf/system/",
                "Telerik.Sitefinity.News.Model.NewsItem",
                Guid.NewGuid(),
                "RelatedNews",
                "Telerik.Sitefinity.News.Model.NewsItem",
                Guid.NewGuid());

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task RelateItemAsync_ShouldSendCorrectPayload()
        {
            var parentId = Guid.NewGuid();
            var childId  = Guid.NewGuid();
            var handler  = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.RelateItemAsync(
                httpClient,
                "http://localhost/sf/system/",
                "Telerik.Sitefinity.News.Model.NewsItem",
                parentId,
                "RelatedItems",
                "Telerik.Sitefinity.News.Model.NewsItem",
                childId);

            var body = handler.LastRequestBody;
            body.Should().Contain(parentId.ToString());
            body.Should().Contain(childId.ToString());
            body.Should().Contain("RelatedItems");
        }

        [Fact]
        public async Task RelateItemAsync_ShouldPostToCorrectEndpoint()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };
            var baseUrl    = "http://localhost/sf/system/";

            await _mockClient.Object.RelateItemAsync(
                httpClient, baseUrl,
                "SomeType", Guid.NewGuid(), "Field", "OtherType", Guid.NewGuid());

            var requestUri = handler.LastRequest?.RequestUri?.ToString();
            requestUri.Should().NotBeNullOrEmpty();
            requestUri.Should().Contain("Default.RelateItem");
        }

        [Fact]
        public async Task RelateItemAsync_ShouldSendJsonContentType()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.RelateItemAsync(
                httpClient, "http://localhost/sf/system/",
                "SomeType", Guid.NewGuid(), "Field", "OtherType", Guid.NewGuid());

            handler.LastRequest?.Content?.Headers.ContentType?.MediaType
                .Should().Be("application/json");
        }
    }

    // -------------------------------------------------------------------------
    // BatchRelateItemsAsync
    // -------------------------------------------------------------------------

    public class RestClientExtensions_BatchRelateItemsTests
    {
        private readonly Mock<IRestClient> _mockClient = new();

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldSucceed_WhenApiReturnsOk()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            Func<Task> act = async () => await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "Telerik.Sitefinity.News.Model.NewsItem",
                Guid.NewGuid(),
                "RelatedNews",
                "Telerik.Sitefinity.News.Model.NewsItem",
                new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldThrow_WhenApiReturnsError()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "Error");
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            Func<Task> act = async () => await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                Guid.NewGuid(),
                "Field",
                "OtherType",
                new List<Guid> { Guid.NewGuid() });

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldSendAllChildIds()
        {
            var childId1 = Guid.NewGuid();
            var childId2 = Guid.NewGuid();
            var childId3 = Guid.NewGuid();
            var handler  = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                Guid.NewGuid(),
                "Field",
                "OtherType",
                new List<Guid> { childId1, childId2, childId3 });

            var body = handler.LastRequestBody;
            body.Should().Contain(childId1.ToString());
            body.Should().Contain(childId2.ToString());
            body.Should().Contain(childId3.ToString());
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldSendCorrectParentIdAndFieldName()
        {
            var parentId       = Guid.NewGuid();
            var relationField  = "MyRelationshipField";
            var handler        = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient     = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                parentId,
                relationField,
                "OtherType",
                new List<Guid> { Guid.NewGuid() });

            var body = handler.LastRequestBody;
            body.Should().Contain(parentId.ToString());
            body.Should().Contain(relationField);
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldPostToCorrectEndpoint()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                Guid.NewGuid(),
                "Field",
                "OtherType",
                new List<Guid> { Guid.NewGuid() });

            var requestUri = handler.LastRequest?.RequestUri?.ToString();
            requestUri.Should().NotBeNullOrEmpty();
            requestUri.Should().Contain("Default.BatchRelateItems");
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldWork_WithEmptyChildList()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            Func<Task> act = async () => await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                Guid.NewGuid(),
                "Field",
                "OtherType",
                new List<Guid>());

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task BatchRelateItemsAsync_ShouldSendJsonContentType()
        {
            var handler    = new StubHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/sf/system/") };

            await _mockClient.Object.BatchRelateItemsAsync(
                httpClient,
                "http://localhost/sf/system/",
                "SomeType",
                Guid.NewGuid(),
                "Field",
                "OtherType",
                new List<Guid> { Guid.NewGuid() });

            handler.LastRequest?.Content?.Headers.ContentType?.MediaType
                .Should().Be("application/json");
        }
    }
}
