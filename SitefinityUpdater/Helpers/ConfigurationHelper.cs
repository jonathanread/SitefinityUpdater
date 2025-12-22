using Microsoft.Extensions.Configuration;
using SitefinityUpdater.RestClient;

namespace SitefinityUpdater.Helpers
{
    internal class ConfigurationHelper
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
            var siteUrl = GetOrPromptForValue(
                configuration["Sitefinity:Url"],
                "Enter the Sitefinity site url (e.g. http://localhost:8080/api/default/):",
                "Sitefinity site url is required.",
                "Using site URL from config");

            var accessKey = GetOrPromptForValue(
                configuration["Sitefinity:AccessKey"],
                "Enter the Sitefinity access key:",
                "Sitefinity access key is required.",
                "Using access key from config",
                isSecret: true);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(siteUrl))
            {
                ConsoleHelper.WriteError("Sitefinity site url and access key are required to proceed. Exiting ...");
                throw new InvalidOperationException("Required configuration missing");
            }

            var siteIdString = GetOrPromptForValue(
                configuration["Sitefinity:SiteId"],
                "What is the site id you want to connect to?",
                "Site ID is required.",
                $"Using site ID from config");

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
