using AngleSharp.Html.Parser;
using Progress.Sitefinity.RestSdk;
using Progress.Sitefinity.RestSdk.Dto;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.Net.Security;

namespace SitefinityUpdater.Helpers
{
    internal class ContentProcessor
    {
        private readonly IRestClient _client;
        private readonly HtmlParser _parser;
        private readonly string _csvFilePath;

        public ContentProcessor(IRestClient client, string csvFilePath)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _parser = new HtmlParser();
            _csvFilePath = csvFilePath ?? throw new ArgumentNullException(nameof(csvFilePath));
        }

        public async Task<string> UpdateContentAsync(string contentType, string fieldName, bool testMode = false)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            var skip = 0;
            var take = testMode ? 1 : 50;
            var fields = new List<string> { fieldName, "Id" };

            var contentResponse = await _client.GetItems<SdkItem>(new GetAllArgs()
            {
                Type = contentType,
                Skip = skip,
                Take = take,
                Count = true,
                Fields = fields
            });

            if (testMode)
            {
                ConsoleHelper.WriteInfo($"TEST MODE: Found {contentResponse.TotalCount} total items. Processing only 1 item.");
            }
            else
            {
                ConsoleHelper.WriteInfo($"Found {contentResponse.TotalCount} items to process.");
            }

            var totalProcessed = 0;
            var totalUpdated = 0;

            while (skip < contentResponse.TotalCount)
            {
                var result = await ProcessBatchAsync(contentResponse.Items, fieldName, contentType);
                totalProcessed += result.ProcessedCount;
                totalUpdated += result.UpdatedCount;

                if (testMode)
                {
                    break;
                }

                skip += take;

                if (skip < contentResponse.TotalCount)
                {
                    contentResponse = await _client.GetItems<SdkItem>(new GetAllArgs()
                    {
                        Type = contentType,
                        Skip = skip,
                        Take = take,
                        Count = true,
                        Fields = fields
                    });
                }
            }

            if (testMode)
            {
                ConsoleHelper.WriteSuccess($"TEST MODE COMPLETED: Processed {totalProcessed} item(s), Updated {totalUpdated} item(s).");
                ConsoleHelper.WriteInfo("Review the results above. Run again without test mode to process all items.");
            }
            else
            {
                ConsoleHelper.WriteSuccess($"Content update completed successfully. Processed {totalProcessed} items, Updated {totalUpdated} items.");
            }

            return "Update completed";
        }

        private async Task<ProcessingResult> ProcessBatchAsync(IList<SdkItem> items, string fieldName, string contentType)
        {
            var itemsToUpdate = new List<SdkItem>();
            var processedCount = 0;
            var mappings = File.Exists(_csvFilePath) ? LoadImageMappings(_csvFilePath) : new List<ImageMapping>();

            foreach (var item in items)
            {
                processedCount++;
                var content = item.GetValue<string>(fieldName);

                if (string.IsNullOrEmpty(content))
                {
                    ConsoleHelper.WriteWarning($"Item {item.Id}: Field '{fieldName}' is empty or null. Skipping.");
                    continue;
                }

                var document = await _parser.ParseDocumentAsync(content);
                var imgsFromDOM = document.Images;

                if (imgsFromDOM != null && imgsFromDOM.Length > 0)
                {
                    var imgDetails = imgsFromDOM
                        .Select(i => new ImgDetail { Title = i.Title ?? i.AlternativeText, Id = GetGuidFromSrc(i.GetAttribute("src")) })
                        .ToList();

                    if (imgDetails.Any())
                    {
                        ConsoleHelper.WriteInfo($"Item {item.Id}: Found {imgDetails.Count} image(s).");

                        var images = await GetImagesAsync(imgDetails);
                        bool itemModified = false;
                        var updatedImageCount = 0;

                        foreach (var img in imgsFromDOM)
                        {
                            var src = img.GetAttribute("src");
                            var imgTitle = img.Title ?? img.AlternativeText;
                            var imgId = GetGuidFromSrc(src);
                            
                            ImageDto sfImg = null;
                            
                            if (imgId.HasValue && mappings.Any())
                            {
                                var mapping = mappings.FirstOrDefault(m => m.SourceId == imgId.Value);
                                if (mapping != null && mapping.TargetId.HasValue)
                                {
                                    sfImg = images.FirstOrDefault(i => i.Id == mapping.TargetId.Value.ToString());
                                    if (sfImg != null)
                                    {
                                        ConsoleHelper.WriteInfo($"  Mapped source ID {imgId.Value} to target ID {mapping.TargetId.Value}");
                                    }
                                }
                            }
                            
                            if (sfImg == null)
                            {
                                sfImg = images.Count == 1 ? images[0] : images.FirstOrDefault(i => 
                                    (i.Title == imgTitle && !string.IsNullOrWhiteSpace(imgTitle)) || 
                                    (imgId.HasValue && i.Id == imgId.Value.ToString()));
                            }

                            if (!string.IsNullOrEmpty(src) && sfImg != null)
                            {
                                var newSrc = sfImg.Url;
                                img.SetAttribute("src", newSrc);
                                img.RemoveAttribute("sfref");
                                itemModified = true;
                                updatedImageCount++;
                            }
                        }

                        if (itemModified)
                        {
                            item.SetValue(fieldName, document.DocumentElement.OuterHtml);
                            itemsToUpdate.Add(item);
                            ConsoleHelper.WriteSuccess($"Item {item.Id}: Updated {updatedImageCount} image(s). Marked for update.");
                        }
                    }
                }
                else
                {
                    ConsoleHelper.WriteWarning($"Item {item.Id}: No images found in field '{fieldName}'.");
                }
            }

            var updatedCount = 0;
            if (itemsToUpdate.Count > 0)
            {
                await BatchUpdateItemsAsync(itemsToUpdate, contentType);
                updatedCount = itemsToUpdate.Count;
            }

            return new ProcessingResult
            {
                ProcessedCount = processedCount,
                UpdatedCount = updatedCount
            };
        }

        private Guid? GetGuidFromSrc(string src)
        {
            var pattern = @"Item with ID: '([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})'";
            var match = Regex.Match(src, pattern);
            if (match.Success)
            {
                return Guid.Parse(match.Groups[1].Value); // "1eb77cf8-a54a-471b-917b-818adef56ce7"
            }

            return null;
        }

        private async Task BatchUpdateItemsAsync(IList<SdkItem> items, string contentType)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            try
            {
                var updateTasks = items.Select(item => _client.UpdateItem(new UpdateArgs()
                {
                    Type = contentType,
                    Data = item
                }));
                await Task.WhenAll(updateTasks);

                ConsoleHelper.WriteSuccess($"Batch update completed. Updated {items.Count} items.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error during batch update: {ex.Message}");
                throw;
            }
        }

        private async Task<IList<ImageDto>> GetImagesAsync(IEnumerable<ImgDetail> imgDetails)
        {
            var imgDetailsList = imgDetails.ToList();
            
            if (!File.Exists(_csvFilePath))
            {
                ConsoleHelper.WriteWarning($"CSV file not found at: {_csvFilePath}. Proceeding without ID mapping.");
                return await GetImagesByTitleAsync(imgDetailsList);
            }

            var mappings = LoadImageMappings(_csvFilePath);
            var targetIds = new List<Guid>();
            var titles = new List<string>();

            foreach (var imgDetail in imgDetailsList)
            {
                if (imgDetail.Id.HasValue)
                {
                    var mapping = mappings.FirstOrDefault(m => m.SourceId == imgDetail.Id.Value);
                    if (mapping != null && mapping.TargetId.HasValue)
                    {
                        targetIds.Add(mapping.TargetId.Value);
                      }
                }
                
                if (!string.IsNullOrWhiteSpace(imgDetail.Title))
                {
                    titles.Add(imgDetail.Title);
                }
            }

            return await GetImagesByTitleOrIdAsync(titles, targetIds);
        }

        private List<ImageMapping> LoadImageMappings(string csvFilePath)
        {
            try
            {
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });
                
                var records = csv.GetRecords<ImageMapping>().ToList();
                ConsoleHelper.WriteInfo($"Loaded {records.Count} image mappings from CSV.");
                return records;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error loading CSV file: {ex.Message}");
                return new List<ImageMapping>();
            }
        }

        private async Task<IList<ImageDto>> GetImagesByTitleAsync(List<ImgDetail> imgDetails)
        {
            var titles = imgDetails.Where(i => !string.IsNullOrWhiteSpace(i.Title)).Select(i => i.Title).ToList();
            
            if (!titles.Any())
            {
                return new List<ImageDto>();
            }

            var images = await _client.GetItems<ImageDto>(new GetAllArgs()
            {
                Type = RestClientContentTypes.Images,
                Filter = $"Title in ({string.Join(",", titles.Select(t => $"'{t}'"))})",
                Take = titles.Count
            });

            return images.Items;
        }

        private async Task<IList<ImageDto>> GetImagesByTitleOrIdAsync(List<string> titles, List<Guid> targetIds)
        {
            var filterParts = new List<string>();

            if (titles.Any())
            {
                filterParts.Add($"Title in ({string.Join(",", titles.Select(t => $"'{t}'"))})");
            }

            if (targetIds.Any())
            {
                filterParts.Add($"Id in ({string.Join(",", targetIds.Select(id => $"{id}"))})");
            }

            if (!filterParts.Any())
            {
                return new List<ImageDto>();
            }

            var filter = string.Join(" or ", filterParts);
            var take = Math.Max(titles.Count, targetIds.Count);

            var images = await _client.GetItems<ImageDto>(new GetAllArgs()
            {
                Type = RestClientContentTypes.Images,
                Filter = filter,
                Take = take
            });

            ConsoleHelper.WriteInfo($"Found {images.Items.Count} images from {titles.Count} title(s) and {targetIds.Count} target ID(s).");
            return images.Items;
        }
    }

    internal class ProcessingResult
    {
        public int ProcessedCount { get; set; }
        public int UpdatedCount { get; set; }
    }

    internal class ImgDetail
    {
        public string Title { get; set; }
        public Guid? Id { get; set; }
    }

    internal class ImageMapping
    {
        [Name("Image Title")]
        public string ImageTitle { get; set; }
        
        [Name("Source Id")]
        public Guid SourceId { get; set; }
        
        [Name("Target Id")]
        public string TargetIdString { get; set; }
        
        [Ignore]
        public Guid? TargetId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TargetIdString) || TargetIdString.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                
                if (Guid.TryParse(TargetIdString, out var guid))
                {
                    return guid;
                }
                
                return null;
            }
        }
    }
}
