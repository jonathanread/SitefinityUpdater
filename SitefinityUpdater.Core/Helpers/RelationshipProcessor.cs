using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;

namespace SitefinityContentUpdater.Core.Helpers
{
    public class RelationshipProcessor
    {
        private readonly IRestClient _sourceClient;
        private readonly IRestClient _targetClient;
        private const int BatchSize = 50;

        public RelationshipProcessor(IRestClient sourceClient, IRestClient targetClient)
        {
            _sourceClient = sourceClient ?? throw new ArgumentNullException(nameof(sourceClient));
            _targetClient = targetClient ?? throw new ArgumentNullException(nameof(targetClient));
        }

        public async Task<string> BuildRelationshipsAsync(
            string contentType,
            IEnumerable<string> relationshipFieldNames,
            bool testMode = false)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            var fieldNames = relationshipFieldNames?.ToList() ?? [];
            if (fieldNames.Count == 0)
            {
                throw new ArgumentException("At least one relationship field name is required.", nameof(relationshipFieldNames));
            }

            ConsoleHelper.WriteInfo($"Starting relationship sync for content type: {contentType}");
            ConsoleHelper.WriteInfo($"Relationship fields: {string.Join(", ", fieldNames)}");

            var totalProcessed = 0;
            var totalUpdated = 0;
            var totalFailed = 0;
            var skip = 0;

            // Build fields list to include Id and all relationship fields
            var fieldsToFetch = new List<string> { "Id" };
            fieldsToFetch.AddRange(fieldNames);

            // Get initial count from source
            var sourceResponse = await _sourceClient.GetItems<SdkItem>(new GetAllArgs
            {
                Type = contentType,
                Skip = 0,
                Take = BatchSize,
                Count = true,
                Fields = fieldsToFetch
            });

            var totalCount = sourceResponse.TotalCount ?? 0;
            ConsoleHelper.WriteInfo($"Found {totalCount} items in source site.");

            if (testMode)
            {
                ConsoleHelper.WriteInfo("TEST MODE: Processing only 1 item.");
            }

            // Iterate over all pages
            while (skip < totalCount)
            {
                if (skip > 0)
                {
                    sourceResponse = await _sourceClient.GetItems<SdkItem>(new GetAllArgs
                    {
                        Type = contentType,
                        Skip = skip,
                        Take = BatchSize,
                        Count = true,
                        Fields = fieldsToFetch
                    });
                }

                ConsoleHelper.WriteInfo($"Processing batch: items {skip + 1} to {Math.Min(skip + BatchSize, totalCount)} of {totalCount}");

                foreach (var sourceItem in sourceResponse.Items)
                {
                    totalProcessed++;

                    try
                    {
                        var updated = await ProcessItemRelationshipsAsync(contentType, sourceItem, fieldNames);
                        if (updated)
                        {
                            totalUpdated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        totalFailed++;
                        ConsoleHelper.WriteError($"Error processing item {sourceItem.Id}: {ex.Message}");
                    }

                    if (testMode)
                    {
                        break;
                    }
                }

                if (testMode)
                {
                    break;
                }

                skip += BatchSize;
            }

            if (testMode)
            {
                ConsoleHelper.WriteSuccess($"TEST MODE COMPLETED: Processed {totalProcessed} item(s), Updated {totalUpdated}, Failed {totalFailed}.");
                ConsoleHelper.WriteInfo("Review the results above. Run again without test mode to process all items.");
            }
            else
            {
                ConsoleHelper.WriteSuccess($"Relationship sync completed. Processed {totalProcessed} items, Updated {totalUpdated}, Failed {totalFailed}.");
            }

            return "Completed";
        }

        private async Task<bool> ProcessItemRelationshipsAsync(
            string contentType,
            SdkItem sourceItem,
            List<string> fieldNames)
        {
            var itemId = sourceItem.Id;
            var anyUpdated = false;

            foreach (var fieldName in fieldNames)
            {
                try
                {
                    // Get related item IDs from source
                    var relatedIds = GetRelatedItemIds(sourceItem, fieldName);

                    if (relatedIds.Count == 0)
                    {
                        ConsoleHelper.WriteInfo($"Item {itemId}: No related items found in field '{fieldName}'");
                        continue;
                    }

                    ConsoleHelper.WriteInfo($"Item {itemId}: Found {relatedIds.Count} related item(s) in field '{fieldName}'");

                    // Create relationships on target site in batches
                    await BatchRelateItemsOnTargetAsync(contentType, itemId, fieldName, relatedIds);
                    anyUpdated = true;

                    ConsoleHelper.WriteSuccess($"Item {itemId}: Created {relatedIds.Count} relationship(s) for field '{fieldName}'");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Item {itemId}: Failed to process field '{fieldName}': {ex.Message}");
                }
            }

            return anyUpdated;
        }

        private List<string> GetRelatedItemIds(SdkItem sourceItem, string fieldName)
        {
            var relatedIds = new List<string>();

            try
            {
                // Try to get the field value as a collection of related items
                var fieldValue = sourceItem.GetValue<object>(fieldName);

                if (fieldValue == null)
                {
                    return relatedIds;
                }

                // Handle different possible formats of related items
                if (fieldValue is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        var id = ExtractIdFromRelatedItem(item);
                        if (!string.IsNullOrEmpty(id))
                        {
                            relatedIds.Add(id);
                        }
                    }
                }
                else if (fieldValue is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    // Single ID as string
                    if (Guid.TryParse(stringValue, out _))
                    {
                        relatedIds.Add(stringValue);
                    }
                }
                else
                {
                    // Try to extract ID from single object
                    var id = ExtractIdFromRelatedItem(fieldValue);
                    if (!string.IsNullOrEmpty(id))
                    {
                        relatedIds.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteWarning($"Could not extract related IDs from field '{fieldName}': {ex.Message}");
            }

            return relatedIds;
        }

        private string? ExtractIdFromRelatedItem(object item)
        {
            if (item == null)
            {
                return null;
            }

            // If it's already a string GUID
            if (item is string str && Guid.TryParse(str, out _))
            {
                return str;
            }

            // If it's a dictionary/dynamic object with Id property
            if (item is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("Id", out var idValue) && idValue != null)
                {
                    return idValue.ToString();
                }
            }

            // If it's a SdkItem
            if (item is SdkItem sdkItem)
            {
                return sdkItem.Id;
            }

            // Try reflection as last resort
            var idProperty = item.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var value = idProperty.GetValue(item);
                return value?.ToString();
            }

            return null;
        }

        private async Task BatchRelateItemsOnTargetAsync(
            string contentType,
            string parentItemId,
            string relationshipFieldName,
            List<string> relatedItemIds)
        {
            var totalBatches = (int)Math.Ceiling(relatedItemIds.Count / (double)BatchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = relatedItemIds.Skip(i * BatchSize).Take(BatchSize).ToList();

                if (totalBatches > 1)
                {
                    ConsoleHelper.WriteInfo($"  Processing batch {i + 1} of {totalBatches} ({batch.Count} items)...");
                }

                // Create relationships using Task.WhenAll for concurrent execution
                var relateTasks = batch.Select(relatedItemId =>
                    _targetClient.RelateItem(new RelateArgs
                    {
                        Type = contentType,
                        Id = parentItemId,
                        RelationName = relationshipFieldName,
                        RelatedItemId = relatedItemId
                    })
                );

                await Task.WhenAll(relateTasks);
            }
        }
    }
}
