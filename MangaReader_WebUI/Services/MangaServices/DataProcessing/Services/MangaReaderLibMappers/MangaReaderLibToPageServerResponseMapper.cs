using MangaReader.WebUI.Models.ViewModels.Common;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToPageServerResponseMapper : IMangaReaderLibToPageServerResponseMapper
    {
        private readonly ILogger<MangaReaderLibToPageServerResponseMapper> _logger;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToPageServerResponseMapper(
            ILogger<MangaReaderLibToPageServerResponseMapper> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/')
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public PageServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId,
            string mangaReaderLibBaseUrlIgnored)
        {
            Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
            Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterid không được rỗng.");
            
            var pages = chapterPagesData.Data?
                .OrderBy(p => p.Attributes.PageNumber)
                .Select(p => p.Attributes.PublicId != null ? $"{_cloudinaryBaseUrl}/{p.Attributes.PublicId}" : null)
                .Where(url => url != null)
                .ToList() ?? new List<string?>();

            _logger.LogInformation("[MRLib Page Mapper] Mapped {Count} pages for ChapterId: {ChapterId}", pages.Count, chapterId);

            return new PageServerResponse
            {
                Result = "ok",
                BaseUrl = "", // Không dùng BaseUrl nữa
                Chapter = new PageChapterData
                {
                    Hash = chapterId,
                    Data = pages!,
                    DataSaver = pages! // DataSaver dùng chung URL
                }
            };
        }
    }
} 