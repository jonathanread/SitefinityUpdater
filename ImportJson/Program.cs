using System.Text.Json;
using System.Text.Json.Nodes;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;
using Progress.Sitefinity.RestSdk.Management;
using SitefinityContentUpdater.Core.Helpers;
using SitefinityContentUpdater.Core.RestClient;

internal class Program
{
    private const string ChildPrefix = "Child_";
    private const string ParentIdFieldName = "ParentId";

    private static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSuccess("Sitefinity JSON Importer");
        ConsoleHelper.WriteInfo("Import JSON in format { \"SitefinityContentTypeName\":[{}] }");
        Console.WriteLine();

        try
        {
            var configuration = ConfigurationHelper.LoadConfiguration();
            var config = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

            var client = await ConnectToSiteAsync(config);
            if (client == null)
            {
                return;
            }

            var jsonFilePath = GetJsonFilePath(args, configuration);
            if (!File.Exists(jsonFilePath))
            {
                ConsoleHelper.WriteError($"JSON file not found: {jsonFilePath}");
                return;
            }

            var importPlan = await BuildImportPlanAsync(jsonFilePath);
            if (importPlan.TopLevelTypes.Count == 0)
            {
                ConsoleHelper.WriteWarning("No content items found in JSON file.");
                return;
            }

            ConsoleHelper.WriteInfo($"Top-level content types found: {importPlan.TopLevelTypes.Count}");
            ConsoleHelper.WriteInfo($"Top-level items found: {importPlan.TopLevelTypes.Sum(t => t.Items.Count)}");
            ConsoleHelper.WriteInfo($"Contains child items: {(importPlan.HasChildren ? "Yes" : "No")}");

            var keepAsDraft = ConsoleHelper.Confirm("Do you prefer items to remain as draft? (y/n)");
            var publishItems = !keepAsDraft;
            if (publishItems)
            {
                ConsoleHelper.WriteInfo("Items will be published after creation (default behavior).");
            }
            else
            {
                ConsoleHelper.WriteWarning("Items will remain in draft status.");
            }

            var testMode = ConsoleHelper.Confirm("Do you want to test on 1 top-level item first? (y/n)");
            if (testMode)
            {
                ConsoleHelper.WriteInfo("Running in TEST MODE - Only 1 top-level item will be processed.");
            }
            else
            {
                ConsoleHelper.WriteWarning("Running in FULL MODE - All items will be processed.");
                if (!ConsoleHelper.Confirm("Are you sure you want to continue? (y/n)"))
                {
                    ConsoleHelper.WriteInfo("Operation cancelled.");
                    return;
                }
            }

            var createdParents = await CreateTopLevelItemsAsync(client, importPlan, testMode, publishItems);

            if (importPlan.HasChildren)
            {
                await CreateChildItemsAsync(client, createdParents, publishItems);
            }

            Console.WriteLine();
            ConsoleHelper.WriteSuccess("Import completed. Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            ConsoleHelper.WriteError(ex.ToString());
            if (ex.InnerException != null)
            {
                ConsoleHelper.WriteError($"Inner exception: {ex.InnerException.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    private static async Task<IRestClient?> ConnectToSiteAsync(SitefinityConfig config)
    {
        var client = await RestClientFactory.GetRestClient(config);
        var validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId);

        if (!validationResult.IsValid)
        {
            if (!validationResult.RequiresReconnect)
            {
                return null;
            }

            config.SiteId = validationResult.SiteId;
            client = await RestClientFactory.GetRestClient(config);
            validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId);

            if (!validationResult.IsValid)
            {
                return null;
            }
        }

        return client;
    }

    private static string GetJsonFilePath(string[] args, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var configuredPath = configuration["JsonFilePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        ConsoleHelper.WriteInfo("No JSON file path found in appsettings.");

        while (true)
        {
            var input = ConsoleHelper.ReadLine("Enter the JSON file path:");
            if (!string.IsNullOrWhiteSpace(input))
            {
                return Path.GetFullPath(input);
            }

            ConsoleHelper.WriteWarning("JSON file path is required.");
        }
    }

    private static async Task<ImportPlan> BuildImportPlanAsync(string jsonFilePath)
    {
        var json = await File.ReadAllTextAsync(jsonFilePath);
        var root = JsonNode.Parse(json) as JsonObject;

        if (root == null)
        {
            throw new InvalidOperationException("Invalid JSON. Root must be an object.");
        }

        var topLevelTypes = new List<TopLevelTypePlan>();
        var hasChildren = false;

        foreach (var property in root)
        {
            var contentType = property.Key;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                continue;
            }

            if (property.Value is not JsonArray array)
            {
                ConsoleHelper.WriteWarning($"Skipping '{contentType}' because value is not an array.");
                continue;
            }

            var items = new List<ImportItemPlan>();
            foreach (var node in array)
            {
                if (node is not JsonObject jsonObject)
                {
                    ConsoleHelper.WriteWarning($"Skipping non-object item in '{contentType}' array.");
                    continue;
                }

                var parsedItem = ParseImportItem(jsonObject);
                if (parsedItem.ChildCollections.Count > 0)
                {
                    hasChildren = true;
                }

                items.Add(parsedItem);
            }

            if (items.Count > 0)
            {
                topLevelTypes.Add(new TopLevelTypePlan(contentType, items));
            }
        }

        return new ImportPlan(topLevelTypes, hasChildren);
    }

    private static ImportItemPlan ParseImportItem(JsonObject jsonObject)
    {
        var fields = new JsonObject();
        var childCollections = new List<ChildCollectionPlan>();

        foreach (var field in jsonObject)
        {
            if (field.Key.StartsWith(ChildPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var childContentType = field.Key[ChildPrefix.Length..].Trim();
                if (string.IsNullOrWhiteSpace(childContentType))
                {
                    ConsoleHelper.WriteWarning($"Skipping child field '{field.Key}' because it does not contain a child content type name.");
                    continue;
                }

                var childItems = ParseChildItems(field.Value);

                if (childItems.Count > 0)
                {
                    childCollections.Add(new ChildCollectionPlan(childContentType, childItems));
                }

                continue;
            }

            fields[field.Key] = field.Value?.DeepClone();
        }

        return new ImportItemPlan(fields, childCollections);
    }

    private static List<ImportItemPlan> ParseChildItems(JsonNode? childNode)
    {
        var result = new List<ImportItemPlan>();

        switch (childNode)
        {
            case JsonObject childObject:
                result.Add(ParseImportItem((JsonObject)childObject.DeepClone()));
                break;

            case JsonArray childArray:
                foreach (var item in childArray)
                {
                    if (item is JsonObject itemObject)
                    {
                        result.Add(ParseImportItem((JsonObject)itemObject.DeepClone()));
                    }
                }
                break;
        }

        return result;
    }

    private static async Task<List<CreatedParentItem>> CreateTopLevelItemsAsync(IRestClient client, ImportPlan importPlan, bool testMode, bool publishItems)
    {
        var createdParents = new List<CreatedParentItem>();
        var processed = 0;

        foreach (var typePlan in importPlan.TopLevelTypes)
        {
            foreach (var itemPlan in typePlan.Items)
            {
                try
                {
                    var sdkItem = BuildSdkItem(itemPlan.Fields);
                    var created = await client.CreateItem<SdkItem>(BuildCreateArgs(typePlan.ContentType, sdkItem, publishItems));

                    if (created == null)
                    {
                        throw new InvalidOperationException($"CreateItem returned null for type '{typePlan.ContentType}'.");
                    }

                    if (string.IsNullOrWhiteSpace(created.Id))
                    {
                        throw new InvalidOperationException($"Created item returned empty ID for type '{typePlan.ContentType}'.");
                    }

                    if (publishItems)
                    {
                        await PublishItemAsync(client, typePlan.ContentType, created);
                    }

                    ConsoleHelper.WriteSuccess($"Created parent item in '{typePlan.ContentType}' with ID: {created.Id}");

                    createdParents.Add(new CreatedParentItem(
                        typePlan.ContentType,
                        created.Id,
                        itemPlan.ChildCollections));

                    processed++;
                    if (testMode && processed >= 1)
                    {
                        return createdParents;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed creating top-level item for content type '{typePlan.ContentType}'. Payload: {itemPlan.Fields.ToJsonString()}",
                        ex);
                }
            }
        }

        return createdParents;
    }

    private static async Task CreateChildItemsAsync(IRestClient client, List<CreatedParentItem> createdParents, bool publishItems)
    {
        foreach (var parent in createdParents)
        {
            await CreateChildItemsForParentAsync(client, parent.Id, parent.ChildCollections, publishItems);
        }
    }

    private static async Task CreateChildItemsForParentAsync(
        IRestClient client,
        string parentId,
        List<ChildCollectionPlan> childCollections,
        bool publishItems)
    {
        foreach (var childCollection in childCollections)
        {
            if (string.IsNullOrWhiteSpace(childCollection.ContentType))
            {
                ConsoleHelper.WriteWarning($"Skipping child creation for parent '{parentId}' because child content type is empty.");
                continue;
            }

            foreach (var childPlan in childCollection.Items)
            {
                JsonObject? childPayload = null;

                try
                {
                    childPayload = (JsonObject)childPlan.Fields.DeepClone();

                    // Child items must use the ID returned by the just-created parent item.
                    if (Guid.TryParse(parentId, out var parentGuid))
                    {
                        childPayload[ParentIdFieldName] = parentGuid;
                    }
                    else
                    {
                        childPayload[ParentIdFieldName] = parentId;
                    }

                    var childItem = BuildSdkItem(childPayload);
                    var createdChild = await client.CreateItem<SdkItem>(BuildCreateArgs(childCollection.ContentType, childItem, publishItems));

                    if (createdChild == null)
                    {
                        throw new InvalidOperationException($"CreateItem returned null for child type '{childCollection.ContentType}'.");
                    }

                    if (string.IsNullOrWhiteSpace(createdChild.Id))
                    {
                        throw new InvalidOperationException($"Created child item returned empty ID for type '{childCollection.ContentType}'.");
                    }

                    if (publishItems)
                    {
                        await PublishItemAsync(client, childCollection.ContentType, createdChild);
                    }

                    ConsoleHelper.WriteSuccess(
                        $"Created child item in '{childCollection.ContentType}' with ID: {createdChild.Id} (ParentId: {parentId})");

                    if (childPlan.ChildCollections.Count > 0)
                    {
                        await CreateChildItemsForParentAsync(client, createdChild.Id, childPlan.ChildCollections, publishItems);
                    }
                }
                catch (Exception ex)
                {
                    var payloadForError = childPayload?.ToJsonString() ?? childPlan.Fields.ToJsonString();
                    throw new InvalidOperationException(
                        $"Failed creating child item for type '{childCollection.ContentType}' (ParentId: {parentId}). Payload: {payloadForError}",
                        ex);
                }
            }
        }
    }

    private static CreateArgs BuildCreateArgs(string contentType, SdkItem data, bool publishItems)
    {
        return new CreateArgs
        {
            Type = contentType,
            Data = data
        };
    }

    private static async Task PublishItemAsync(IRestClient client, string contentType, SdkItem item)
    {
        try
        {
            await ItemManagementExtensions.Publish(client, new PublishArgs(contentType, item.Id));
            ConsoleHelper.WriteInfo($"Published item '{item.Id}' in '{contentType}'.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Item '{item.Id}' was created but failed to publish for type '{contentType}'.", ex);
        }
    }

    private static SdkItem BuildSdkItem(JsonObject jsonObject)
    {
        var item = new SdkItem();

        foreach (var property in jsonObject)
        {
            var value = ConvertJsonNode(property.Value);
            if (value == null)
            {
                continue;
            }

            // Sitefinity dynamic types often expect Guid values for *Id fields.
            if (value is string stringValue &&
                property.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(stringValue, out var guidValue))
            {
                item.SetValue(property.Key, guidValue);
                continue;
            }

            item.SetValue(property.Key, value);
        }

        return item;
    }

    private static object? ConvertJsonNode(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        if (node is JsonObject jsonObject)
        {
            var dictionary = new Dictionary<string, object?>();
            foreach (var property in jsonObject)
            {
                dictionary[property.Key] = ConvertJsonNode(property.Value);
            }

            return dictionary;
        }

        if (node is JsonArray jsonArray)
        {
            return jsonArray.Select(ConvertJsonNode).ToList();
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolValue)) return boolValue;
            if (jsonValue.TryGetValue<int>(out var intValue)) return intValue;
            if (jsonValue.TryGetValue<long>(out var longValue)) return longValue;
            if (jsonValue.TryGetValue<decimal>(out var decimalValue)) return decimalValue;
            if (jsonValue.TryGetValue<double>(out var doubleValue)) return doubleValue;
            if (jsonValue.TryGetValue<string>(out var stringValue)) return stringValue;

            using var document = JsonDocument.Parse(jsonValue.ToJsonString());
            return ConvertJsonElement(document.RootElement);
        }

        return node.ToJsonString();
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }

    private sealed record ImportPlan(List<TopLevelTypePlan> TopLevelTypes, bool HasChildren);
    private sealed record TopLevelTypePlan(string ContentType, List<ImportItemPlan> Items);
    private sealed record ImportItemPlan(JsonObject Fields, List<ChildCollectionPlan> ChildCollections);
    private sealed record ChildCollectionPlan(string ContentType, List<ImportItemPlan> Items);
    private sealed record CreatedParentItem(string ContentType, string Id, List<ChildCollectionPlan> ChildCollections);
}
