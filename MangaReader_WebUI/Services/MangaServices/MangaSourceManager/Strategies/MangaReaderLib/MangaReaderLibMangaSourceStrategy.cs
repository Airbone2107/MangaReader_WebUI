// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibMangaSourceStrategy.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.Enums; // Cho PublicationDemographic
using MangaReaderLib.DTOs.Authors; // Cho AuthorAttributesDto
using System.Text.Json; // Cho JsonSerializer

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibMangaSourceStrategy : IMangaApiSourceStrategy
    {
        private readonly IMangaReaderLibMangaClient _mangaClient;
        private readonly IMangaReaderLibCoverApiService _coverApiService;
        private readonly IMangaReaderLibAuthorClient _authorClient;
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaViewModelMapper;
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
            _logger.LogInformation("[MRLib Strategy->FetchMangaAsync] TitleFilter: {TitleFilter}", sortManga?.Title);
            
            List<PublicationDemographic>? publicationDemographics = null;
            if (sortManga?.Demographic != null && sortManga.Demographic.Any())
            {
                publicationDemographics = new List<PublicationDemographic>();
                foreach (var demoStr in sortManga.Demographic)
                {
                    if (Enum.TryParse<PublicationDemographic>(demoStr, true, out var pubDemo))
                    {
                        publicationDemographics.Add(pubDemo);
                    }
                }
            }

            // Map ContentRating của MangaDex (List<string>) sang string của MangaReaderLib (lấy phần tử đầu tiên nếu có)
            string? contentRatingFilter = null;
            if (sortManga?.ContentRating != null && sortManga.ContentRating.Any())
            {
                // MangaReaderLib ContentRating enum không có "None", "All".
                // Nếu UI gửi "safe", "suggestive", "erotica", "pornographic" thì map.
                var validRatings = sortManga.ContentRating
                    .Where(r => Enum.TryParse<ContentRating>(r, true, out _))
                    .ToList();
                if (validRatings.Any())
                {
                     // MangaReaderLib API chỉ nhận 1 content rating filter
                    contentRatingFilter = validRatings.First();
                }
            }


            var libResult = await _mangaClient.GetMangasAsync(
                offset: offset,
                limit: limit,
                titleFilter: sortManga?.Title,
                statusFilter: sortManga?.Status?.FirstOrDefault(),
                contentRatingFilter: contentRatingFilter, // Sử dụng contentRatingFilter đã map
                publicationDemographicsFilter: publicationDemographics,
                originalLanguageFilter: sortManga?.OriginalLanguage?.FirstOrDefault(),
                yearFilter: sortManga?.Year,
                authorIdsFilter: sortManga?.Authors?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(), // authors là ID trong SortManga
                includedTags: sortManga?.IncludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                includedTagsMode: sortManga?.IncludedTagsMode,
                excludedTags: sortManga?.ExcludedTags?.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList(),
                excludedTagsMode: sortManga?.ExcludedTagsMode,
                orderBy: sortManga?.SortBy, // Cần mapping logic nếu SortBy của UI khác API
                ascending: sortManga?.SortBy switch { "title" => true, _ => false }, // Mặc định desc cho các trường hợp khác
                includes: new List<string> { "cover_art", "author" } // Luôn yêu cầu includes
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

        public async Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaDetailsAsync] ID: {MangaId}", mangaId);
            if (!Guid.TryParse(mangaId, out var guidMangaId)) return null;

            // Yêu cầu include author, artist
            var libResponse = await _mangaClient.GetMangaByIdAsync(guidMangaId, new List<string> { "author" });
            if (libResponse?.Data == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchMangaDetailsAsync] Failed to get manga details for ID {MangaId}", mangaId);
                return null;
            }

            var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(libResponse.Data, true); // isDetails = true
            if (mangaDexModel == null) return null;
            
            return new MangaResponse { Result = "ok", Response = "entity", Data = mangaDexModel };
        }
        
        // Hàm MapMangaReaderLibDtoToMangaDexModel cần được cập nhật để xử lý attributes trong relationship
        private async Task<Manga?> MapMangaReaderLibDtoToMangaDexModel(global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Mangas.MangaAttributesDto> dto, bool isDetails = false)
        {
            if (dto == null || dto.Attributes == null) return null;

            _logger.LogDebug("[MRLib Strategy Mapper] Mapping MangaReaderLib DTO ID: {MangaId}, Title: {Title}. IsDetails: {IsDetails}", dto.Id, dto.Attributes.Title, isDetails);

            var relationshipsDex = new List<Relationship>();
            var mangaDexTags = new List<MangaReader.WebUI.Models.Mangadex.Tag>();

            // Cover Art
            var coverRelLib = dto.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelLib != null)
            {
                string? publicIdForCover = null;
                Guid coverArtGuid = Guid.Empty;

                if (isDetails && Guid.TryParse(coverRelLib.Id, out coverArtGuid)) // Khi get detail, ID là GUID của CoverArt
                {
                     try
                    {
                        var coverArtDetailsResponse = await _coverApiService.GetCoverArtByIdAsync(coverArtGuid);
                        publicIdForCover = coverArtDetailsResponse?.Data?.Attributes?.PublicId;
                         _logger.LogDebug("[MRLib Strategy Mapper] Cover Art (Details): Fetched PublicId '{PublicId}' for CoverArt GUID '{CoverArtGuid}'", publicIdForCover, coverArtGuid);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[MRLib Strategy Mapper] Cover Art (Details): Error fetching cover art details for GUID {CoverArtGuid}", coverArtGuid);
                    }
                }
                else if (!isDetails && !string.IsNullOrEmpty(coverRelLib.Id)) // Khi get list + include, ID là PublicId
                {
                    publicIdForCover = coverRelLib.Id;
                    _logger.LogDebug("[MRLib Strategy Mapper] Cover Art (List): Using PublicId '{PublicId}' directly from relationship ID.", publicIdForCover);
                }
                
                if (!string.IsNullOrEmpty(publicIdForCover))
                {
                    var mangaDexCoverAttributes = new CoverAttributes 
                    { 
                        FileName = publicIdForCover, // MangaDex dùng FileName, ta sẽ gán publicId vào đây
                        // Các thuộc tính khác của CoverAttributes có thể null hoặc lấy từ coverArtDetailsResponse nếu isDetails
                        Volume = isDetails && coverArtGuid != Guid.Empty ? (await _coverApiService.GetCoverArtByIdAsync(coverArtGuid))?.Data?.Attributes?.Volume : null,
                        Description = isDetails && coverArtGuid != Guid.Empty ? (await _coverApiService.GetCoverArtByIdAsync(coverArtGuid))?.Data?.Attributes?.Description : null,
                        CreatedAt = DateTimeOffset.UtcNow, // Giá trị mặc định
                        UpdatedAt = DateTimeOffset.UtcNow, // Giá trị mặc định
                        Version = 1 
                    };
                    relationshipsDex.Add(new Relationship
                    {
                        Id = coverArtGuid != Guid.Empty ? coverArtGuid : (Guid.TryParse(coverRelLib.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid()), // Dùng GUID nếu có, nếu không thì parse ID của rel, fallback NewGuid
                        Type = "cover_art",
                        Attributes = mangaDexCoverAttributes
                    });
                }
                else
                {
                     _logger.LogWarning("[MRLib Strategy Mapper] Cover Art: Could not determine PublicId for cover art for MangaId {MangaId}. Relationship ID: {RelId}", dto.Id, coverRelLib.Id);
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
                    string staffName = "Không rõ";
                    string? staffBio = null;

                    if (staffRelLib.Attributes != null) // API đã include attributes
                    {
                        try
                        {
                            var libStaffAttrs = JsonSerializer.Deserialize<global::MangaReaderLib.DTOs.Authors.AuthorAttributesDto>(JsonSerializer.Serialize(staffRelLib.Attributes));
                            if (libStaffAttrs != null)
                            {
                                staffName = libStaffAttrs.Name ?? staffName;
                                staffBio = libStaffAttrs.Biography;
                                _logger.LogDebug("[MRLib Strategy Mapper] Staff (from included attributes): Type '{Type}', Name '{Name}' for MangaId {MangaId}", staffRelLib.Type, staffName, dto.Id);
                            }
                        }
                        catch (JsonException ex)
                        {
                             _logger.LogWarning(ex, "[MRLib Strategy Mapper] Staff: Failed to deserialize included attributes for staff ID {StaffId}, Type {StaffType}", staffRelLib.Id, staffRelLib.Type);
                        }
                    }
                    else if (isDetails) // Nếu là trang detail và API không include, thử gọi API phụ
                    {
                         try
                        {
                            var staffDetails = await _authorClient.GetAuthorByIdAsync(staffIdGuid);
                            if (staffDetails?.Data?.Attributes != null)
                            {
                                staffName = staffDetails.Data.Attributes.Name ?? staffName;
                                staffBio = staffDetails.Data.Attributes.Biography;
                                 _logger.LogDebug("[MRLib Strategy Mapper] Staff (from API call): Type '{Type}', Name '{Name}' for MangaId {MangaId}", staffRelLib.Type, staffName, dto.Id);
                            } else _logger.LogWarning("[MRLib Strategy Mapper] Staff: Could not get details for staffId {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[MRLib Strategy Mapper] Staff: Error fetching staff details for ID {StaffId} for MangaId {MangaId}", staffIdGuid, dto.Id);
                        }
                    }

                    var mangaDexStaffAttributes = new MangaReader.WebUI.Models.Mangadex.AuthorAttributes 
                    { 
                        Name = staffName,
                        Biography = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", staffBio ?? "" } },
                        CreatedAt = DateTimeOffset.UtcNow, // Mặc định
                        UpdatedAt = DateTimeOffset.UtcNow, // Mặc định
                        Version = 1 
                    };
                    relationshipsDex.Add(new Relationship
                    {
                        Id = staffIdGuid, 
                        Type = staffRelLib.Type,
                        Attributes = mangaDexStaffAttributes
                    });
                } else _logger.LogWarning("[MRLib Strategy Mapper] Invalid staff ID format: {StaffId} for MangaId {MangaId}", staffRelLib.Id, dto.Id);
            }

            // Tags: Lấy từ `attributes.Tags`
            if (dto.Attributes.Tags != null && dto.Attributes.Tags.Any())
            {
                foreach (var libTagResource in dto.Attributes.Tags)
                {
                    if (libTagResource?.Attributes != null && Guid.TryParse(libTagResource.Id, out var tagIdGuid))
                    {
                        var mdTagAttrs = new MangaReader.WebUI.Models.Mangadex.TagAttributes 
                        { 
                            Name = new Dictionary<string, string> { { "en", libTagResource.Attributes.Name } },
                            Group = libTagResource.Attributes.TagGroupName?.ToLowerInvariant() ?? "other", 
                            Version = 1 
                        };
                        // Không thêm vào relationshipsDex vì MangaDex model không có tag trong relationship
                        mangaDexTags.Add(new MangaReader.WebUI.Models.Mangadex.Tag { Id = tagIdGuid, Type = "tag", Attributes = mdTagAttrs });
                    }
                }
                 _logger.LogDebug("[MRLib Strategy Mapper] Processed {TagCount} tags for manga {MangaId}.", mangaDexTags.Count, dto.Id);
            }


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
                        (t.Attributes?.LanguageKey?.Equals("vi", StringComparison.OrdinalIgnoreCase) ?? false) );
                    
                    if (preferredTranslation?.Attributes?.Description != null)
                    {
                        description = preferredTranslation.Attributes.Description;
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
                    AltTitles = new List<Dictionary<string, string>>(),
                    Description = new Dictionary<string, string> { { dto.Attributes.OriginalLanguage ?? "en", description } },
                    Status = dto.Attributes.Status.ToString(),
                    ContentRating = dto.Attributes.ContentRating.ToString(),
                    OriginalLanguage = dto.Attributes.OriginalLanguage,
                    PublicationDemographic = dto.Attributes.PublicationDemographic?.ToString(),
                    Year = dto.Attributes.Year,
                    IsLocked = dto.Attributes.IsLocked,
                    CreatedAt = dto.Attributes.CreatedAt,
                    UpdatedAt = dto.Attributes.UpdatedAt,
                    Version = 1,
                    Tags = mangaDexTags.Any() ? mangaDexTags : null,
                },
                Relationships = relationshipsDex.Any() ? relationshipsDex : null
            };
        }

        public async Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            _logger.LogInformation("[MRLib Strategy->FetchMangaByIdsAsync] IDs: [{MangaIds}]", string.Join(", ", mangaIds));
            var mappedData = new List<Manga>();
            foreach (var idStr in mangaIds)
            {
                if (Guid.TryParse(idStr, out var guidId))
                {
                    var libResponse = await _mangaClient.GetMangaByIdAsync(guidId, new List<string> { "cover_art", "author" }); // Thêm includes
                    if (libResponse?.Data != null)
                    {
                        var mangaDexModel = await MapMangaReaderLibDtoToMangaDexModel(libResponse.Data);
                        if (mangaDexModel != null) mappedData.Add(mangaDexModel);
                    }
                }
            }
            return new MangaList { Result = "ok", Response = "collection", Data = mappedData, Limit = mappedData.Count, Total = mappedData.Count };
        }
    }
} 