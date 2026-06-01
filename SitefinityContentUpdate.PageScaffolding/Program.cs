using Progress.Sitefinity.RestSdk;
using SitefinityContentUpdater.Core.Helpers;
using SitefinityContentUpdater.Core.RestClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSuccess("Sitefinity Page Scaffolding Tool");
        ConsoleHelper.WriteInfo("Creates content pages in Sitefinity from a comma-separated list.");

        try
        {
            var configuration = ConfigurationHelper.LoadConfiguration();
            var config = await ConfigurationHelper.GetSitefinityConfigAsync(configuration);

            var client = await ConnectToSiteAsync(config);
            if (client == null)
            {
                return;
            }

            var pageProcessor = new PageProcessor(client);

            // ----------------------------------------------------------------
            // 1. Ask for the page template name FIRST and validate it
            // ----------------------------------------------------------------
            var templateId = await ResolveTemplateAsync(pageProcessor, configuration);
            if (templateId == null)
            {
                return;
            }

            // ----------------------------------------------------------------
            // 2. Collect pages (comma-separated)
            // ----------------------------------------------------------------
            var pageDefinitions = CollectPageDefinitions(configuration);
            if (pageDefinitions.Count == 0)
            {
                ConsoleHelper.WriteError("No pages were entered. Exiting ...");
                return;
            }

            // ----------------------------------------------------------------
            // 3. Confirm
            // ----------------------------------------------------------------
            ConsoleHelper.WriteInfo($"\nAbout to create {pageDefinitions.Count} page(s):");
            foreach (var p in pageDefinitions)
            {
                var indent     = string.IsNullOrEmpty(p.ParentTitle) ? "  " : "    ";
                var parentNote = string.IsNullOrEmpty(p.ParentTitle) ? "" : $" [child of '{p.ParentTitle}']";
                ConsoleHelper.WriteInfo($"{indent}{p.Title}{parentNote}");
            }

            if (!ConsoleHelper.Confirm("\nProceed with page creation? (y/n)"))
            {
                ConsoleHelper.WriteInfo("Operation cancelled.");
                return;
            }

            await pageProcessor.ScaffoldPagesAsync(pageDefinitions, templateId.Value);

            ConsoleHelper.WriteSuccess("\nPage scaffolding completed.");
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

    // -------------------------------------------------------------------------
    // Template resolution
    // -------------------------------------------------------------------------

    private static async Task<Guid?> ResolveTemplateAsync(
        PageProcessor pageProcessor,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var templateNameFromConfig = configuration["PageScaffolding:TemplateName"];
        string templateName;

        if (!string.IsNullOrWhiteSpace(templateNameFromConfig))
        {
            ConsoleHelper.WriteInfo($"Using template name from config: {templateNameFromConfig}");
            templateName = templateNameFromConfig;
        }
        else
        {
            templateName = ConsoleHelper.ReadLine(
                "Enter the page template title to use for scaffolding (e.g. Bootstrap4 - Landing Page):") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(templateName))
        {
            ConsoleHelper.WriteError("Template name is required. Exiting ...");
            return null;
        }

        ConsoleHelper.WriteInfo($"Looking up template '{templateName}' in Sitefinity ...");
        var templateId = await pageProcessor.GetTemplateIdByNameAsync(templateName);

        if (templateId == null)
        {
            ConsoleHelper.WriteError($"Template '{templateName}' was not found in Sitefinity. Cannot continue without a valid template.");
            return null;
        }

        ConsoleHelper.WriteSuccess($"Template found — ID: {templateId}");
        return templateId;
    }

    // -------------------------------------------------------------------------
    // Page input — comma-separated
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads pages from appsettings ("PageScaffolding:Pages" array) or prompts the user for a
    /// comma-separated list. Each entry supports the optional parent/child notation:
    ///   About Us, About Us -> Contact, Services, Services -> Web Design
    /// </summary>
    private static List<PageDefinition> CollectPageDefinitions(
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var configPages = configuration.GetSection("PageScaffolding:Pages")
            .GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        List<string> rawEntries;

        if (configPages.Count > 0)
        {
            ConsoleHelper.WriteInfo($"Using {configPages.Count} page(s) from appsettings.");
            // Support both a pre-split array in appsettings and a single comma-separated entry.
            rawEntries = configPages
                .SelectMany(v => v!.Split(','))
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }
        else
        {
            ConsoleHelper.WriteInfo("\nEnter pages to scaffold as a comma-separated list.");
            ConsoleHelper.WriteInfo("For child pages use:  Parent -> Child");
            ConsoleHelper.WriteInfo("Example: Home, About Us, About Us -> Contact, Services, Services -> Web Design\n");

            var line = Console.ReadLine() ?? string.Empty;
            rawEntries = line
                .Split(',')
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }

        var definitions = new List<PageDefinition>();
        foreach (var entry in rawEntries)
        {
            var parts = entry.Split("->", 2);
            if (parts.Length == 2)
            {
                definitions.Add(new PageDefinition
                {
                    Title       = parts[1].Trim(),
                    ParentTitle = parts[0].Trim()
                });
            }
            else
            {
                definitions.Add(new PageDefinition
                {
                    Title = parts[0].Trim()
                });
            }
        }

        return definitions;
    }

    // -------------------------------------------------------------------------
    // Site connection
    // -------------------------------------------------------------------------

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
