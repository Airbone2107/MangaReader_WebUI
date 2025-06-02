using MangaReader.WebUI.Models.Mangadex;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Chapters;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToAtHomeServerResponseMapper : IMangaReaderLibToAtHomeServerResponseMapper
    {
        private readonly ILogger<MangaReaderLibToAtHomeServerResponseMapper> _logger;

        public MangaReaderLibToAtHomeServerResponseMapper(ILogger<MangaReaderLibToAtHomeServerResponseMapper> logger)
        {
            _logger = logger;
        }

        public AtHomeServerResponse MapToAtHomeServerResponse(
            ApiCollectionResponse<ResourceObject<ChapterPageAttributesDto>> chapterPagesData,
            string chapterId, // chapterId được truyền vào từ MangaSourceManagerService
            string mangaReaderLibBaseUrl) 
        {
            Debug.Assert(chapterPagesData != null, "chapterPagesData không được null khi mapping.");
            Debug.Assert(!string.IsNullOrEmpty(chapterId), "chapterId không được rỗng.");
            Debug.Assert(!string.IsNullOrEmpty(mangaReaderLibBaseUrl), "mangaReaderLibBaseUrl không được rỗng.");

            var pages = new List<string>();
            if (chapterPagesData.Data != null && chapterPagesData.Data.Any())
            {
                // Sắp xếp các trang theo PageNumber
                var sortedPages = chapterPagesData.Data.OrderBy(p => p.Attributes.PageNumber);

                foreach (var pageDto in sortedPages)
                {
                    if (pageDto?.Attributes?.PublicId != null)
                    {
                        // URL ảnh của MangaReaderLib là: {baseUrl}/chapters/{chapterId}/pages/{publicId}/image
                        var imageUrl = $"{mangaReaderLibBaseUrl.TrimEnd('/')}/chapters/{chapterId}/pages/{pageDto.Attributes.PublicId}/image";
                        pages.Add(imageUrl);
                        _logger.LogDebug("Mapped MangaReaderLib page: ChapterId={ChapterId}, PageNumber={PageNumber}, PublicId={PublicId} to URL: {ImageUrl}",
                            chapterId, pageDto.Attributes.PageNumber, pageDto.Attributes.PublicId, imageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Skipping page due to missing PublicId. ChapterId={ChapterId}, PageDtoId={PageDtoId}", chapterId, pageDto?.Id);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No page data found in chapterPagesData for ChapterId={ChapterId}", chapterId);
            }

            // MangaDex AtHomeServerResponse có baseUrl và hash.
            // Đối với MangaReaderLib, ta sẽ không dùng `baseUrl` của MangaDex@Home.
            // Thay vào đó, `Data` và `DataSaver` sẽ chứa các URL đầy đủ.
            // `baseUrl` có thể để trống hoặc là base URL của API MangaReaderLib.
            // `hash` có thể là chapterId.
            return new AtHomeServerResponse
            {
                Result = "ok",
                BaseUrl = "", // Không cần thiết vì Data đã chứa URL đầy đủ
                Chapter = new AtHomeChapterData
                {
                    Hash = chapterId, 
                    Data = pages,
                    DataSaver = pages // Giả sử không có phiên bản dataSaver riêng
                }
            };
        }
    }
} 