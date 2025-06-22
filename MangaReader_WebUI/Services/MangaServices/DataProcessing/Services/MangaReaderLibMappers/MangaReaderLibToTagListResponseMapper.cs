using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Tags;          // Cho TagAttributesDto
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToTagListResponseMapper : IMangaReaderLibToTagListResponseMapper
    {
        private readonly ILogger<MangaReaderLibToTagListResponseMapper> _logger;

        public MangaReaderLibToTagListResponseMapper(ILogger<MangaReaderLibToTagListResponseMapper> logger)
        {
            _logger = logger;
        }

        public TagListViewModel MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsDataFromLib)
        {
            Debug.Assert(tagsDataFromLib != null, "tagsDataFromLib không được null khi mapping thành TagListViewModel.");

            var tagListViewModel = new TagListViewModel
            {
                Result = tagsDataFromLib.Result,
                Response = tagsDataFromLib.ResponseType,
                Limit = tagsDataFromLib.Limit,
                Offset = tagsDataFromLib.Offset,
                Total = tagsDataFromLib.Total,
                Data = new List<TagViewModel>()
            };

            if (tagsDataFromLib.Data != null)
            {
                foreach (var libTagResource in tagsDataFromLib.Data)
                {
                    if (libTagResource?.Attributes != null)
                    {
                        try
                        {
                            var libTagAttributes = libTagResource.Attributes;
                            
                            var dexTagAttributes = new TagAttributesViewModel
                            {
                                Name = new Dictionary<string, string> { { "en", libTagAttributes.Name } },
                                Description = new Dictionary<string, string>(),
                                Group = libTagAttributes.TagGroupName?.ToLowerInvariant() ?? "other",
                                Version = 1
                            };

                            var dexTag = new TagViewModel
                            {
                                Id = libTagResource.Id,
                                Type = "tag",
                                Attributes = dexTagAttributes
                            };
                            tagListViewModel.Data.Add(dexTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib Tag ID {TagId} sang TagViewModel.", libTagResource.Id);
                            continue;
                        }
                    }
                }
            }
            return tagListViewModel;
        }
    }
} 