using System.Collections.Concurrent;
using Progress.Sitefinity.Clients.Taxa;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;

namespace SitefinityContentUpdater.Core.Helpers
{
    /// <summary>
    /// Resolves taxon titles to Sitefinity taxon GUIDs, creating missing taxa on demand.
    /// Results are cached per taxonomy for the lifetime of the processor (one import run).
    /// </summary>
    public class TaxonomyProcessor
    {
        private readonly IRestClient _client;
        private readonly ITaxaClient _taxaClient;

        // Key: taxonomyName (case-insensitive) → dictionary of title (case-insensitive) → taxon GUID
        private readonly ConcurrentDictionary<string, Dictionary<string, Guid>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public TaxonomyProcessor(IRestClient client) : this(client, client.Taxa()) { }

        public TaxonomyProcessor(IRestClient client, ITaxaClient taxaClient)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _taxaClient = taxaClient ?? throw new ArgumentNullException(nameof(taxaClient));
        }

        /// <summary>
        /// Resolves (or creates) each taxon title in <paramref name="taxonTitles"/> within the
        /// named taxonomy and returns the ordered list of GUIDs suitable for setting on an SdkItem field.
        /// </summary>
        public async Task<Guid[]> ResolveOrCreateTaxaAsync(string taxonomyName, IEnumerable<string> taxonTitles)
        {
            if (string.IsNullOrWhiteSpace(taxonomyName))
                throw new ArgumentNullException(nameof(taxonomyName));

            var titles = taxonTitles?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
            if (titles.Count == 0)
                return [];

            await EnsureTaxonomyCacheLoadedAsync(taxonomyName);

            var result = new List<Guid>(titles.Count);
            foreach (var title in titles)
            {
                var id = await ResolveOrCreateTaxonAsync(taxonomyName, title.Trim());
                result.Add(id);
            }

            return [.. result];
        }

        /// <summary>
        /// Pre-warms the cache for all supplied taxonomy names in parallel.
        /// Call this once before the import loop starts to avoid per-item round-trips
        /// and to surface missing taxonomies early.
        /// </summary>
        public async Task PreWarmAsync(IEnumerable<string> taxonomyNames)
        {
            var names = taxonomyNames
                ?.Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            if (names.Count == 0)
                return;

            ConsoleHelper.WriteInfo($"Pre-warming taxonomy cache for {names.Count} taxonomy/taxonomies: {string.Join(", ", names)}");

            // Load all taxonomies concurrently — each gets its own isolated byTitle dict
            await Task.WhenAll(names.Select(LoadTaxonomyAsync));

            ConsoleHelper.WriteInfo("Taxonomy cache pre-warm complete.");
        }

        // ── private helpers ──────────────────────────────────────────────────────────

        private Task EnsureTaxonomyCacheLoadedAsync(string taxonomyName)
            => _cache.ContainsKey(taxonomyName) ? Task.CompletedTask : LoadTaxonomyAsync(taxonomyName);

        // Fetches ALL pages of an existing taxonomy into the cache.
        // Safe to call concurrently for different taxonomy names; each builds its own dict
        // and assigns it atomically at the end so there is no partial-read window.
        private async Task LoadTaxonomyAsync(string taxonomyName)
        {
            // Double-check inside the async path — another concurrent call may have already loaded it
            if (_cache.ContainsKey(taxonomyName))
                return;

            var byTitle = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            try
            {
                const int take = 200;
                var skip = 0;

                var page = await _client.GetItems<TaxonDto>(new GetAllArgs
                {
                    Type = taxonomyName,
                    Skip = skip,
                    Take = take,
                    Count = true,
                    Fields = ["Id", "Title"]
                });

                var total = page.TotalCount ?? 0;
                ConsoleHelper.WriteInfo($"Taxonomy '{taxonomyName}': found {total} existing taxon(s).");

                ProcessPage(page.Items, byTitle);
                skip += take;

                // Fetch remaining pages concurrently when there are multiple
                if (skip < total)
                {
                    var remainingPages = (int)Math.Ceiling((total - skip) / (double)take);
                    var pageTasks = Enumerable.Range(0, remainingPages).Select(i =>
                        _client.GetItems<TaxonDto>(new GetAllArgs
                        {
                            Type = taxonomyName,
                            Skip = skip + i * take,
                            Take = take,
                            Count = false,
                            Fields = ["Id", "Title"]
                        }));

                    var pages = await Task.WhenAll(pageTasks);
                    foreach (var p in pages)
                        ProcessPage(p.Items, byTitle);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteWarning($"Could not load existing taxa for '{taxonomyName}': {ex.Message}. Will create new taxa as needed.");
            }

            // Assign atomically — concurrent callers for the same name will see either
            // the empty state or the fully-populated dict, never a partial one.
            _cache.TryAdd(taxonomyName, byTitle);
        }

        private static void ProcessPage(IEnumerable<TaxonDto> items, Dictionary<string, Guid> byTitle)
        {
            foreach (var taxon in items)
            {
                if (string.IsNullOrWhiteSpace(taxon.Title) || string.IsNullOrWhiteSpace(taxon.Id))
                    continue;

                if (Guid.TryParse(taxon.Id, out var guid))
                    byTitle[taxon.Title] = guid;
            }
        }

        private async Task<Guid> ResolveOrCreateTaxonAsync(string taxonomyName, string title)
        {
            var byTitle = _cache[taxonomyName];

            if (byTitle.TryGetValue(title, out var existing))
            {
                ConsoleHelper.WriteInfo($"Taxon '{title}' in '{taxonomyName}': resolved from cache (ID {existing}).");
                return existing;
            }

            // Not found — create it
            var newTaxon = new TaxonDto { Title = title };
            TaxonDto created;

            try
            {
                created = await _taxaClient.CreateTaxon<TaxonDto>(newTaxon, taxonomyName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create taxon '{title}' in taxonomy '{taxonomyName}': {ex.Message}", ex);
            }

            if (created == null || string.IsNullOrWhiteSpace(created.Id))
                throw new InvalidOperationException($"CreateTaxon returned null or empty ID for '{title}' in '{taxonomyName}'.");

            if (!Guid.TryParse(created.Id, out var newGuid))
                throw new InvalidOperationException($"CreateTaxon returned non-GUID ID '{created.Id}' for '{title}' in '{taxonomyName}'.");

            byTitle[title] = newGuid;
            ConsoleHelper.WriteSuccess($"Created taxon '{title}' in '{taxonomyName}' (ID {newGuid}).");
            return newGuid;
        }
    }
}
