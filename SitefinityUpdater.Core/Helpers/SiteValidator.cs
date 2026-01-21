using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;

namespace SitefinityContentUpdater.Core.Helpers
{
    public class SiteValidator
    {
        public static async Task<SiteValidationResult> ValidateAndConfirmSiteAsync(IRestClient client, Guid currentSiteId, string siteName = "")
        {
            var siteLabel = string.IsNullOrEmpty(siteName) ? "Sitefinity site" : $"{siteName} site";
            
            var site = await client.Sites().GetCurrentSite();
            
            if (site == null)
            {
                ConsoleHelper.WriteError($"Failed to connect to {siteLabel}. Please check the site url and access key and try again.");
                return new SiteValidationResult { IsValid = false, SiteId = currentSiteId };
            }

            ConsoleHelper.WriteSuccess($"Successfully connected to {siteLabel}: {site.Name}");

            if (!ConsoleHelper.Confirm($"Is this the correct {siteLabel}? (y/n)"))
            {
                var newSiteId = ConsoleHelper.ReadLine($"Enter the site ID for the {siteLabel} you want to connect to:");
                
                if (!string.IsNullOrEmpty(newSiteId) && Guid.TryParse(newSiteId, out var parsedSiteId))
                {
                    return new SiteValidationResult { IsValid = false, SiteId = parsedSiteId, RequiresReconnect = true };
                }
                else
                {
                    ConsoleHelper.WriteError("Invalid site ID provided.");
                    return new SiteValidationResult { IsValid = false, SiteId = currentSiteId };
                }
            }

            ConsoleHelper.WriteSuccess($"Confirmed {siteLabel}. Proceeding...");
            return new SiteValidationResult { IsValid = true, SiteId = currentSiteId };
        }
    }

    public class SiteValidationResult
    {
        public bool IsValid { get; set; }
        public Guid SiteId { get; set; }
        public bool RequiresReconnect { get; set; }
    }
}
