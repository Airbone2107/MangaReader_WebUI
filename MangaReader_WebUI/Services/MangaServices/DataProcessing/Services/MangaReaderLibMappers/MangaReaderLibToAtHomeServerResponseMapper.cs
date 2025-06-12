using MangaReader.WebUI.Models.Mangadex;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToAtHomeServerResponseMapper : IMangaReaderLibToAtHomeServerResponseMapper
    {
        private readonly ILogger<MangaReaderLibToAtHomeServerResponseMapper> _logger;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToAtHomeServerResponseMapper(
            ILogger<MangaReaderLibToAtHomeServerResponseMapper> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured for AtHomeServerResponseMapper.");
        }

        public AtHomeServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored)
        {
            Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
            Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterId không được rỗng.");
            
            _logger.LogInformation("[MRLib AtHome Mapper] Bắt đầu map chapter pages cho ChapterId: {ChapterId}. Dữ liệu đầu vào: {ChapterPagesDataJson}", 
                chapterId, JsonSerializer.Serialize(chapterPagesData));

            var pages = new List<string>();
            if (chapterPagesData.Data != null && chapterPagesData.Data.Any())
            {
                var sortedPages = chapterPagesData.Data.OrderBy(p => p.Attributes.PageNumber);
                 _logger.LogDebug("[MRLib AtHome Mapper] Sorted pages DTOs: {SortedPagesJson}", JsonSerializer.Serialize(sortedPages));

                foreach (var pageDto in sortedPages)
                {
                    if (pageDto?.Attributes?.PublicId != null)
                    {
                        var imageUrl = $"{_cloudinaryBaseUrl}/{pageDto.Attributes.PublicId}";
                        pages.Add(imageUrl);
                        _logger.LogDebug("[MRLib AtHome Mapper] Mapped MangaReaderLib page: ChapterId={ChapterId}, PageNumber={PageNumber}, PublicId={PublicId} to Cloudinary URL: {ImageUrl}",
                            chapterId, pageDto.Attributes.PageNumber, pageDto.Attributes.PublicId, imageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("[MRLib AtHome Mapper] Skipping page due to missing PublicId. ChapterId={ChapterId}, PageDtoId={PageDtoId}", chapterId, pageDto?.Id);
                    }
                }
            }
            else
            {
                _logger.LogWarning("[MRLib AtHome Mapper] No page data found in chapterPagesData for ChapterId={ChapterId}", chapterId);
            }

            var result = new AtHomeServerResponse
            {
                Result = "ok",
                BaseUrl = "",
                Chapter = new AtHomeChapterData
                {
                    Hash = chapterId,
                    Data = pages,
                    DataSaver = pages
                }
            };
            _logger.LogInformation("[MRLib AtHome Mapper] Kết thúc map chapter pages cho ChapterId: {ChapterId}. Kết quả: {ResultJson}", 
                chapterId, JsonSerializer.Serialize(result));
            return result;
        }
    }
} 