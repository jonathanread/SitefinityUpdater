using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Clients.LayoutEditor;
using Progress.Sitefinity.RestSdk.Dto;
using Progress.Sitefinity.RestSdk.Filters;
using Progress.Sitefinity.RestSdk.Management;

namespace SitefinityContentUpdater.Core.Helpers
{
    /// <summary>
    /// Handles scaffolding of Sitefinity pages exclusively via the REST SDK.
    /// </summary>
    public class PageProcessor
    {
        private readonly IRestClient _client;

        public PageProcessor(IRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // -----------------------------------------------------------------------
        // Template lookup
        // -----------------------------------------------------------------------

        /// <summary>
        /// Looks up a page template by its Title via the REST SDK using
        /// <see cref="RestClientContentTypes.PageTemplates"/> and a typed
        /// <see cref="FilterClause"/> for an exact-match title filter.
        /// Returns <see langword="null"/> when no matching template is found.
        /// </summary>
        public async Task<Guid?> GetTemplateIdByNameAsync(string templateName)
        {
            var results = await _client.GetItems<SdkItem>(new GetAllArgs
            {
                Type   = RestClientContentTypes.PageTemplates,
                Take   = 1,
                Fields = ["Id", "Title"],
                Filter = new FilterClause
                {
                    FieldName  = "Title",
                    FieldValue = templateName,
                    Operator   = FilterClause.Operators.Equal
                }
            });

            var item = results.Items.FirstOrDefault();
            return item is not null && Guid.TryParse(item.Id, out var id) ? id : null;
        }

        // -----------------------------------------------------------------------
        // Page lookup
        // -----------------------------------------------------------------------

        /// <summary>
        /// Looks up an existing Sitefinity page by its Title via the REST SDK using
        /// <see cref="RestClientContentTypes.Pages"/> and a typed <see cref="FilterClause"/>.
        /// Returns <see langword="null"/> when no matching page is found.
        /// </summary>
        public async Task<Guid?> GetPageIdByTitleAsync(string title)
        {
            var results = await _client.GetItems<SdkItem>(new GetAllArgs
            {
                Type   = RestClientContentTypes.Pages,
                Take   = 1,
                Fields = ["Id", "Title"],
                Filter = new FilterClause
                {
                    FieldName  = "Title",
                    FieldValue = title,
                    Operator   = FilterClause.Operators.Equal
                }
            });

            var item = results.Items.FirstOrDefault();
            return item is not null && Guid.TryParse(item.Id, out var id) ? id : null;
        }

        // -----------------------------------------------------------------------
        // Scaffolding orchestration
        // -----------------------------------------------------------------------

        /// <summary>
        /// Scaffolds the supplied page definitions, creating each as a Sitefinity content page
        /// with the page title rendered as an &lt;h1&gt; in the page content.
        /// When a child page references a parent that was not part of the input list, the method
        /// attempts to look it up in Sitefinity; if still not found the user is asked whether
        /// they want it created before proceeding.
        /// </summary>
        public async Task ScaffoldPagesAsync(IList<PageDefinition> pageDefinitions, Guid templateId)
        {
            var knownPages = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var page in pageDefinitions)
            {
                Guid? parentId = null;

                if (!string.IsNullOrEmpty(page.ParentTitle))
                {
                    parentId = await ResolveParentIdAsync(page.ParentTitle, templateId, knownPages);

                    if (parentId == null)
                    {
                        ConsoleHelper.WriteError($"  Skipping '{page.Title}' — parent '{page.ParentTitle}' could not be resolved.");
                        continue;
                    }
                }

                ConsoleHelper.WriteInfo($"  Creating page: '{page.Title}'" +
                    (parentId.HasValue ? $" (child of '{page.ParentTitle}')" : ""));

                try
                {
                    var pageId = await CreatePageAsync(page.Title, templateId, parentId);
                    knownPages[page.Title] = pageId;
                    ConsoleHelper.WriteSuccess($"    Created '{page.Title}' — ID: {pageId}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"    Failed to create '{page.Title}': {ex.Message}");
                    if (ex.InnerException is not null)
                        ConsoleHelper.WriteError($"      Inner: {ex.InnerException.Message}");
                }
            }
        }

        private async Task<Guid?> ResolveParentIdAsync(
            string parentTitle,
            Guid templateId,
            Dictionary<string, Guid> knownPages)
        {
            if (knownPages.TryGetValue(parentTitle, out var cachedId))
                return cachedId;

            ConsoleHelper.WriteInfo($"  Looking up parent page '{parentTitle}' in Sitefinity ...");
            var existingId = await GetPageIdByTitleAsync(parentTitle);
            if (existingId.HasValue)
            {
                ConsoleHelper.WriteSuccess($"  Found existing parent page '{parentTitle}' — ID: {existingId.Value}");
                knownPages[parentTitle] = existingId.Value;
                return existingId.Value;
            }

            ConsoleHelper.WriteWarning($"  Parent page '{parentTitle}' does not exist in Sitefinity.");
            if (!ConsoleHelper.Confirm($"  Would you like to create '{parentTitle}' as a root page now? (y/n)"))
                return null;

            try
            {
                var newParentId = await CreatePageAsync(parentTitle, templateId, parentId: null);
                knownPages[parentTitle] = newParentId;
                ConsoleHelper.WriteSuccess($"  Created parent page '{parentTitle}' — ID: {newParentId}");
                return newParentId;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"  Failed to create parent page '{parentTitle}': {ex.Message}");
                if (ex.InnerException is not null)
                    ConsoleHelper.WriteError($"    Inner: {ex.InnerException.Message}");
                return null;
            }
        }

        // -----------------------------------------------------------------------
        // Page creation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a single Sitefinity page via the REST SDK using
        /// <see cref="RestClientContentTypes.Pages"/>, adds an &lt;h1&gt; content block
        /// to the <c>Content</c> placeholder via <c>client.Pages().CreateWidget()</c>,
        /// publishes it via <see cref="ItemManagementExtensions.Publish"/>, and returns its new ID.
        /// </summary>
        public async Task<Guid> CreatePageAsync(string title, Guid templateId, Guid? parentId = null)
        {
            var urlName = ToUrlName(title);

            var data = new Dictionary<string, object>
            {
                ["Title"]            = title,
                ["Name"]             = urlName,
                ["UrlName"]          = urlName,
                ["ShowInNavigation"] = true,
                ["TemplateId"]       = templateId.ToString()
            };

            if (parentId.HasValue)
                data["ParentId"] = parentId.Value.ToString();

            var created = await _client.CreateItem<SdkItem>(new CreateArgs
            {
                Type = RestClientContentTypes.Pages,
                Data = data
            });

            if (!Guid.TryParse(created.Id, out var pageId))
                throw new InvalidOperationException("Page creation response did not include a valid Id.");

            await AddH1ContentBlockAsync(created, title);
            await PublishPageAsync(created);

            return pageId;
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Adds a content block widget containing an &lt;h1&gt; with the page title to the
        /// <c>Content</c> placeholder using <c>_client.Pages().CreateWidget()</c>.
        /// </summary>
        private async Task AddH1ContentBlockAsync(SdkItem page, string title)
        {
            try
            {
                var h1Html = $"<h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>";

                var pagesClient = _client.Pages();

                await pagesClient.Lock(new LockArgs(
                    itemType: RestClientContentTypes.Pages,
                    itemId: page.Id,
                    version: 0
                ));

                await pagesClient.CreateWidget(new AddWidgetArgs
                {
                    Name            = "SitefinityContentBlock",
                    PlaceholderName = "Content",
                    Id              = page.Id,
                    Type            = RestClientContentTypes.Pages,
                    Properties      = new Dictionary<string, string>
                    {
                        ["Content"] = h1Html
                    }
                });
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"    [AddContentBlock] Failed for page '{title}' (ID: {page.Id})");
                ConsoleHelper.WriteError($"      Message: {ex.Message}");
                if (ex.InnerException is not null)
                    ConsoleHelper.WriteError($"      Inner:   {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Publishes a Sitefinity page using the SDK's
        /// <see cref="ItemManagementExtensions.Publish"/> extension on <see cref="IRestClient"/>.
        /// </summary>
        private async Task PublishPageAsync(SdkItem page)
        {
            try
            {
                await _client.Publish(new PublishArgs(
                    itemType: RestClientContentTypes.Pages,
                    id: page.Id
                ));
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"    [Publish] Failed for page ID: {page.Id}");
                ConsoleHelper.WriteError($"      Message: {ex.Message}");
                if (ex.InnerException is not null)
                    ConsoleHelper.WriteError($"      Inner:   {ex.InnerException.Message}");
            }
        }

        private static string ToUrlName(string title) =>
            System.Text.RegularExpressions.Regex
                .Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
                .Trim('-');
    }

    /// <summary>
    /// Represents a page to scaffold.
    /// </summary>
    public class PageDefinition
    {
        /// <summary>The display title of the page.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The title of the parent page, if this is a child page.
        /// <c>null</c> or empty means a root-level page.
        /// </summary>
        public string? ParentTitle { get; set; }
    }
}
