using Progress.Sitefinity.RestSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SitefinityUpdater.RestClient
{
    internal class RestClientFactory
    {
        internal static async Task<IRestClient> GetRestClient(SitefinityConfig config)
        {
            if(config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Url = config.Url.EndsWith('/') ? config.Url : config.Url + "/";

            // create the http client that holds the Bearer Token
            var httpClient = await CreateClient(config);
            // create the RestClient that is resposible for creating the items in the CMS
            var restClient = new Progress.Sitefinity.RestSdk.Client.RestClient(httpClient);
            // initialize the client
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
