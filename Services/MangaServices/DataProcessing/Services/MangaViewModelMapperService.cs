using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.AuthServices; // Cần cho IUserService
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.Models;
using MangaReader.WebUI.Services.MangaServices.MangaInformation; // Cần cho MangaUtilityService
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services;

/// <summary>
/// Triển khai IMangaViewModelMapper, chịu trách nhiệm chuyển đổi Model MangaDex thành ViewModel.
/// </summary>
public class MangaViewModelMapperService(
    ILogger<MangaViewModelMapperService> logger,
    IMangaDataExtractor mangaDataExtractor, // Để lấy dữ liệu đã xử lý
    IMangaFollowService mangaFollowService, // Để kiểm tra trạng thái theo dõi
    IUserService userService,               // Để kiểm tra đăng nhập
    MangaUtilityService mangaUtilityService // Để lấy rating/views giả
    ) : IMangaViewModelMapper
{
    public async Task<MangaViewModel> MapToMangaViewModelAsync(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes!; // Sử dụng ! vì đã assert ở trên
        var relationships = mangaData.Relationships;

        try
        {
            string title = mangaDataExtractor.ExtractMangaTitle(attributes.Title, attributes.AltTitles);
            string description = mangaDataExtractor.ExtractMangaDescription(attributes.Description);
            string coverUrl = mangaDataExtractor.ExtractCoverUrl(id, relationships);
            string status = mangaDataExtractor.ExtractAndTranslateStatus(attributes.Status);
            List<string> tags = mangaDataExtractor.ExtractAndTranslateTags(attributes.Tags);
            var (author, artist) = mangaDataExtractor.ExtractAuthorArtist(relationships);
            var altTitlesDict = mangaDataExtractor.ExtractAlternativeTitles(attributes.AltTitles);
            string preferredAltTitle = mangaDataExtractor.ExtractPreferredAlternativeTitle(altTitlesDict);

            // Kiểm tra trạng thái follow (yêu cầu User Service và Follow Service)
            bool isFollowing = false;
            if (userService.IsAuthenticated())
            {
                // Thêm try-catch riêng cho việc kiểm tra follow để không làm crash toàn bộ mapping
                try
                {
                    isFollowing = await mangaFollowService.IsFollowingMangaAsync(id);
                }
                catch (Exception followEx)
                {
                    logger.LogError(followEx, "Lỗi khi kiểm tra trạng thái theo dõi cho manga {MangaId} trong Mapper.", id);
                    // isFollowing vẫn là false
                }
            }

            return new MangaViewModel
            {
                Id = id,
                Title = title,
                Description = description,
                CoverUrl = coverUrl,
                Status = status,
                Tags = tags,
                Author = author,
                Artist = artist,
                OriginalLanguage = attributes.OriginalLanguage ?? "",
                PublicationDemographic = attributes.PublicationDemographic ?? "",
                ContentRating = attributes.ContentRating ?? "",
                AlternativeTitles = preferredAltTitle, // Lấy tiêu đề thay thế ưu tiên
                LastUpdated = attributes.UpdatedAt.DateTime, // Chuyển DateTimeOffset thành DateTime
                IsFollowing = isFollowing,
                // Lấy dữ liệu giả từ Utility Service
                Rating = mangaUtilityService.GetMangaRating(id),
                Views = 0 // MangaDex API không cung cấp Views
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi nghiêm trọng khi mapping MangaData thành MangaViewModel cho ID: {MangaId}", id);
            // Trả về ViewModel lỗi để không làm crash trang
            return new MangaViewModel
            {
                Id = id,
                Title = $"Lỗi tải ({id})",
                Description = "Đã xảy ra lỗi khi xử lý dữ liệu.",
                CoverUrl = "/images/cover-placeholder.jpg",
                Status = "Lỗi",
                Tags = new List<string>(),
                Author = "Lỗi",
                Artist = "Lỗi"
            };
        }
    }

    public ChapterViewModel MapToChapterViewModel(MangaReader.WebUI.Models.Mangadex.Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterViewModel.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterViewModel.");

        var attributes = chapterData.Attributes!; // Use ! because of the assert

        try
        {
            string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);
            string chapterNumber = mangaDataExtractor.ExtractChapterNumber(attributes);

            // Xử lý relationships (đơn giản hóa, chỉ lấy ID và Type)
            var relationships = chapterData.Relationships?
                .Where(r => r != null)
                .Select(r => new ChapterRelationship { Id = r!.Id.ToString(), Type = r.Type })
                .ToList() ?? new List<ChapterRelationship>();

            return new ChapterViewModel
            {
                Id = chapterData.Id.ToString(),
                Title = displayTitle,
                Number = chapterNumber,
                Language = attributes.TranslatedLanguage ?? "unknown",
                PublishedAt = attributes.PublishAt.DateTime, // Convert DateTimeOffset to DateTime
                Relationships = relationships
            };
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Lỗi khi mapping ChapterData thành ChapterViewModel cho ID: {ChapterId}", chapterData?.Id);
            // Trả về ViewModel lỗi
            return new ChapterViewModel
            {
                Id = chapterData?.Id.ToString() ?? "error",
                Title = "Lỗi tải chương",
                Number = "?",
                Language = "error",
                PublishedAt = DateTime.MinValue,
                Relationships = new List<ChapterRelationship>()
            };
        }
    }

     public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(Manga mangaData, List<ChapterViewModel> chapters)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

        // Map thông tin manga cơ bản
        var mangaViewModel = await MapToMangaViewModelAsync(mangaData);

        // Trích xuất danh sách tiêu đề thay thế đã nhóm
        var alternativeTitlesByLanguage = mangaDataExtractor.ExtractAlternativeTitles(mangaData.Attributes?.AltTitles);

        return new MangaDetailViewModel
        {
            Manga = mangaViewModel,
            Chapters = chapters ?? new List<ChapterViewModel>(), // Đảm bảo không null
            AlternativeTitlesByLanguage = alternativeTitlesByLanguage
        };
    }

    public MangaInfoViewModel MapToMangaInfoViewModel(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaInfoViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes;
        var relationships = mangaData.Relationships;

        string title = "Lỗi tải tiêu đề";
        if (attributes != null)
        {
            title = mangaDataExtractor.ExtractMangaTitle(attributes.Title, attributes.AltTitles);
        }

        string coverUrl = mangaDataExtractor.ExtractCoverUrl(id, relationships);

        // MangaInfoViewModel không cần trạng thái follow hay các thông tin phức tạp khác
        return new MangaInfoViewModel
        {
            MangaId = id,
            MangaTitle = title,
            CoverUrl = coverUrl
        };
    }

     public SimpleChapterInfo MapToSimpleChapterInfo(MangaReader.WebUI.Models.Mangadex.Chapter chapterData)
    {
        Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành SimpleChapterInfo.");
        Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành SimpleChapterInfo.");

        var attributes = chapterData.Attributes!;

        string displayTitle = mangaDataExtractor.ExtractChapterDisplayTitle(attributes);

        return new SimpleChapterInfo
        {
            ChapterId = chapterData.Id.ToString(),
            DisplayTitle = displayTitle,
            PublishedAt = attributes.PublishAt.DateTime
        };
    }
    
    public LastReadMangaViewModel MapToLastReadMangaViewModel(MangaInfoViewModel mangaInfo, ChapterInfo chapterInfo, DateTime lastReadAt)
    {
        Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành LastReadMangaViewModel.");
        Debug.Assert(chapterInfo != null, "chapterInfo không được null khi mapping thành LastReadMangaViewModel.");

        return new LastReadMangaViewModel
        {
            MangaId = mangaInfo.MangaId,
            MangaTitle = mangaInfo.MangaTitle,
            CoverUrl = mangaInfo.CoverUrl,
            ChapterId = chapterInfo.Id,
            ChapterTitle = chapterInfo.Title,
            ChapterPublishedAt = chapterInfo.PublishedAt,
            LastReadAt = lastReadAt
        };
    }

    public FollowedMangaViewModel MapToFollowedMangaViewModel(MangaInfoViewModel mangaInfo, List<SimpleChapterInfo> latestChapters)
    {
        Debug.Assert(mangaInfo != null, "mangaInfo không được null khi mapping thành FollowedMangaViewModel.");
        Debug.Assert(latestChapters != null, "latestChapters không được null khi mapping thành FollowedMangaViewModel.");

        return new FollowedMangaViewModel
        {
            MangaId = mangaInfo.MangaId,
            MangaTitle = mangaInfo.MangaTitle,
            CoverUrl = mangaInfo.CoverUrl,
            LatestChapters = latestChapters
        };
    }
} 