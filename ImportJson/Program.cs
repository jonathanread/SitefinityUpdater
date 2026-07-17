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
    private const string RelatedPrefix = "Related_";
    private const string TaxonPrefix = "Taxon_";
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
            ConsoleHelper.WriteInfo($"Contains related items: {(importPlan.HasRelated ? "Yes" : "No")}");
            ConsoleHelper.WriteInfo($"Contains taxonomy fields: {(importPlan.HasTaxa ? "Yes" : "No")}");

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

            var taxonomyProcessor = new TaxonomyProcessor(client);

            if (importPlan.HasTaxa)
            {
                var allTaxonomyNames = CollectTaxonomyNames(importPlan);
                await taxonomyProcessor.PreWarmAsync(allTaxonomyNames);
            }

            var createdParents = await CreateTopLevelItemsAsync(client, importPlan, testMode, publishItems, taxonomyProcessor);

            if (importPlan.HasChildren)
            {
                await CreateChildItemsAsync(client, createdParents, publishItems, taxonomyProcessor);
            }

            if (importPlan.HasRelated)
            {
                await CreateRelatedItemsAsync(client, createdParents, publishItems, taxonomyProcessor);
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

    // Walks the entire import plan tree (top-level → children → related, recursively) and
    // returns every distinct TaxonomyName referenced by any TaxonFieldPlan at any depth.
    // Used to pre-warm the TaxonomyProcessor cache in a single upfront batch.
    private static HashSet<string> CollectTaxonomyNames(ImportPlan plan)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in plan.TopLevelTypes)
            foreach (var item in type.Items)
                CollectTaxonomyNamesFromItem(item, names);
        return names;
    }

    private static void CollectTaxonomyNamesFromItem(ImportItemPlan item, HashSet<string> names)
    {
        foreach (var taxonField in item.TaxonFields)
            names.Add(taxonField.TaxonomyName);

        foreach (var child in item.ChildCollections)
            foreach (var childItem in child.Items)
                CollectTaxonomyNamesFromItem(childItem, names);

        foreach (var related in item.RelatedCollections)
            foreach (var relatedItem in related.Items)
                CollectTaxonomyNamesFromItem(relatedItem, names);
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
        var hasRelated = false;
        var hasTaxa = false;

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

                if (parsedItem.RelatedCollections.Count > 0)
                {
                    hasRelated = true;
                }

                if (parsedItem.TaxonFields.Count > 0)
                {
                    hasTaxa = true;
                }

                items.Add(parsedItem);
            }

            if (items.Count > 0)
            {
                topLevelTypes.Add(new TopLevelTypePlan(contentType, items));
            }
        }

        return new ImportPlan(topLevelTypes, hasChildren, hasRelated, hasTaxa);
    }

    private static ImportItemPlan ParseImportItem(JsonObject jsonObject)
    {
        var fields = new JsonObject();
        var childCollections = new List<ChildCollectionPlan>();
        var relatedCollections = new List<RelatedCollectionPlan>();
        var taxonFields = new List<TaxonFieldPlan>();

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

                var childItems = ParseNestedItems(field.Value);

                if (childItems.Count > 0)
                {
                    childCollections.Add(new ChildCollectionPlan(childContentType, childItems));
                }

                continue;
            }

            if (field.Key.StartsWith(RelatedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var relationFieldName = field.Key[RelatedPrefix.Length..].Trim();
                if (string.IsNullOrWhiteSpace(relationFieldName))
                {
                    ConsoleHelper.WriteWarning($"Skipping related field '{field.Key}' because it does not contain a relation field name.");
                    continue;
                }

                var plan = ParseRelatedCollection(relationFieldName, field.Value);
                if (plan != null)
                {
                    relatedCollections.Add(plan);
                }

                continue;
            }

            if (field.Key.StartsWith(TaxonPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fieldName = field.Key[TaxonPrefix.Length..].Trim();
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    ConsoleHelper.WriteWarning($"Skipping taxon field '{field.Key}' because it does not contain a field name.");
                    continue;
                }

                var taxonPlan = ParseTaxonField(fieldName, field.Value);
                if (taxonPlan != null)
                {
                    taxonFields.Add(taxonPlan);
                }

                continue;
            }

            fields[field.Key] = field.Value?.DeepClone();
        }

        return new ImportItemPlan(fields, childCollections, relatedCollections, taxonFields);
    }

    // Parses a Taxon_ value.
    // Supported format: { "TaxonomyName": "Tags", "Taxa": ["Title A", "Title B"] }
    // Single string shorthand: { "TaxonomyName": "Tags", "Taxa": "Title A" }
    private static TaxonFieldPlan? ParseTaxonField(string fieldName, JsonNode? node)
    {
        if (node is not JsonObject wrapper)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Taxon_{fieldName}' — value must be a JSON object with 'TaxonomyName' and 'Taxa'.");
            return null;
        }

        var taxonomyNameNode = wrapper["TaxonomyName"];
        if (taxonomyNameNode == null)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Taxon_{fieldName}' — missing required 'TaxonomyName' property.");
            return null;
        }

        var taxonomyName = taxonomyNameNode.GetValue<string>().Trim();
        if (string.IsNullOrWhiteSpace(taxonomyName))
        {
            ConsoleHelper.WriteWarning($"Skipping 'Taxon_{fieldName}' — 'TaxonomyName' is empty.");
            return null;
        }

        var taxaNode = wrapper["Taxa"];
        if (taxaNode == null)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Taxon_{fieldName}' — missing required 'Taxa' property.");
            return null;
        }

        var titles = new List<string>();
        switch (taxaNode)
        {
            case JsonArray taxaArray:
                foreach (var item in taxaArray)
                {
                    var title = item?.GetValue<string>()?.Trim();
                    if (!string.IsNullOrWhiteSpace(title))
                        titles.Add(title);
                }
                break;

            case JsonValue taxaValue:
                var singleTitle = taxaValue.GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(singleTitle))
                    titles.Add(singleTitle);
                break;
        }

        if (titles.Count == 0)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Taxon_{fieldName}' — 'Taxa' contains no valid titles.");
            return null;
        }

        return new TaxonFieldPlan(fieldName, taxonomyName, titles);
    }

    // Parses a Related_ value which must be an object with "ContentType" and "Items" (or a single object / array shorthand).
    // Supported formats:
    //   { "ContentType": "...", "Items": [ {...}, {...} ] }
    //   { "ContentType": "...", "Items": { ... } }      <- single item shorthand
    private static RelatedCollectionPlan? ParseRelatedCollection(string relationFieldName, JsonNode? node)
    {
        if (node is not JsonObject wrapper)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Related_{relationFieldName}' — value must be a JSON object with 'ContentType' and 'Items'.");
            return null;
        }

        var contentTypeNode = wrapper["ContentType"];
        if (contentTypeNode == null)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Related_{relationFieldName}' — missing required 'ContentType' property.");
            return null;
        }

        var contentType = contentTypeNode.GetValue<string>().Trim();
        if (string.IsNullOrWhiteSpace(contentType))
        {
            ConsoleHelper.WriteWarning($"Skipping 'Related_{relationFieldName}' — 'ContentType' is empty.");
            return null;
        }

        var itemsNode = wrapper["Items"];
        if (itemsNode == null)
        {
            ConsoleHelper.WriteWarning($"Skipping 'Related_{relationFieldName}' — missing required 'Items' property.");
            return null;
        }

        var items = ParseNestedItems(itemsNode);
        if (items.Count == 0)
        {
            return null;
        }

        return new RelatedCollectionPlan(relationFieldName, contentType, items);
    }

    private static List<ImportItemPlan> ParseNestedItems(JsonNode? node)
    {
        var result = new List<ImportItemPlan>();

        switch (node)
        {
            case JsonObject obj:
                result.Add(ParseImportItem((JsonObject)obj.DeepClone()));
                break;

            case JsonArray array:
                foreach (var item in array)
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

    private static async Task<List<CreatedParentItem>> CreateTopLevelItemsAsync(IRestClient client, ImportPlan importPlan, bool testMode, bool publishItems, TaxonomyProcessor taxonomyProcessor)
    {
        var createdParents = new List<CreatedParentItem>();
        var processed = 0;

        foreach (var typePlan in importPlan.TopLevelTypes)
        {
            var typeName = typePlan.ContentType.Split('.').Last();

            foreach (var itemPlan in typePlan.Items)
            {
                try
                {
                    var fields = (JsonObject)itemPlan.Fields.DeepClone();

                    if (itemPlan.TaxonFields.Count > 0)
                    {
                        var resolvedTaxa = await ResolveTaxaAsync(itemPlan.TaxonFields, taxonomyProcessor);
                        ApplyTaxaToFields(fields, resolvedTaxa);
                    }

                    var sdkItem = BuildSdkItem(fields);

                    // Check for an existing item before creating a duplicate
                    var existing = await FindExistingItemAsync(client, typePlan.ContentType, fields);
                    SdkItem created;
                    if (existing != null)
                    {
                        ConsoleHelper.WriteWarning($"Skipping creation of {typeName} — item already exists with ID: {existing.Id}");
                        created = existing;
                    }
                    else
                    {
                        // Create as draft first
                        created = await client.CreateItem<SdkItem>(BuildCreateArgs(typePlan.ContentType, sdkItem, false));

                        if (created == null)
                        {
                            throw new InvalidOperationException($"CreateItem returned null for type '{typePlan.ContentType}'.");
                        }

                        if (string.IsNullOrWhiteSpace(created.Id))
                        {
                            throw new InvalidOperationException($"Created item returned empty ID for type '{typePlan.ContentType}'.");
                        }

                        ConsoleHelper.WriteSuccess($"Created {typeName} item with ID: {created.Id}");
                    }

                    createdParents.Add(new CreatedParentItem(
                        typePlan.ContentType,
                        created.Id,
                        itemPlan.ChildCollections,
                        itemPlan.RelatedCollections,
                        IsNew: existing == null));

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

        // Publish newly created top-level items (skip pre-existing ones)
        var newParents = createdParents.Where(p => p.IsNew).ToList();
        if (publishItems && newParents.Count > 0)
        {
            ConsoleHelper.WriteInfo($"Publishing {newParents.Count} top-level item(s)...");

            var publishCount = 0;
            foreach (var parent in newParents)
            {
                try
                {
                    await PublishItemAsync(client, parent.ContentType, new SdkItem { Id = parent.Id });
                    publishCount++;
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteWarning($"Failed to publish item '{parent.Id}': {ex.Message}. Item created but remains in draft.");
                }
            }

            ConsoleHelper.WriteSuccess($"Published {publishCount} of {newParents.Count} top-level item(s)");
        }

        return createdParents;
    }

    private static async Task CreateChildItemsAsync(IRestClient client, List<CreatedParentItem> createdParents, bool publishItems, TaxonomyProcessor taxonomyProcessor)
    {
        foreach (var parent in createdParents)
        {
            await CreateChildItemsForParentAsync(client, parent.Id, parent.ChildCollections, publishItems, taxonomyProcessor);
        }
    }

    private static async Task CreateChildItemsForParentAsync(
        IRestClient client,
        string parentId,
        List<ChildCollectionPlan> childCollections,
        bool publishItems,
        TaxonomyProcessor taxonomyProcessor)
    {
        ConsoleHelper.WriteInfo($"Creating {childCollections.Count} child collection type(s) for parent '{parentId}'...");

        for (int collectionIndex = 0; collectionIndex < childCollections.Count; collectionIndex++)
        {
            var childCollection = childCollections[collectionIndex];

            if (string.IsNullOrWhiteSpace(childCollection.ContentType))
            {
                ConsoleHelper.WriteWarning($"Skipping child creation for parent '{parentId}' because child content type is empty.");
                continue;
            }

            var typeName = childCollection.ContentType.Split('.').Last();
            ConsoleHelper.WriteInfo($"[{collectionIndex + 1}/{childCollections.Count}] Processing {childCollection.Items.Count} {typeName} item(s)...");

            var createdChildren = new List<(SdkItem Item, List<ChildCollectionPlan> GrandChildren, List<RelatedCollectionPlan> GrandRelated)>();

            // First pass: Create all children as draft
            foreach (var childPlan in childCollection.Items)
            {
                JsonObject? childPayload = null;

                try
                {
                    childPayload = (JsonObject)childPlan.Fields.DeepClone();

                    if (childPlan.TaxonFields.Count > 0)
                    {
                        var resolvedTaxa = await ResolveTaxaAsync(childPlan.TaxonFields, taxonomyProcessor);
                        ApplyTaxaToFields(childPayload, resolvedTaxa);
                    }

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

                    // Dedup: check whether this child already exists before creating
                    var existingChild = await FindExistingItemAsync(client, childCollection.ContentType, childPayload);
                    SdkItem createdChild;
                    if (existingChild != null)
                    {
                        ConsoleHelper.WriteWarning($"Skipping creation of {typeName} child — item already exists with ID: {existingChild.Id}");
                        createdChild = existingChild;
                    }
                    else
                    {
                        // Always create as draft first
                        createdChild = await client.CreateItem<SdkItem>(BuildCreateArgs(childCollection.ContentType, childItem, false));

                        if (createdChild == null)
                        {
                            throw new InvalidOperationException($"CreateItem returned null for child type '{childCollection.ContentType}'.");
                        }

                        if (string.IsNullOrWhiteSpace(createdChild.Id))
                        {
                            throw new InvalidOperationException($"Created child item returned empty ID for type '{childCollection.ContentType}'.");
                        }
                    }

                    createdChildren.Add((createdChild, childPlan.ChildCollections, childPlan.RelatedCollections));
                }
                catch (Exception ex)
                {
                    var payloadForError = childPayload?.ToJsonString() ?? childPlan.Fields.ToJsonString();
                    var titleValue = childPayload?.ContainsKey("Title") == true ? childPayload["Title"]?.ToString() : "unknown";

                    var errorMessage = $"Failed creating child item '{titleValue}' for type '{childCollection.ContentType}' (ParentId: {parentId}). Payload: {payloadForError}";

                    // Extract the actual API error if available
                    var currentEx = ex;
                    var exceptionChain = new List<string>();
                    while (currentEx != null)
                    {
                        exceptionChain.Add($"{currentEx.GetType().Name}: {currentEx.Message}");
                        currentEx = currentEx.InnerException;
                    }

                    errorMessage += "\nException chain:\n  " + string.Join("\n  \u2192 ", exceptionChain);

                    ConsoleHelper.WriteError(errorMessage);
                    throw new InvalidOperationException(errorMessage, ex);
                }
            }

            ConsoleHelper.WriteSuccess($"Created {createdChildren.Count} {typeName} item(s)");

            // Second pass: Process grandchildren and grand-related for each child
            if (createdChildren.Any(c => c.GrandChildren.Count > 0 || c.GrandRelated.Count > 0))
            {
                foreach (var (child, grandChildren, grandRelated) in createdChildren)
                {
                    if (grandChildren.Count > 0)
                    {
                        await CreateChildItemsForParentAsync(client, child.Id, grandChildren, publishItems, taxonomyProcessor);
                    }

                    if (grandRelated.Count > 0)
                    {
                        await CreateRelatedItemsForParentAsync(client, childCollection.ContentType, child.Id, grandRelated, publishItems, taxonomyProcessor);
                    }
                }
            }

            // Third pass: Publish all children if requested
            if (publishItems && createdChildren.Count > 0)
            {
                ConsoleHelper.WriteInfo($"Publishing {createdChildren.Count} {typeName} item(s)...");

                foreach (var (child, _, _) in createdChildren)
                {
                    try
                    {
                        await PublishItemAsync(client, childCollection.ContentType, child);
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteWarning($"Failed to publish {typeName} item '{child.Id}': {ex.Message}. Item created but remains in draft.");
                    }
                }

                ConsoleHelper.WriteSuccess($"Published {createdChildren.Count} {typeName} item(s)");
            }

            // Brief delay between different child types
            if (collectionIndex < childCollections.Count - 1)
            {
                await Task.Delay(200);
            }
        }

        ConsoleHelper.WriteSuccess($"Finished processing all {childCollections.Count} child collection type(s) for parent '{parentId}'");
    }

    private static async Task CreateRelatedItemsAsync(IRestClient client, List<CreatedParentItem> createdParents, bool publishItems, TaxonomyProcessor taxonomyProcessor)
    {
        foreach (var parent in createdParents)
        {
            if (parent.RelatedCollections.Count > 0)
            {
                await CreateRelatedItemsForParentAsync(client, parent.ContentType, parent.Id, parent.RelatedCollections, publishItems, taxonomyProcessor);
            }
        }
    }

    // Creates items for every RelatedCollectionPlan, then wires each created item to the parent
    // via RelateItem using the named relationship field.  Recurses into child/related collections
    // on each newly created item so nesting can go arbitrarily deep.
    private static async Task CreateRelatedItemsForParentAsync(
        IRestClient client,
        string parentContentType,
        string parentId,
        List<RelatedCollectionPlan> relatedCollections,
        bool publishItems,
        TaxonomyProcessor taxonomyProcessor)
    {
        ConsoleHelper.WriteInfo($"Creating {relatedCollections.Count} related collection(s) for '{parentContentType}' item '{parentId}'...");

        for (int collectionIndex = 0; collectionIndex < relatedCollections.Count; collectionIndex++)
        {
            var relatedCollection = relatedCollections[collectionIndex];

            if (string.IsNullOrWhiteSpace(relatedCollection.ContentType))
            {
                ConsoleHelper.WriteWarning($"Skipping related collection '{relatedCollection.RelationFieldName}' — ContentType is empty.");
                continue;
            }

            var typeName = relatedCollection.ContentType.Split('.').Last();
            ConsoleHelper.WriteInfo($"[{collectionIndex + 1}/{relatedCollections.Count}] Creating {relatedCollection.Items.Count} '{typeName}' item(s) for relation '{relatedCollection.RelationFieldName}'...");

            var createdRelated = new List<(SdkItem Item, List<ChildCollectionPlan> Children, List<RelatedCollectionPlan> NestedRelated)>();

            // Pass 1: create each related item as draft (no ParentId injected)
            foreach (var itemPlan in relatedCollection.Items)
            {
                try
                {
                    var fields = (JsonObject)itemPlan.Fields.DeepClone();

                    if (itemPlan.TaxonFields.Count > 0)
                    {
                        var resolvedTaxa = await ResolveTaxaAsync(itemPlan.TaxonFields, taxonomyProcessor);
                        ApplyTaxaToFields(fields, resolvedTaxa);
                    }

                    var sdkItem = BuildSdkItem(fields);

                    // Dedup: check whether this related item already exists before creating
                    var existingRelated = await FindExistingItemAsync(client, relatedCollection.ContentType, fields);
                    SdkItem created;
                    if (existingRelated != null)
                    {
                        ConsoleHelper.WriteWarning($"Skipping creation of {typeName} related item — already exists with ID: {existingRelated.Id}");
                        created = existingRelated;
                    }
                    else
                    {
                        created = await client.CreateItem<SdkItem>(BuildCreateArgs(relatedCollection.ContentType, sdkItem, false));

                        if (created == null)
                        {
                            throw new InvalidOperationException($"CreateItem returned null for related type '{relatedCollection.ContentType}'.");
                        }

                        if (string.IsNullOrWhiteSpace(created.Id))
                        {
                            throw new InvalidOperationException($"Created related item returned empty ID for type '{relatedCollection.ContentType}'.");
                        }

                        ConsoleHelper.WriteSuccess($"Created related {typeName} item '{created.Id}'");
                    }

                    createdRelated.Add((created, itemPlan.ChildCollections, itemPlan.RelatedCollections));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed creating related item for type '{relatedCollection.ContentType}' (parent '{parentId}'). Payload: {itemPlan.Fields.ToJsonString()}",
                        ex);
                }
            }

            // Pass 2: wire each created item to the parent via the named relationship field
            ConsoleHelper.WriteInfo($"Relating {createdRelated.Count} '{typeName}' item(s) to '{parentId}' via '{relatedCollection.RelationFieldName}'...");
            foreach (var (relatedItem, _, _) in createdRelated)
            {
                try
                {
                    await client.RelateItem(new RelateArgs
                    {
                        Type = parentContentType,
                        Id = parentId,
                        RelationName = relatedCollection.RelationFieldName,
                        RelatedItemId = relatedItem.Id
                    });

                    ConsoleHelper.WriteSuccess($"Related '{typeName}' item '{relatedItem.Id}' to '{parentId}' via '{relatedCollection.RelationFieldName}'");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to relate '{typeName}' item '{relatedItem.Id}' to '{parentId}': {ex.Message}");
                    throw;
                }
            }

            // Pass 3: recurse into child/related collections on each newly created related item
            foreach (var (relatedItem, children, nestedRelated) in createdRelated)
            {
                if (children.Count > 0)
                {
                    await CreateChildItemsForParentAsync(client, relatedItem.Id, children, publishItems, taxonomyProcessor);
                }

                if (nestedRelated.Count > 0)
                {
                    await CreateRelatedItemsForParentAsync(client, relatedCollection.ContentType, relatedItem.Id, nestedRelated, publishItems, taxonomyProcessor);
                }
            }

            // Pass 4: publish if requested
            if (publishItems && createdRelated.Count > 0)
            {
                ConsoleHelper.WriteInfo($"Publishing {createdRelated.Count} '{typeName}' related item(s)...");
                foreach (var (relatedItem, _, _) in createdRelated)
                {
                    try
                    {
                        await PublishItemAsync(client, relatedCollection.ContentType, relatedItem);
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteWarning($"Failed to publish related {typeName} item '{relatedItem.Id}': {ex.Message}. Item created but remains in draft.");
                    }
                }
                ConsoleHelper.WriteSuccess($"Published {createdRelated.Count} '{typeName}' related item(s)");
            }

            if (collectionIndex < relatedCollections.Count - 1)
            {
                await Task.Delay(200);
            }
        }

        ConsoleHelper.WriteSuccess($"Finished processing all {relatedCollections.Count} related collection(s) for '{parentId}'");
    }

    // Resolves all TaxonFieldPlan entries on an item plan into a map of fieldName → Guid[].
    // Taxon IDs are obtained from the TaxonomyProcessor (resolve-or-create with caching).
    private static async Task<Dictionary<string, Guid[]>> ResolveTaxaAsync(
        List<TaxonFieldPlan> taxonFields,
        TaxonomyProcessor taxonomyProcessor)
    {
        var result = new Dictionary<string, Guid[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var plan in taxonFields)
        {
            try
            {
                var ids = await taxonomyProcessor.ResolveOrCreateTaxaAsync(plan.TaxonomyName, plan.TaxonTitles);
                if (ids.Length > 0)
                {
                    result[plan.FieldName] = ids;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed resolving taxa for field '{plan.FieldName}' in taxonomy '{plan.TaxonomyName}': {ex.Message}");
                throw;
            }
        }

        return result;
    }

    // Stamps resolved taxon Guid[] values onto the JsonObject fields before BuildSdkItem is called.
    private static void ApplyTaxaToFields(JsonObject fields, Dictionary<string, Guid[]> resolvedTaxa)
    {
        foreach (var (fieldName, ids) in resolvedTaxa)
        {
            var jsonArray = new JsonArray();
            foreach (var id in ids)
            {
                jsonArray.Add(id.ToString());
            }

            fields[fieldName] = jsonArray;
        }
    }

    // Queries Sitefinity for an existing item with the same Title (or UrlName as fallback).
    // Returns the first matching SdkItem, or null when no duplicate is found.
    private static async Task<SdkItem?> FindExistingItemAsync(IRestClient client, string contentType, JsonObject fields)
    {
        string? matchValue = null;
        string? matchField = null;

        if (fields.TryGetPropertyValue("Title", out var titleNode) && titleNode != null)
        {
            matchValue = titleNode.GetValue<string>()?.Trim();
            matchField = "Title";
        }
        else if (fields.TryGetPropertyValue("UrlName", out var urlNode) && urlNode != null)
        {
            matchValue = urlNode.GetValue<string>()?.Trim();
            matchField = "UrlName";
        }

        if (string.IsNullOrWhiteSpace(matchValue) || matchField == null)
        {
            return null;
        }

        try
        {
            var response = await client.GetItems<SdkItem>(new GetAllArgs
            {
                Type = contentType,
                Take = 1,
                Fields = new[] { "Id", "Title", "UrlName" },
                Filter = $"{matchField} = \"{matchValue}\""
            });

            return response?.Items?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Could not check for existing '{contentType}' by {matchField}='{matchValue}': {ex.Message}. Proceeding with creation.");
            return null;
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
        await ItemManagementExtensions.Publish(client, new PublishArgs(contentType, item.Id));
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

    private sealed record ImportPlan(List<TopLevelTypePlan> TopLevelTypes, bool HasChildren, bool HasRelated, bool HasTaxa);
    private sealed record TopLevelTypePlan(string ContentType, List<ImportItemPlan> Items);
    private sealed record ImportItemPlan(JsonObject Fields, List<ChildCollectionPlan> ChildCollections, List<RelatedCollectionPlan> RelatedCollections, List<TaxonFieldPlan> TaxonFields);
    private sealed record ChildCollectionPlan(string ContentType, List<ImportItemPlan> Items);
    // RelationFieldName = the Sitefinity relationship field name (text after "Related_" prefix)
    private sealed record RelatedCollectionPlan(string RelationFieldName, string ContentType, List<ImportItemPlan> Items);
    // FieldName = SdkItem field to set (text after "Taxon_" prefix); TaxonomyName = taxonomy to query/create within
    private sealed record TaxonFieldPlan(string FieldName, string TaxonomyName, List<string> TaxonTitles);
    // IsNew = false when the item already existed in Sitefinity (dedup); used to skip re-publishing
    private sealed record CreatedParentItem(string ContentType, string Id, List<ChildCollectionPlan> ChildCollections, List<RelatedCollectionPlan> RelatedCollections, bool IsNew = true);
}
