using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;
using System.Globalization;

namespace SitefinityContentUpdater.Core.Helpers
{
    public class RelationshipProcessor
    {
        private readonly IRestClient _client;
        private readonly string _csvFilePath;

        public RelationshipProcessor(IRestClient client, string csvFilePath)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _csvFilePath = csvFilePath ?? throw new ArgumentNullException(nameof(csvFilePath));
        }

        public async Task<string> BuildRelationshipsAsync(string contentType, string relationshipFieldName, bool testMode = false)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (string.IsNullOrEmpty(relationshipFieldName))
            {
                throw new ArgumentNullException(nameof(relationshipFieldName));
            }

            if (!File.Exists(_csvFilePath))
            {
                ConsoleHelper.WriteError($"CSV file not found at: {_csvFilePath}");
                return "Failed";
            }

            var relationships = LoadRelationshipsFromCsv();

            if (relationships.Count == 0)
            {
                ConsoleHelper.WriteError("No relationships found in CSV file.");
                return "Failed";
            }

            ConsoleHelper.WriteInfo($"Loaded {relationships.Count} relationship mapping(s) from CSV.");

            var totalProcessed = 0;
            var totalUpdated = 0;
            var totalFailed = 0;

            var itemsToProcess = testMode ? relationships.Take(1).ToList() : relationships;

            if (testMode)
            {
                ConsoleHelper.WriteInfo($"TEST MODE: Processing only 1 relationship mapping.");
            }
            else
            {
                ConsoleHelper.WriteInfo($"Processing {relationships.Count} relationship mapping(s).");
            }

            foreach (var relationship in itemsToProcess)
            {
                totalProcessed++;

                try
                {
                    var result = await ProcessRelationshipAsync(contentType, relationshipFieldName, relationship);
                    
                    if (result)
                    {
                        totalUpdated++;
                    }
                    else
                    {
                        totalFailed++;
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    ConsoleHelper.WriteError($"Error processing relationship for item {relationship.ParentItemId}: {ex.Message}");
                }
            }

            if (testMode)
            {
                ConsoleHelper.WriteSuccess($"TEST MODE COMPLETED: Processed {totalProcessed} mapping(s), Updated {totalUpdated}, Failed {totalFailed}.");
                ConsoleHelper.WriteInfo("Review the results above. Run again without test mode to process all mappings.");
            }
            else
            {
                ConsoleHelper.WriteSuccess($"Relationship building completed. Processed {totalProcessed} mappings, Updated {totalUpdated}, Failed {totalFailed}.");
            }

            return "Completed";
        }

        private async Task<bool> ProcessRelationshipAsync(string contentType, string relationshipFieldName, RelationshipMapping mapping)
        {
            try
            {
                // Get the parent item
                var parentItem = await _client.GetItem<SdkItem>(new GetItemArgs()
                {
                    Type = contentType,
                    Id = mapping.ParentItemId
                });

                if (parentItem == null)
                {
                    ConsoleHelper.WriteWarning($"Parent item not found: {mapping.ParentItemId}");
                    return false;
                }

                // Parse related item IDs
                var relatedItemIds = ParseRelatedItemIds(mapping.RelatedItemIds);

                if (relatedItemIds.Count == 0)
                {
                    ConsoleHelper.WriteWarning($"No valid related item IDs found for parent item: {mapping.ParentItemId}");
                    return false;
                }

                // Verify related items exist
                var validRelatedIds = await VerifyRelatedItemsExist(mapping.RelatedContentType, relatedItemIds);

                if (validRelatedIds.Count == 0)
                {
                    ConsoleHelper.WriteWarning($"No valid related items found for parent item: {mapping.ParentItemId}");
                    return false;
                }

                // Create the relationship by updating the parent item
                var updateItem = new SdkItem(parentItem.Id)
                {
                    [relationshipFieldName] = validRelatedIds
                };

                await _client.UpdateItem(new UpdateArgs()
                {
                    Type = contentType,
                    Item = updateItem
                });

                ConsoleHelper.WriteSuccess($"Updated item {mapping.ParentItemId}: Added {validRelatedIds.Count} related item(s) to '{relationshipFieldName}'");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to update item {mapping.ParentItemId}: {ex.Message}");
                return false;
            }
        }

        private List<Guid> ParseRelatedItemIds(string relatedItemIdsString)
        {
            var ids = new List<Guid>();

            if (string.IsNullOrWhiteSpace(relatedItemIdsString))
            {
                return ids;
            }

            // Split by comma, semicolon, or pipe
            var separators = new[] { ',', ';', '|' };
            var parts = relatedItemIdsString.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (Guid.TryParse(trimmed, out var guid))
                {
                    ids.Add(guid);
                }
                else
                {
                    ConsoleHelper.WriteWarning($"Invalid GUID format: {trimmed}");
                }
            }

            return ids;
        }

        private async Task<List<Guid>> VerifyRelatedItemsExist(string relatedContentType, List<Guid> relatedItemIds)
        {
            var validIds = new List<Guid>();

            try
            {
                // Build filter to check if items exist
                var filter = string.Join(" or ", relatedItemIds.Select(id => $"Id eq {id}"));

                var response = await _client.GetItems<SdkItem>(new GetAllArgs()
                {
                    Type = relatedContentType,
                    Filter = filter,
                    Take = relatedItemIds.Count,
                    Fields = new List<string> { "Id" }
                });

                foreach (var item in response.Items)
                {
                    validIds.Add(item.Id);
                }

                if (validIds.Count != relatedItemIds.Count)
                {
                    var notFoundIds = relatedItemIds.Except(validIds).ToList();
                    foreach (var notFoundId in notFoundIds)
                    {
                        ConsoleHelper.WriteWarning($"Related item not found: {notFoundId}");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error verifying related items: {ex.Message}");
            }

            return validIds;
        }

        private List<RelationshipMapping> LoadRelationshipsFromCsv()
        {
            var relationships = new List<RelationshipMapping>();

            try
            {
                using var reader = new StreamReader(_csvFilePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                relationships = csv.GetRecords<RelationshipMapping>().ToList();
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error loading CSV file: {ex.Message}");
            }

            return relationships;
        }
    }

    public class RelationshipMapping
    {
        [Name("ParentItemId")]
        public Guid ParentItemId { get; set; }

        [Name("RelatedContentType")]
        public string RelatedContentType { get; set; } = string.Empty;

        [Name("RelatedItemIds")]
        public string RelatedItemIds { get; set; } = string.Empty;
    }
}
