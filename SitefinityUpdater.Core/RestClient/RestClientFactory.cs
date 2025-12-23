using Progress.Sitefinity.RestSdk;

namespace SitefinityContentUpdater.Core.RestClient
{
    public class RestClientFactory
    {
        public static async Task<IRestClient> GetRestClient(SitefinityConfig config)
        {
            if(config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Url = config.Url.EndsWith('/') ? config.Url : config.Url + "/";

            var httpClient = await CreateClient(config);
            var restClient = new Progress.Sitefinity.RestSdk.Client.RestClient(httpClient);
            await restClient.Init(new RequestArgs() { AdditionalQueryParams = new Dictionary<string, string> { { "sf_site", config.SiteId.ToString() } } });
            return restClient;
        }

        private static async Task<HttpClient> CreateClient(SitefinityConfig config)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(config.Url);

            client.DefaultRequestHeaders.Add("X-SF-Access-Key", $"{config.AccessKey}");

            return client;
        }
    }
}
