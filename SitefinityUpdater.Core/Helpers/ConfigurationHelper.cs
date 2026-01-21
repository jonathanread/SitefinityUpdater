using Microsoft.Extensions.Configuration;
using SitefinityContentUpdater.Core.RestClient;

namespace SitefinityContentUpdater.Core.Helpers
{
    public class ConfigurationHelper
    {
        public static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public static async Task<SitefinityConfig> GetSitefinityConfigAsync(IConfiguration configuration)
        {
            return await GetSitefinityConfigAsync(configuration, "Sitefinity", "Target Site");
        }

        public static async Task<SitefinityConfig> GetSourceSitefinityConfigAsync(IConfiguration configuration)
        {
            return await GetSitefinityConfigAsync(configuration, "SourceSite", "Source Site");
        }

        public static async Task<SitefinityConfig> GetTargetSitefinityConfigAsync(IConfiguration configuration)
        {
            return await GetSitefinityConfigAsync(configuration, "TargetSite", "Target Site");
        }

        private static async Task<SitefinityConfig> GetSitefinityConfigAsync(
            IConfiguration configuration,
            string sectionName,
            string displayName)
        {
            ConsoleHelper.WriteInfo($"Configuring {displayName}...");

            var siteUrl = GetOrPromptForValue(
                configuration[$"{sectionName}:Url"],
                $"Enter the {displayName} Sitefinity site url (e.g. http://localhost:8080/api/default/):",
                $"{displayName} site url is required.",
                $"Using {displayName} URL from config");

            var accessKey = GetOrPromptForValue(
                configuration[$"{sectionName}:AccessKey"],
                $"Enter the {displayName} Sitefinity access key:",
                $"{displayName} access key is required.",
                $"Using {displayName} access key from config",
                isSecret: true);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(siteUrl))
            {
                ConsoleHelper.WriteError($"{displayName} site url and access key are required to proceed. Exiting ...");
                throw new InvalidOperationException($"Required {displayName} configuration missing");
            }

            var siteIdString = GetOrPromptForValue(
                configuration[$"{sectionName}:SiteId"],
                $"What is the {displayName} site id you want to connect to?",
                $"{displayName} Site ID is required.",
                $"Using {displayName} Site ID from config");

            return new SitefinityConfig
            {
                Url = siteUrl,
                AccessKey = accessKey,
                SiteId = Guid.Parse(siteIdString)
            };
        }

        public static string GetCsvFilePath(IConfiguration configuration)
        {
            var csvFileName = configuration["CsvFilePath"] ?? "image_mappings_20251214_073902.csv";
            var baseDirectory = AppContext.BaseDirectory;
            var csvFilePath = Path.Combine(baseDirectory, csvFileName);
            
            ConsoleHelper.WriteInfo($"CSV file path configured as: {csvFilePath}");
            
            return csvFilePath;
        }

        private static string GetOrPromptForValue(
            string? configValue,
            string promptMessage,
            string errorMessage,
            string configFoundMessage,
            bool isSecret = false)
        {
            if (!string.IsNullOrEmpty(configValue))
            {
                var displayValue = isSecret ? configFoundMessage : $"{configFoundMessage}: {configValue}";
                ConsoleHelper.WriteInfo(displayValue);
                return configValue;
            }

            Console.WriteLine(promptMessage);
            var value = Console.ReadLine();

            if (string.IsNullOrEmpty(value))
            {
                ConsoleHelper.WriteError(errorMessage);
                Console.WriteLine(promptMessage);
                value = Console.ReadLine();
            }

            return value ?? string.Empty;
        }
    }
}
