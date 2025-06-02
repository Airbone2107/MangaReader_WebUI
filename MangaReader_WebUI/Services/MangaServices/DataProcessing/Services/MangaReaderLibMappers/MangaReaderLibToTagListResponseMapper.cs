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

        public TagListResponse MapToTagListResponse(ApiCollectionResponse<ResourceObject<TagAttributesDto>> tagsData)
        {
            Debug.Assert(tagsData != null, "tagsData không được null khi mapping thành TagListResponse.");

            var tagListResponse = new TagListResponse
            {
                Result = tagsData.Result,
                Response = tagsData.ResponseType,
                Limit = tagsData.Limit,
                Offset = tagsData.Offset,
                Total = tagsData.Total,
                Data = new List<Tag>()
            };

            if (tagsData.Data != null)
            {
                foreach (var tagDto in tagsData.Data)
                {
                    if (tagDto?.Attributes != null)
                    {
                        try
                        {
                            // Ánh xạ từ MangaReaderLib.DTOs.Tags.TagAttributesDto sang MangaReader.WebUI.Models.Mangadex.Tag
                            // Lưu ý: MangaDex TagAttributes có Name là Dictionary<string, string>, MangaReaderLib là string.
                            // Cần tạo một Dictionary cho Name trong Tag của MangaDex.
                            var mangadexTag = new Tag
                            {
                                Id = Guid.Parse(tagDto.Id),
                                Type = "tag", // Loại cố định
                                Attributes = new TagAttributes
                                {
                                    Name = new Dictionary<string, string> { { "en", tagDto.Attributes.Name } }, // Giả định tên tiếng Anh
                                    Description = new Dictionary<string, string>(), // Không có mô tả trong MangaReaderLib
                                    Group = tagDto.Attributes.TagGroupName.ToLower(), // Chuyển tên nhóm thành lowercase để khớp với MangaDex
                                    Version = 1 // Giá trị mặc định
                                }
                            };
                            tagListResponse.Data.Add(mangadexTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi mapping MangaReaderLib Tag ID {TagId} sang MangaDex Tag ViewModel.", tagDto.Id);
                            continue;
                        }
                    }
                }
            }

            return tagListResponse;
        }
    }
} 