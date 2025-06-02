using MangaReader.WebUI.Models;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Mangas;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.AuthServices;
using MangaReader.WebUI.Services.MangaServices;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.UtilityServices;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToMangaViewModelMapper : IMangaReaderLibToMangaViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToMangaViewModelMapper> _logger;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IMangaReaderLibAuthorClient _authorClient;
        private readonly IUserService _userService;
        private readonly IMangaFollowService _mangaFollowService;
        private readonly LocalizationService _localizationService;

        public MangaReaderLibToMangaViewModelMapper(
            ILogger<MangaReaderLibToMangaViewModelMapper> logger,
            IMangaReaderLibCoverApiService coverApiService,
            IMangaReaderLibAuthorClient authorClient,
            IUserService userService,
            IMangaFollowService mangaFollowService,
            LocalizationService localizationService)
        {
            _logger = logger;
            _coverApiService = coverApiService;
            _authorClient = authorClient;
            _userService = userService;
            _mangaFollowService = mangaFollowService;
            _localizationService = localizationService;
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
                string description = ""; // MangaReaderLib không có description trong MangaAttributesDto, cần lấy từ TranslatedManga nếu có
                string coverUrl = "/images/cover-placeholder.jpg";
                string author = "Không rõ";
                string artist = "Không rõ";

                // Lấy Cover URL từ relationships (cần tìm relationship type "cover_art")
                var coverRelationship = relationships?.FirstOrDefault(r => r.Type == "cover_art");
                if (coverRelationship != null && Guid.TryParse(coverRelationship.Id, out Guid coverArtGuid))
                {
                    // Gọi API để lấy CoverArtAttributesDto để có publicId
                    var coverArtResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtGuid);
                    if (coverArtResponse?.Data?.Attributes?.PublicId != null)
                    {
                        // Truyền coverRelationship.Id (là coverArtId) và PublicId
                        coverUrl = _coverApiService.GetCoverArtUrl(coverRelationship.Id, coverArtResponse.Data.Attributes.PublicId);
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy PublicId cho cover art ID {CoverArtId} của manga {MangaId}", coverRelationship.Id, id);
                    }
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy cover_art relationship hoặc ID không hợp lệ cho manga {MangaId}", id);
                }

                // Lấy Author/Artist từ relationships
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel.Type == "author" || rel.Type == "artist")
                        {
                            try
                            {
                                if(Guid.TryParse(rel.Id, out Guid staffId))
                                {
                                    var authorResponse = await _authorClient.GetAuthorByIdAsync(staffId);
                                    if (authorResponse?.Data?.Attributes?.Name != null)
                                    {
                                        if (rel.Type == "author")
                                        {
                                            author = authorResponse.Data.Attributes.Name;
                                        }
                                        else if (rel.Type == "artist")
                                        {
                                            artist = authorResponse.Data.Attributes.Name;
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Invalid Author/Artist ID format: {AuthorId} for manga {MangaId}", rel.Id, id);
                                }
                            }
                            catch (Exception authorEx)
                            {
                                _logger.LogError(authorEx, "Lỗi khi lấy thông tin tác giả/họa sĩ ID {AuthorId} cho manga {MangaId}", rel.Id, id);
                            }
                        }
                    }
                }

                string status = _localizationService.GetStatus(attributes.Status.ToString());
                List<string> tags = new List<string>(); // MangaReaderLib không có tags trực tiếp trong MangaAttributesDto

                // Kiểm tra trạng thái follow
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