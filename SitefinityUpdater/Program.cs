// See https://aka.ms/new-console-template for more information
using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;
using SitefinityContentUpdater.Core.RestClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSuccess("Use this tool to update a content type rich text field");

        try
        {
            var configuration = ConfigurationHelper.LoadConfiguration();
            var config = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

            var client = await ConnectToSiteAsync(config);
            if (client == null)
            {
                return;
            }

            var contentType = ConsoleHelper.ReadLine("Enter the content type you want to update (e.g. newsitem or Telerik.Sitefinity.DynamicTypes.Model.News.NewsItem):");

            if (string.IsNullOrEmpty(contentType))
            {
                ConsoleHelper.WriteError("Content type is required. Exiting ...");
                return;
            }

            var fieldName = ConsoleHelper.ReadLine("Enter the field name to update (e.g. Content):");

            if (string.IsNullOrEmpty(fieldName))
            {
                ConsoleHelper.WriteError("Field name is required. Exiting ...");
                return;
            }

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

            var csvFilePath = ConfigurationHelper.GetCsvFilePath(configuration);
            var processor = new ContentProcessor(client, csvFilePath);
            await processor.UpdateContentAsync(contentType, fieldName, testMode);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"An error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                ConsoleHelper.WriteError($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task<IRestClient?> ConnectToSiteAsync(SitefinityConfig config)
    {
        var client = await RestClientFactory.GetRestClient(config);
        
        var validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId);

        if (!validationResult.IsValid)
        {
            if (validationResult.RequiresReconnect)
            {
                config.SiteId = validationResult.SiteId;
                client = await RestClientFactory.GetRestClient(config);
                
                validationResult = await SiteValidator.ValidateAndConfirmSiteAsync(client, config.SiteId);
                
                if (!validationResult.IsValid)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return client;
    }
}