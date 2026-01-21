using Progress.Sitefinity.RestSdk;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SitefinityContentUpdater.Core.Extensions
{
    /// <summary>
    /// Extension methods for IRestClient to support relationship operations
    /// </summary>
    public static class RestClientExtensions
    {
        /// <summary>
        /// Creates a relationship between a parent item and a child item using the Sitefinity REST API
        /// This is an extension method that provides RelateItem functionality for older SDK versions
        /// </summary>
        /// <param name="client">The REST client instance</param>
        /// <param name="httpClient">The HTTP client with Sitefinity credentials</param>
        /// <param name="baseUrl">The base URL of the Sitefinity instance</param>
        /// <param name="parentType">The content type of the parent item</param>
        /// <param name="parentId">The ID of the parent item</param>
        /// <param name="relationshipFieldName">The name of the relationship field</param>
        /// <param name="childType">The content type of the related item</param>
        /// <param name="childId">The ID of the related item</param>
        public static async Task RelateItemAsync(
            this IRestClient client,
            HttpClient httpClient,
            string baseUrl,
            string parentType,
            Guid parentId,
            string relationshipFieldName,
            string childType,
            Guid childId)
        {
            var endpoint = $"{baseUrl}Default.Model()/Default.RelateItem()";
            
            var requestBody = new
            {
                parentType = parentType,
                parentId = parentId.ToString(),
                relationshipName = relationshipFieldName,
                childType = childType,
                childId = childId.ToString()
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Batch relates multiple items to a parent item using the Sitefinity REST API
        /// </summary>
        /// <param name="client">The REST client instance</param>
        /// <param name="httpClient">The HTTP client with Sitefinity credentials</param>
        /// <param name="baseUrl">The base URL of the Sitefinity instance</param>
        /// <param name="parentType">The content type of the parent item</param>
        /// <param name="parentId">The ID of the parent item</param>
        /// <param name="relationshipFieldName">The name of the relationship field</param>
        /// <param name="childType">The content type of the related items</param>
        /// <param name="childIds">The IDs of the related items</param>
        public static async Task BatchRelateItemsAsync(
            this IRestClient client,
            HttpClient httpClient,
            string baseUrl,
            string parentType,
            Guid parentId,
            string relationshipFieldName,
            string childType,
            List<Guid> childIds)
        {
            var endpoint = $"{baseUrl}Default.BatchRelateItems()";
            
            var requestBody = new
            {
                parentType = parentType,
                parentId = parentId.ToString(),
                relationshipName = relationshipFieldName,
                childType = childType,
                childIds = childIds.Select(id => id.ToString()).ToArray()
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
        }
    }
}
