using MangaReader.WebUI.Models.ViewModels.Manga;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IMangaToMangaViewModelMapper, chịu trách nhiệm chuyển đổi Model MangaDex thành MangaViewModel.
/// </summary>
public class MangaToMangaViewModelMapperService(
    ILogger<MangaToMangaViewModelMapperService> logger,
    IMangaDataExtractor mangaDataExtractor,
    IMangaFollowService mangaFollowService,
    IUserService userService
    ) : IMangaToMangaViewModelMapper
{
    public async Task<MangaViewModel> MapToMangaViewModelAsync(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes!;
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

            bool isFollowing = false;
            if (userService.IsAuthenticated())
            {
                try
                {
                    isFollowing = await mangaFollowService.IsFollowingMangaAsync(id);
                }
                catch (Exception followEx)
                {
                    logger.LogError(followEx, "Lỗi khi kiểm tra trạng thái theo dõi cho manga {MangaId} trong Mapper.", id);
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
                AlternativeTitles = preferredAltTitle,
                LastUpdated = attributes.UpdatedAt.DateTime,
                IsFollowing = isFollowing,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi nghiêm trọng khi mapping MangaData thành MangaViewModel cho ID: {MangaId}", id);
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
} 