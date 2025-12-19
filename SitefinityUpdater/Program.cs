// See https://aka.ms/new-console-template for more information
using AngleSharp;
using AngleSharp.Html.Parser;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;
using Progress.Sitefinity.RestSdk.OData;
using SitefinityUpdater.RestClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Use this tool to update a content type rich text field");
        Console.ResetColor();

        Console.WriteLine("Enter the Sitefinity site url (e.g. http://localhost:8080/api/default/):");
        string siteUrl = Console.ReadLine(); // your sitefinity site url

        if (string.IsNullOrEmpty(siteUrl))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Sitefinity site url is required.");
            Console.ResetColor();

            Console.WriteLine("Enter the Sitefinity site url (e.g. http://localhost:8080/api/default/):");
            siteUrl = Console.ReadLine();
        }

        Console.WriteLine("Enter the Sitefinity access key:");
        string accessKey = Console.ReadLine(); // your sitefinity access key

        if (string.IsNullOrEmpty(accessKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Sitefinity access key is required.");
            Console.ResetColor();

            Console.WriteLine("Enter the Sitefinity access key:");
            accessKey = Console.ReadLine();
        }

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(siteUrl))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Sitefinity site url and access key are required to proceed. Exiting ...");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("What is the site id you want to connect to?");
        var siteId = Console.ReadLine();

        var config = new SitefinityConfig()
        {
            Url = siteUrl,
            AccessKey = accessKey,
            SiteId = Guid.Parse(siteId)
        };

        var client = await RestClientFactory.GetRestClient(config);
        var site = await client.Sites().GetCurrentSite();
        if (site == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to connect to Sitefinity site. Please check the site url and access key and try again.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Successfully connected to Sitefinity site: {site.Name}");
        Console.ResetColor();

        Console.WriteLine("Is this the correct site? y/n");
        var correctSite = Console.ReadLine();

        if (correctSite?.ToLower() != "y")
        {
            Console.WriteLine("What is the site id you want to connect to?");
            siteId = Console.ReadLine();
            config.SiteId = Guid.Parse(siteId);
            client = await RestClientFactory.GetRestClient(config);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Proceeding with the update...");
            Console.ResetColor();
        }
        
        Console.WriteLine("Enter the content type you want to update (e.g. newsitem or Telerik.Sitefinity.DynamicTypes.Model.News.NewsItem):");
        string contentType = Console.ReadLine();

        if (string.IsNullOrEmpty(contentType))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Content type is required. Exiting ...");
            Console.ResetColor();
        }
       
        object message = await UpdateContentAsync(client, contentType);     

        var odataClient = client as IODataRestClient;
    }

    // Change UpdateContentAsync to static, since it is called from a static context
    static async Task<string> UpdateContentAsync(IRestClient client, string contentType)
    {
        string message = string.Empty;
        if (client == null || contentType == null)
        {
            throw new ArgumentNullException("Connection lost or content type missing");
        }
        var skip = 0;
        var take = 50;

        List<string> fields = new() { "Content", "Id" };
        var contentResponse = await client.GetItems<SdkItem>(new GetAllArgs()
        {
            Type = contentType,
            Skip = skip,
            Take = take,
            Count = true,
            Fields = fields
        });

        while (skip < contentResponse.TotalCount)
        {
            await DoContentWorkAsync(contentResponse.Items, client);
            skip += take;
            contentResponse = await client.GetItems<SdkItem>(new GetAllArgs()
            {
                Type = contentType,
                Skip = skip,
                Take = take,
                Count = true,
                Fields = fields
            });
        }

        return message;
    }

    // Change DoContentWorkAsync to static as well, since it is only called from static context
    private static async Task DoContentWorkAsync(IList<SdkItem> items, IRestClient client)
    {
        var parser = new HtmlParser();

        //Can i improve chattyness by getting all the images from all items first?
        foreach (var item in items)
        {
            var content = item.GetValue<string>("Content");

            if (string.IsNullOrEmpty(content))
            {
                continue;
            }

            var document = await parser.ParseDocumentAsync(content);

            //Get all the images in the content
            var imgs = document.Images;
            
            if (imgs != null && imgs.Length > 0)
            {
                //Get the titles of the images to lookup new urls where there is an sfref attribute
                var imgTitles = imgs.Where(i => !string.IsNullOrWhiteSpace(i.GetAttribute("sfref"))).Select(i => i.Title);
                
                var images = await GetImages(imgTitles, client);

                foreach (var img in imgs)
                {
                    var src = img.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src))
                    {                      
                        var newSrc = images.FirstOrDefault(i => i.Title == img.Title)?.ItemDefaultUrl;
                        img.SetAttribute("src", newSrc);
                        img.RemoveAttribute("sfref");
                    }
                }
            }
        }
    }

    private static async Task<IList<ImageDto>> GetImages(IEnumerable<string> imgTitles, IRestClient client)
    {
        var iamges =  await client.GetItems<ImageDto>(new GetAllArgs()
        {
            Type = RestClientContentTypes.Images,
            Filter = $"Title in ({string.Join(",", imgTitles.Select(t => $"'{t}'"))})",
            Take = imgTitles.Count()
        });

        return iamges.Items;    
    }
}