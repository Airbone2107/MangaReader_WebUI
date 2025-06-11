using MangaReader.WebUI.Models.Mangadex; // Cho TagListResponse, Tag, TagAttributes
using MangaReaderLib.DTOs.Common;        // Cho ApiCollectionResponse, ResourceObject
using MangaReaderLib.DTOs.Tags;          // Cho TagAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;                      // Cho Any
using System.Collections.Generic;       // Cho Dictionary, List

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToTagListResponseMapper : IMangaReaderLibToTagListResponseMapper
    {
        private readonly ILogger<MangaReaderLibToTagListResponseMapper> _logger;

        public MangaReaderLibToTagListResponseMapper(ILogger<MangaReaderLibToTagListResponseMapper> logger)
        {
            _logger = logger;
        }

        public TagListResponse MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsDataFromLib)
        {
            Debug.Assert(tagsDataFromLib != null, "tagsDataFromLib không được null khi mapping thành TagListResponse.");

            var tagListResponse = new TagListResponse
            {
                Result = tagsDataFromLib.Result,
                Response = tagsDataFromLib.ResponseType, // Hoặc "collection"
                Limit = tagsDataFromLib.Limit,
                Offset = tagsDataFromLib.Offset,
                Total = tagsDataFromLib.Total,
                Data = new List<Tag>()
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
                            
                            // Tạo MangaDex.TagAttributes
                            var dexTagAttributes = new TagAttributes
                            {
                                // MangaReaderLib TagAttributesDto.Name là string
                                // MangaDex TagAttributes.Name là Dictionary<string, string>
                                // Giả sử tên từ MangaReaderLib là tiếng Anh (en)
                                Name = new Dictionary<string, string> { { "en", libTagAttributes.Name } },
                                Description = new Dictionary<string, string>(), // MangaReaderLib TagAttributesDto không có description
                                Group = libTagAttributes.TagGroupName?.ToLowerInvariant() ?? "other", // Map TagGroupName sang Group
                                Version = 1 // Giá trị mặc định
                            };

                            var dexTag = new Tag
                            {
                                Id = Guid.Parse(libTagResource.Id),
                                Type = "tag", // Loại cố định cho MangaDex
                                Attributes = dexTagAttributes
                            };
                            tagListResponse.Data.Add(dexTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib Tag ID {TagId} sang MangaDex Tag ViewModel.", libTagResource.Id);
                            continue;
                        }
                    }
                }
            }
            return tagListResponse;
        }
    }
} 