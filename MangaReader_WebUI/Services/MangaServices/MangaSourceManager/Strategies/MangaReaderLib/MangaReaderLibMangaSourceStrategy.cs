// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibMangaSourceStrategy.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using MangaReaderLib.DTOs.CoverArts; // Thêm using này nếu chưa có

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibMangaSourceStrategy : IMangaApiSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IMangaReaderLibAuthorClient _authorClient;
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper; // Dùng để lấy một số thuộc tính cơ bản
        private readonly ILogger<MangaReaderLibMangaSourceStrategy> _logger;

        public MangaReaderLibMangaSourceStrategy(
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibCoverApiService coverApiService,
            IMangaReaderLibAuthorClient authorClient,
            IMangaReaderLibTagClient tagClient,
            IMangaReaderLibToMangaViewModelMapper mangaViewModelMapper,
            ILogger<MangaReaderLibMangaSourceStrategy> logger)
        {
            _mangaClient = mangaClient;
            _coverApiService = coverApiService;
            _authorClient = authorClient;
            _tagClient = tagClient;
            _mangaViewModelMapper = mangaViewModelMapper;
            _logger = logger;
        }

        public async Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] Fetching Manga with TitleFilter: {TitleFilter}", sortManga?.Title);
            var libResult = await _mangaClient.GetMangasAsync(
                offset: offset,
                limit: limit,
                titleFilter: sortManga?.Title,
                statusFilter: sortManga?.Status?.FirstOrDefault(),
                contentRatingFilter: sortManga?.ContentRating?.FirstOrDefault()?.ToLowerInvariant(),
                demographicFilter: sortManga?.Demographic?.FirstOrDefault(),
                originalLanguageFilter: sortManga?.OriginalLanguage?.FirstOrDefault(),
                yearFilter: sortManga?.Year,
                tagIdsFilter: sortManga?.IncludedTags?.Where(s => !string.IsNullOrEmpty(s) && Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                orderBy: sortManga?.SortBy,
                ascending: sortManga?.SortBy switch { "title" => true, "createdAt" => false, "updatedAt" => false, _ => false }
            );

            if (libResult?.Data == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchMangaAsync] API returned null or null Data.");
                return new MangaList { Result = "ok", Response = "collection", Data = new List<Manga>(), Total = 0 };
            }
             _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] Received {Count} manga DTOs from MangaReaderLib.", libResult.Data.Count);


            var mappedData = new List<Manga>();
            foreach (var dto in libResult.Data)
            {
                if (dto == null || dto.Attributes == null) continue;
                var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(dto);
                if (mangaDexModel != null)
                {
                    mappedData.Add(mangaDexModel);
                }
            }
             _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] Successfully mapped {MappedCount} manga DTOs.", mappedData.Count);

            return new MangaList
            {
                    Result = "ok",
                    Response = "collection",
                    Data = mappedData,
                Limit = libResult.Limit,
                Offset = libResult.Offset,
                Total = libResult.Total
            };
        }

        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaByIdsAsync] Fetching Manga by IDs: [{MangaIds}]", string.Join(", ", mangaIds));
            var mappedData = new List<Manga>();
            foreach (var idStr in mangaIds)
            {
                if (Guid.TryParse(idStr, out var guidId))
                {
                    var libResponse = await _mangaClient.GetMangaByIdAsync(guidId);
                    if (libResponse?.Data != null)
                    {
                        var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(libResponse.Data);
                        if (mangaDexModel != null) mappedData.Add(mangaDexModel);
                    }
                }
            }
            return new MangaList { Result = "ok", Response = "collection", Data = mappedData, Limit = mappedData.Count, Total = mappedData.Count };
        }

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaDetailsAsync] Fetching Manga Details for ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            var libResponse = await _mangaClient.GetMangaByIdAsync(guidMangaId);
            if (libResponse?.Data == null) return null;

            var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(libResponse.Data, true); // isDetails = true
            if (mangaDexModel == null) return null;
            
            return new MangaResponse { Result = "ok", Response = "entity", Data = mangaDexModel };
        }

        private async Task<Manga?> MapMangaReaderLibDtoToMangaDexModel(global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Mangas.MangaAttributesDto> dto, bool isDetails = false)
        {
            if (dto == null || dto.Attributes == null) return null;

            _logger.LogDebug("[MRLib Strategy Mapper] Mapping MangaReaderLib DTO ID: {MangaId}, Title: {Title}. IsDetails: {IsDetails}", dto.Id, dto.Attributes.Title, isDetails);

            var relationshipsDex = new List<Relationship>();
            var mangaDexTags = new List<MangaReader.WebUI.Models.Mangadex.Tag>();
            string? coverArtPublicIdForMangaAttributes = null; // Biến để lưu publicId cho MangaAttributes

            // Cover Art
            var coverRelLib = dto.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelLib != null && Guid.TryParse(coverRelLib.Id, out var coverArtIdGuid))
            {
                try
                {
                    var coverArtDetailsResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtIdGuid);
                    if (coverArtDetailsResponse?.Data?.Attributes?.PublicId != null)
                    {
                        string publicId = coverArtDetailsResponse.Data.Attributes.PublicId;
                        coverArtPublicIdForMangaAttributes = publicId; // Lưu lại publicId

                        // Tạo một đối tượng CoverAttributes của MangaDex để chứa publicId
                        var mangaDexCoverAttributes = new CoverAttributes 
                        { 
                            FileName = publicId, // MangaDex dùng FileName, ta sẽ gán publicId vào đây
                            Volume = coverArtDetailsResponse.Data.Attributes.Volume,
                            Description = coverArtDetailsResponse.Data.Attributes.Description,
                            CreatedAt = coverArtDetailsResponse.Data.Attributes.CreatedAt,
                            UpdatedAt = coverArtDetailsResponse.Data.Attributes.UpdatedAt,
                            Version = 1 // Giá trị mặc định
                        };

                        relationshipsDex.Add(new Relationship
                        {
                            Id = coverArtIdGuid,
                            Type = "cover_art",
                            Attributes = mangaDexCoverAttributes // Gán đối tượng attributes đã được làm giàu
                        });
                        _logger.LogDebug("[MRLib Strategy Mapper] Successfully enriched cover_art relationship for MangaId {MangaId} with PublicId {PublicId}", dto.Id, publicId);
                    }
                    else
                    {
                        _logger.LogWarning("[MRLib Strategy Mapper] Could not get PublicId for coverArtId {CoverArtId} for MangaId {MangaId}. Response: {ResponseResult}", coverArtIdGuid, dto.Id, coverArtDetailsResponse?.Result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MRLib Strategy Mapper] Error fetching cover art details for ID {CoverArtId} for MangaId {MangaId}", coverArtIdGuid, dto.Id);
                }
            }
            else
            {
                _logger.LogDebug("[MRLib Strategy Mapper] No cover_art relationship or invalid ID for manga {MangaId}", dto.Id);
            }

            // Author/Artist
            var staffRelsLib = dto.Relationships?.Where(r => r.Type == "author" || r.Type == "artist").ToList() ?? new();
            foreach (var staffRelLib in staffRelsLib)
            {
                if (Guid.TryParse(staffRelLib.Id, out var staffIdGuid))
                {
                    try
                    {
                        var staffDetails = await _authorClient.GetAuthorByIdAsync(staffIdGuid);
                        if (staffDetails?.Data?.Attributes != null)
                        {
                            // Tạo đối tượng MangaDex.AuthorAttributes
                            var mangaDexStaffAttributes = new MangaReader.WebUI.Models.Mangadex.AuthorAttributes 
                            { 
                                Name = staffDetails.Data.Attributes.Name,
                                Biography = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", staffDetails.Data.Attributes.Biography ?? "" } },
                                CreatedAt = staffDetails.Data.Attributes.CreatedAt,
                                UpdatedAt = staffDetails.Data.Attributes.UpdatedAt,
                                Version = 1 // Giá trị mặc định
                            };
                            relationshipsDex.Add(new Relationship
                            {
                                Id = staffIdGuid, 
                                Type = staffRelLib.Type, // Giữ nguyên type "author" hoặc "artist"
                                Attributes = mangaDexStaffAttributes // Gán object attributes đã làm giàu
                            });
                             _logger.LogDebug("[MRLib Strategy Mapper] Successfully enriched {StaffType} relationship for MangaId {MangaId} with Name {StaffName}", staffRelLib.Type, dto.Id, staffDetails.Data.Attributes.Name);
                        } else _logger.LogWarning("[MRLib Strategy Mapper] Could not get details for staffId {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[MRLib Strategy Mapper] Error fetching staff details for ID {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                    }
                } else _logger.LogWarning("[MRLib Strategy Mapper] Invalid staff ID format: {StaffId} for MangaId {MangaId}", staffRelLib.Id, dto.Id);
            }

            // Tags
            var tagRelsLib = dto.Relationships?.Where(r => r.Type == "tag").ToList() ?? new();
            foreach (var tagRelLib in tagRelsLib)
            {
                if (Guid.TryParse(tagRelLib.Id, out var tagIdGuid))
                {
                    try
                    {
                        var tagDetails = await _tagClient.GetTagByIdAsync(tagIdGuid);
                        if (tagDetails?.Data?.Attributes != null)
                        {
                            var mdTagAttrs = new MangaReader.WebUI.Models.Mangadex.TagAttributes 
                            { 
                                Name = new Dictionary<string, string> { { "en", tagDetails.Data.Attributes.Name } }, // Mặc định lấy tên tag là tiếng Anh
                                Group = tagDetails.Data.Attributes.TagGroupName?.ToLowerInvariant() ?? "other", 
                                Version = 1 
                            };
                            relationshipsDex.Add(new Relationship { Id = tagIdGuid, Type = "tag", Attributes = mdTagAttrs });
                            mangaDexTags.Add(new MangaReader.WebUI.Models.Mangadex.Tag { Id = tagIdGuid, Type = "tag", Attributes = mdTagAttrs });
                        } else _logger.LogWarning("[MRLib Strategy Mapper] Could not get details for tagId {TagId} for MangaId {MangaId}", tagIdGuid, dto.Id);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "[MRLib Strategy Mapper] Error fetching tag details for ID {TagId} for MangaId {MangaId}", tagIdGuid, dto.Id);
                    }
                }
            }
            _logger.LogDebug("[MRLib Strategy Mapper] Processed {RelCount} relationships for manga {MangaId}.", relationshipsDex.Count, dto.Id);

            string description = "Mô tả sẽ được tải ở trang chi tiết.";
            if (isDetails) 
            {
                 description = "Không có mô tả.";
                try
                {
                    var translations = await _mangaClient.GetMangaTranslationsAsync(Guid.Parse(dto.Id));
                    var preferredTranslation = translations?.Data?.FirstOrDefault(t => 
                        (t.Attributes?.LanguageKey?.Equals("en", StringComparison.OrdinalIgnoreCase) ?? false) || 
                        (t.Attributes?.LanguageKey?.Equals(dto.Attributes.OriginalLanguage, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (t.Attributes?.LanguageKey?.Equals("vi", StringComparison.OrdinalIgnoreCase) ?? false) ); // Thêm ưu tiên tiếng Việt
                    
                    if (preferredTranslation?.Attributes?.Description != null)
                    {
                        description = preferredTranslation.Attributes.Description;
                        _logger.LogDebug("[MRLib Strategy Mapper] Found description for manga {MangaId} (lang: {Lang}): {Desc}", dto.Id, preferredTranslation.Attributes.LanguageKey, description.Substring(0, Math.Min(50, description.Length)));
                    } else {
                        _logger.LogInformation("[MRLib Strategy Mapper] No suitable (en, vi, original) language description found for manga {MangaId}.", dto.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MRLib Strategy Mapper] Error fetching translations for description for MangaId {MangaId}", dto.Id);
                }
            }

            return new Manga
            {
                Id = Guid.Parse(dto.Id),
                Type = "manga",
                Attributes = new MangaAttributes
                {
                    Title = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", dto.Attributes.Title } },
                    AltTitles = new List<Dictionary<string, string>>(), // MangaReaderLib không có altTitles trực tiếp trong Manga DTO
                    Description = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", description } },
                    Status = dto.Attributes.Status.ToString(),
                    ContentRating = dto.Attributes.ContentRating.ToString(),
                    OriginalLanguage = dto.Attributes.OriginalLanguage,
                    PublicationDemographic = dto.Attributes.PublicationDemographic?.ToString(),
                    Year = dto.Attributes.Year,
                    IsLocked = dto.Attributes.IsLocked,
                    CreatedAt = dto.Attributes.CreatedAt,
                    UpdatedAt = dto.Attributes.UpdatedAt,
                    Version = 1, // Giả định version
                    Tags = mangaDexTags,
                    // ChapterNumbersResetOnNewVolume, AvailableTranslatedLanguages, LatestUploadedChapter không có trong MangaReaderLib
                },
                Relationships = relationshipsDex.Any() ? relationshipsDex : null // Trả về null nếu rỗng
            };
        }
    }
} 