using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;
using SitefinityContentUpdater.Core.RestClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSuccess("Sitefinity Relationship Builder");
        ConsoleHelper.WriteInfo("Use this tool to sync relationships between content items from a source site to a target site");
        Console.WriteLine();

        try
        {
            var configuration = ConfigurationHelper.LoadConfiguration();

            // Configure and connect to source site
            ConsoleHelper.WriteInfo("=== Source Site Configuration ===");
            var sourceConfig = await ConfigurationHelper.GetSourceSitefinityConfigAsync(configuration);
            var sourceClient = await ConnectToSiteAsync(sourceConfig, "Source");
            if (sourceClient == null)
            {
                return;
            }

            Console.WriteLine();

            // Configure and connect to target site
            ConsoleHelper.WriteInfo("=== Target Site Configuration ===");
            var targetConfig = await ConfigurationHelper.GetTargetSitefinityConfigAsync(configuration);
            var targetClient = await ConnectToSiteAsync(targetConfig, "Target");
            if (targetClient == null)
            {
                return;
            }

            Console.WriteLine();

            var contentType = ConsoleHelper.ReadLine("Enter the content type (e.g. newsitems or Telerik.Sitefinity.DynamicTypes.Model.News.NewsItems):");

            if (string.IsNullOrEmpty(contentType))
            {
                ConsoleHelper.WriteError("Content type is required. Exiting ...");
                return;
            }

            var relationshipFieldNamesInput = ConsoleHelper.ReadLine("Enter the relationship field names separated by commas (e.g. RelatedNews,RelatedImages,Tags):");

            if (string.IsNullOrEmpty(relationshipFieldNamesInput))
            {
                ConsoleHelper.WriteError("At least one relationship field name is required. Exiting ...");
                return;
            }

            var relationshipFieldNames = relationshipFieldNamesInput
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (relationshipFieldNames.Count == 0)
            {
                ConsoleHelper.WriteError("At least one valid relationship field name is required. Exiting ...");
                return;
            }

            ConsoleHelper.WriteInfo($"Relationship fields to sync: {string.Join(", ", relationshipFieldNames)}");

            var testMode = ConsoleHelper.Confirm("Do you want to test on 1 item first? (y/n)");

            if (testMode)
            {
                ConsoleHelper.WriteInfo("Running in TEST MODE - Only 1 item will be processed.");
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

            Console.WriteLine();

            var processor = new RelationshipProcessor(sourceClient, targetClient);
            await processor.BuildRelationshipsAsync(contentType, relationshipFieldNames, testMode);

            Console.WriteLine();
            ConsoleHelper.WriteSuccess("Process completed. Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                ConsoleHelper.WriteError($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    private static async Task<IRestClient?> ConnectToSiteAsync(SitefinityConfig config, string siteName)
    {
        ConsoleHelper.WriteInfo($"Connecting to {siteName} site...");

        var client = await RestClientFactory.GetRestClient(config);

        var validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId, siteName);

        if (!validationResult.IsValid)
        {
            if (validationResult.RequiresReconnect)
            {
                config.SiteId = validationResult.SiteId;
                client = await RestClientFactory.GetRestClient(config);

                validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId, siteName);

                if (!validationResult.IsValid)
                {
                    ConsoleHelper.WriteError($"Failed to connect to {siteName} site.");
                    return null;
                }
            }
            else
            {
                ConsoleHelper.WriteError($"Failed to connect to {siteName} site.");
                return null;
            }
        }

        ConsoleHelper.WriteSuccess($"Connected to {siteName} site successfully.");
        return client;
    }
}
