using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibChapterSourceStrategy : IChapterApiSourceStrategy
    {
        private readonly IMangaReaderLibChapterClient _chapterClient;
        private readonly IMangaReaderLibChapterPageClient _chapterPageClient;
        private readonly IMangaReaderLibTranslatedMangaClient _translatedMangaClient;
        private readonly IMangaReaderLibMangaClient _mangaClient; // Để lấy OriginalLanguage
        private readonly IMangaReaderLibToChapterViewModelMapper _chapterViewModelMapper;
        private readonly IMangaReaderLibToAtHomeServerResponseMapper _atHomeResponseMapper;
        private readonly ILogger<MangaReaderLibChapterSourceStrategy> _logger;
        private readonly string _mangaReaderLibApiBaseUrl;

        public MangaReaderLibChapterSourceStrategy(
            IMangaReaderLibChapterClient chapterClient,
            IMangaReaderLibChapterPageClient chapterPageClient,
            IMangaReaderLibTranslatedMangaClient translatedMangaClient,
            IMangaReaderLibMangaClient mangaClient,
            IMangaReaderLibToChapterViewModelMapper chapterViewModelMapper,
            IMangaReaderLibToAtHomeServerResponseMapper atHomeResponseMapper,
            IConfiguration configuration,
            ILogger<MangaReaderLibChapterSourceStrategy> logger)
        {
            _chapterClient = chapterClient;
            _chapterPageClient = chapterPageClient;
            _translatedMangaClient = translatedMangaClient;
            _mangaClient = mangaClient;
            _chapterViewModelMapper = chapterViewModelMapper;
            _atHomeResponseMapper = atHomeResponseMapper;
            _logger = logger;
            _mangaReaderLibApiBaseUrl = configuration["MangaReaderApiSettings:BaseUrl"]?.TrimEnd('/')
                                     ?? throw new InvalidOperationException("MangaReaderApiSettings:BaseUrl is not configured.");
        }

        public async Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Manga ID: {MangaId}, Languages: {Languages}", mangaId, languages);
            if (!Guid.TryParse(mangaId, out var guidMangaId))
            {
                _logger.LogError("[MRLib Strategy->FetchChaptersAsync] Invalid Manga ID format: {MangaId}", mangaId);
                return null;
            }

            var targetLanguages = languages.Split(',').Select(l => l.Trim().ToLowerInvariant()).Where(l => !string.IsNullOrEmpty(l)).ToList();
            if (!targetLanguages.Any()) return new ChapterList { Result = "ok", Data = new List<Chapter>() };

            var translatedMangasResponse = await _mangaClient.GetMangaTranslationsAsync(guidMangaId, limit: 100);
            if (translatedMangasResponse?.Data == null || !translatedMangasResponse.Data.Any())
            {
                _logger.LogWarning("[MRLib Strategy->FetchChaptersAsync] No translations found for Manga ID {MangaId}", mangaId);
                return new ChapterList { Result = "ok", Data = new List<Chapter>() };
            }

            var allChaptersFromLib = new List<global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Chapters.ChapterAttributesDto>>();
            string foundLanguageKey = "";

            var mangaDetails = await _mangaClient.GetMangaByIdAsync(guidMangaId);
            var originalLang = mangaDetails?.Data?.Attributes?.OriginalLanguage?.ToLowerInvariant();
            var languagesToTry = new List<string>();
            languagesToTry.AddRange(targetLanguages);
            if (!string.IsNullOrEmpty(originalLang) && !languagesToTry.Contains(originalLang)) languagesToTry.Add(originalLang);
            if (!languagesToTry.Contains("en")) languagesToTry.Add("en");

            _logger.LogDebug("[MRLib Strategy->FetchChaptersAsync] Languages to try for manga {MangaId}: [{Languages}]", mangaId, string.Join(",", languagesToTry));

            foreach (var langKey in languagesToTry)
            {
                var translatedManga = translatedMangasResponse.Data.FirstOrDefault(tm => tm.Attributes.LanguageKey.Equals(langKey, StringComparison.OrdinalIgnoreCase));
                if (translatedManga != null && Guid.TryParse(translatedManga.Id, out var tmGuid))
                {
                    var chaptersResponse = await _chapterClient.GetChaptersByTranslatedMangaAsync(tmGuid, orderBy: "ChapterNumber", ascending: order == "asc", limit: maxChapters ?? 500);
                    if (chaptersResponse?.Data != null && chaptersResponse.Data.Any())
                    {
                        _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Found {Count} chapters for lang {LanguageKey}", chaptersResponse.Data.Count, langKey);
                        allChaptersFromLib.AddRange(chaptersResponse.Data);
                        foundLanguageKey = langKey;
                        if (targetLanguages.Contains(langKey)) break;
                    }
                }
            }

            if (!allChaptersFromLib.Any())
            {
                _logger.LogWarning("[MRLib Strategy->FetchChaptersAsync] No chapters found for manga {MangaId} after trying all languages.", mangaId);
                return new ChapterList { Result = "ok", Data = new List<Chapter>(), Total = 0 };
            }
            if (string.IsNullOrEmpty(foundLanguageKey)) foundLanguageKey = "en";

            var mappedChapters = allChaptersFromLib
                .Select(dto => _chapterViewModelMapper.MapToChapterViewModel(dto, foundLanguageKey))
                .Select(vm => new Chapter
                {
                    Id = Guid.TryParse(vm.Id, out var chapterGuid) ? chapterGuid : Guid.NewGuid(),
                    Type = "chapter",
                    Attributes = new ChapterAttributes { 
                        Title = vm.Title, 
                        Volume = vm.Number,
                        ChapterNumber = vm.Number, 
                        Pages = 0,
                        TranslatedLanguage = vm.Language, 
                        PublishAt = vm.PublishedAt, 
                        ReadableAt = vm.PublishedAt, 
                        CreatedAt = vm.PublishedAt,
                        UpdatedAt = vm.PublishedAt,
                        Version = 1 
                    },
                    Relationships = vm.Relationships?.Select(r => new Relationship { Id = Guid.TryParse(r.Id, out var relGuid) ? relGuid : Guid.NewGuid(), Type = r.Type }).ToList() ?? new List<Relationship>()
                }).ToList();
            _logger.LogInformation("[MRLib Strategy->FetchChaptersAsync] Successfully mapped {Count} chapters.", mappedChapters.Count);

            return new ChapterList { Result = "ok", Response = "collection", Data = mappedChapters, Limit = mappedChapters.Count, Total = mappedChapters.Count };
        }

        public async Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Chapter ID: {ChapterId}", chapterId);
            if (!Guid.TryParse(chapterId, out var guidChapterId))
            {
                _logger.LogError("[MRLib Strategy->FetchChapterInfoAsync] Invalid Chapter ID format: {ChapterId}", chapterId);
                return null;
            }

            var libResponse = await _chapterClient.GetChapterByIdAsync(guidChapterId);
            if (libResponse?.Data?.Attributes == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Could not get chapter details from MangaReaderLib for ID {ChapterId}. Response: {ResponseResult}", chapterId, libResponse?.Result);
                return null;
            }

            string determinedLanguage = "en"; // Fallback

            var tmRelationship = libResponse.Data.Relationships?.FirstOrDefault(r => r.Type.Equals("translated_manga", StringComparison.OrdinalIgnoreCase));
            if (tmRelationship != null && Guid.TryParse(tmRelationship.Id, out var tmGuid))
            {
                var tmDetails = await _translatedMangaClient.GetTranslatedMangaByIdAsync(tmGuid);
                if (!string.IsNullOrEmpty(tmDetails?.Data?.Attributes?.LanguageKey))
                {
                    determinedLanguage = tmDetails.Data.Attributes.LanguageKey;
                }
                else
                {
                    _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Could not determine translated language for chapter {ChapterId} from TranslatedManga {TmId}. TM Response: {TmResponse}. Falling back to original manga language.", chapterId, tmGuid, tmDetails?.Result);
                    await FallbackToMangaOriginalLanguage(libResponse.Data, chapterId, determinedLanguage, (newLang) => determinedLanguage = newLang);
                }
            }
            else
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] No 'translated_manga' relationship found or invalid ID for chapter {ChapterId}. TM Relationship ID: {TmRelId}. Falling back to original manga language.", chapterId, tmRelationship?.Id);
                await FallbackToMangaOriginalLanguage(libResponse.Data, chapterId, determinedLanguage, (newLang) => determinedLanguage = newLang);
            }

            var chapterViewModel = _chapterViewModelMapper.MapToChapterViewModel(libResponse.Data, determinedLanguage);

            var mangaDexRelationships = new List<MangaReader.WebUI.Models.Mangadex.Relationship>();
            if (libResponse.Data.Relationships != null && libResponse.Data.Relationships.Any())
            {
                foreach (var rel in libResponse.Data.Relationships)
                {
                    if (rel == null || string.IsNullOrEmpty(rel.Type) || string.IsNullOrEmpty(rel.Id)) continue;

                    if (rel.Type.Equals("manga", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(rel.Id, out Guid mangaGuid))
                    {
                        mangaDexRelationships.Add(new MangaReader.WebUI.Models.Mangadex.Relationship { Id = mangaGuid, Type = "manga" });
                    }
                    else if (rel.Type.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("[MRLib Strategy->FetchChapterInfoAsync] Skipping 'user' relationship for MRLib mapping as UserID format differs. MRLib UserID: {UserId}", rel.Id);
                    }
                }
            }
            
            var chapterAttributes = new MangaReader.WebUI.Models.Mangadex.ChapterAttributes
            {
                Title = chapterViewModel.Title,
                Volume = libResponse.Data.Attributes.Volume,
                ChapterNumber = libResponse.Data.Attributes.ChapterNumber,
                Pages = libResponse.Data.Attributes.PagesCount,
                TranslatedLanguage = determinedLanguage, 
                PublishAt = chapterViewModel.PublishedAt,
                ReadableAt = chapterViewModel.PublishedAt,
                CreatedAt = libResponse.Data.Attributes.CreatedAt,
                UpdatedAt = libResponse.Data.Attributes.UpdatedAt,
                Version = 1
            };

            if (libResponse.Data.Relationships != null)
            {
                var mangaRel = libResponse.Data.Relationships.FirstOrDefault(r => r.Type == "manga");
                if (mangaRel != null && Guid.TryParse(mangaRel.Id, out Guid uploaderGuidPlaceholder))
                {
                    // chapterAttributes.Uploader = uploaderGuidPlaceholder;
                }
            }

            return new ChapterResponse
            {
                Result = "ok",
                Response = "entity",
                Data = new MangaReader.WebUI.Models.Mangadex.Chapter
                {
                    Id = Guid.Parse(chapterViewModel.Id),
                    Type = "chapter",
                    Attributes = chapterAttributes,
                    Relationships = mangaDexRelationships.Any() ? mangaDexRelationships : null
                }
            };
        }

        private async Task FallbackToMangaOriginalLanguage(global::MangaReaderLib.DTOs.Common.ResourceObject<global::MangaReaderLib.DTOs.Chapters.ChapterAttributesDto> chapterData, string chapterId, string currentDeterminedLanguage, Action<string> setLanguageAction)
        {
            var mangaRelationship = chapterData.Relationships?.FirstOrDefault(r => r.Type.Equals("manga", StringComparison.OrdinalIgnoreCase));
            if (mangaRelationship != null && Guid.TryParse(mangaRelationship.Id, out var mangaGuid))
            {
                _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Attempting to get original language for MangaId {MangaId} as fallback for chapter {ChapterId}.", mangaGuid, chapterId);
                var mangaDetails = await _mangaClient.GetMangaByIdAsync(mangaGuid);
                if (!string.IsNullOrEmpty(mangaDetails?.Data?.Attributes?.OriginalLanguage))
                {
                    setLanguageAction(mangaDetails.Data.Attributes.OriginalLanguage);
                    _logger.LogInformation("[MRLib Strategy->FetchChapterInfoAsync] Using original manga language '{OriginalLanguage}' as fallback for chapter {ChapterId}.", mangaDetails.Data.Attributes.OriginalLanguage, chapterId);
                    return;
                }
                else
                {
                    _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] Original manga language not found for MangaId {MangaId}. Falling back to 'en' for chapter {ChapterId}.", mangaGuid, chapterId);
                }
            }
            else
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterInfoAsync] 'manga' relationship not found or invalid for chapter {ChapterId} during language fallback. Falling back to 'en'.", chapterId);
            }
            setLanguageAction("en"); // Fallback cuối cùng
        }

        public async Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            _logger.LogInformation("[MRLib Strategy->FetchChapterPagesAsync] Chapter ID: {ChapterId}", chapterId);
            if (!Guid.TryParse(chapterId, out var guidChapterId)) return null;

            var pagesResponse = await _chapterPageClient.GetChapterPagesAsync(guidChapterId, limit: 500);
            if (pagesResponse == null)
            {
                _logger.LogWarning("[MRLib Strategy->FetchChapterPagesAsync] GetChapterPagesAsync returned null for ChapterId: {ChapterId}", chapterId);
                return null;
            }
            _logger.LogInformation("[MRLib Strategy->FetchChapterPagesAsync] Received {Count} page DTOs for ChapterId: {ChapterId}", pagesResponse.Data?.Count ?? 0, chapterId);

            return _atHomeResponseMapper.MapToAtHomeServerResponse(pagesResponse, chapterId, _mangaReaderLibApiBaseUrl);
        }
    }
}