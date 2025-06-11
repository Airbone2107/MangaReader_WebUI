using MangaReader.WebUI.Models;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.CoverArts;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaViewModelMapper : IMangaReaderLibToMangaViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToMangaViewModelMapper> _logger;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IUserService _userService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly LocalizationService _localizationService;
        private readonly string _cloudinaryBaseUrl;

        public MangaReaderLibToMangaViewModelMapper(
            ILogger<MangaReaderLibToMangaViewModelMapper> logger,
            IMangaReaderLibCoverApiService coverApiService,
            IUserService userService,
            IMangaFollowService mangaFollowService,
            LocalizationService localizationService,
            IConfiguration configuration)
        {
            _logger = logger;
            _coverApiService = coverApiService;
            _userService = userService;
            _mangaFollowService = mangaFollowService;
            _localizationService = localizationService;
            _cloudinaryBaseUrl = configuration["MangaReaderApiSettings:CloudinaryBaseUrl"]?.TrimEnd('/') 
                                ?? throw new InvalidOperationException("MangaReaderApiSettings:CloudinaryBaseUrl is not configured.");
        }

        public async Task<MangaViewModel> MapToMangaViewModelAsync(ResourceObject<MangaAttributesDto> mangaData, bool isFollowing = false)
        {
            Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaViewModel.");
            Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaViewModel.");

            string id = mangaData.Id;
            var attributes = mangaData.Attributes!;
            var relationships = mangaData.Relationships;

            try
            {
                string title = attributes.Title;
                string description = "";
                string coverUrl = "/images/cover-placeholder.jpg";
                string author = "Không rõ";
                string artist = "Không rõ";

                var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
                if (coverRelationship != null && !string.IsNullOrEmpty(coverRelationship.Id))
                {
                    coverUrl = $"{_cloudinaryBaseUrl}/{coverRelationship.Id}";
                    _logger.LogDebug("MangaReaderLib Mapper: Cover URL set to {CoverUrl} using PublicId from relationship.", coverUrl);
                }
                else
                {
                    _logger.LogWarning("MangaReaderLib Mapper: No cover_art relationship with PublicId found for manga {MangaId}. Using placeholder.", id);
                }

                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel.Attributes != null)
                        {
                            try
                            {
                                var relAttributes = JsonSerializer.Deserialize<AuthorAttributesDto>(JsonSerializer.Serialize(rel.Attributes));
                                if (relAttributes != null)
                                {
                                    if (rel.Type == "author") author = relAttributes.Name ?? author;
                                    else if (rel.Type == "artist") artist = relAttributes.Name ?? artist;
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "MangaReaderLib Mapper: Failed to deserialize relationship attributes for manga {MangaId}, relationship type {RelType}.", id, rel.Type);
                            }
                        }
                    }
                }
                _logger.LogDebug("MangaReaderLib Mapper: Author: {Author}, Artist: {Artist} for manga {MangaId}", author, artist, id);

                string status = _localizationService.GetStatus(attributes.Status.ToString());
                
                List<string> tags = new List<string>();
                if (attributes.Tags != null && attributes.Tags.Any())
                {
                    tags = attributes.Tags
                        .Where(t => t.Attributes != null && !string.IsNullOrEmpty(t.Attributes.Name))
                        .Select(t => t.Attributes.Name)
                        .Distinct()
                        .OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), false))
                        .ToList();
                }
                _logger.LogDebug("MangaReaderLib Mapper: Tags: [{Tags}] for manga {MangaId}", string.Join(", ", tags), id);

                if (_userService.IsAuthenticated())
                {
                    try
                    {
                        isFollowing = await _mangaFollowService.IsFollowingMangaAsync(id);
                    }
                    catch (Exception followEx)
                    {
                        _logger.LogError(followEx, "Lỗi khi kiểm tra trạng thái theo dõi cho manga {MangaId} trong MangaReaderLib Mapper.", id);
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
                    PublicationDemographic = attributes.PublicationDemographic?.ToString() ?? "",
                    ContentRating = attributes.ContentRating.ToString() ?? "",
                    AlternativeTitles = "",
                    LastUpdated = attributes.UpdatedAt,
                    IsFollowing = isFollowing,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi mapping MangaReaderLib MangaData thành MangaViewModel cho ID: {MangaId}", id);
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
} 