using MangaReader.WebUI.Enums;
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Để sử dụng MangaList, MangaResponse, ChapterList, AtHomeServerResponse, TagListResponse
using MangaReader.WebUI.Services.APIServices.Interfaces;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.APIServices.Services; // Để inject các concrete MangaDex API Services
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager
{
    // MangaSourceManagerService sẽ triển khai tất cả các interface API chính
    public class MangaSourceManagerService : IMangaApiService, IChapterApiService, ICoverApiService, ITagApiService, IApiStatusService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MangaSourceManagerService> _logger;

        // MangaDex API Services (inject dưới dạng concrete class vì chúng không còn là interface được đăng ký)
        private readonly MangaApiService _mangaDexApiService;
        private readonly ChapterApiService _mangaDexChapterApiService;
        private readonly CoverApiService _mangaDexCoverApiService;
        private readonly TagApiService _mangaDexTagApiService;
        private readonly ApiStatusService _mangaDexApiStatusService;

        // MangaReaderLib API Clients
        private readonly IMangaReaderLibMangaClient _mangaReaderLibMangaClient;
        private readonly IMangaReaderLibChapterClient _mangaReaderLibChapterClient;
        private readonly IMangaReaderLibChapterPageClient _mangaReaderLibChapterPageClient;
        private readonly IMangaReaderLibCoverApiService _mangaReaderLibCoverApiService;
        private readonly IMangaReaderLibTagClient _mangaReaderLibTagClient;
        private readonly IMangaReaderLibTranslatedMangaClient _mangaReaderLibTranslatedMangaClient; // Cần để lấy Chapters của TranslatedManga

        // Mappers (cho cả MangaDex và MangaReaderLib)
        private readonly IMangaToMangaViewModelMapper _mangaDexToMangaViewModelMapper;
        private readonly IMangaReaderLibToMangaViewModelMapper _mangaReaderLibToMangaViewModelMapper;
        private readonly IChapterToChapterViewModelMapper _mangaDexToChapterViewModelMapper;
        private readonly IMangaReaderLibToChapterViewModelMapper _mangaReaderLibToChapterViewModelMapper;
        private readonly IMangaReaderLibToTagListResponseMapper _mangaReaderLibToTagListResponseMapper;
        private readonly IMangaReaderLibToAtHomeServerResponseMapper _mangaReaderLibToAtHomeServerResponseMapper;

        private readonly MangaDexMangaSourceStrategy _mangaDexMangaStrategy;
        private readonly MangaDexChapterSourceStrategy _mangaDexChapterStrategy;
        private readonly MangaDexCoverSourceStrategy _mangaDexCoverStrategy;
        private readonly MangaDexTagSourceStrategy _mangaDexTagStrategy;
        private readonly MangaDexApiStatusSourceStrategy _mangaDexApiStatusStrategy;

        private readonly MangaReaderLibMangaSourceStrategy _mangaReaderLibMangaStrategy;
        private readonly MangaReaderLibChapterSourceStrategy _mangaReaderLibChapterStrategy;
        private readonly MangaReaderLibCoverSourceStrategy _mangaReaderLibCoverStrategy;
        private readonly MangaReaderLibTagSourceStrategy _mangaReaderLibTagStrategy;
        private readonly MangaReaderLibApiStatusSourceStrategy _mangaReaderLibApiStatusStrategy;

        public MangaSourceManagerService(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<MangaSourceManagerService> logger,
            // MangaDex Concrete Services
            MangaApiService mangaDexApiService,
            ChapterApiService mangaDexChapterApiService,
            CoverApiService mangaDexCoverApiService,
            TagApiService mangaDexTagApiService,
            ApiStatusService mangaDexApiStatusService,
            // MangaReaderLib Client Interfaces
            IMangaReaderLibMangaClient mangaReaderLibMangaClient,
            IMangaReaderLibChapterClient mangaReaderLibChapterClient,
            IMangaReaderLibChapterPageClient mangaReaderLibChapterPageClient,
            IMangaReaderLibCoverApiService mangaReaderLibCoverApiService,
            IMangaReaderLibTagClient mangaReaderLibTagClient,
            IMangaReaderLibTranslatedMangaClient mangaReaderLibTranslatedMangaClient,
            // Mappers
            IMangaToMangaViewModelMapper mangaDexToMangaViewModelMapper,
            IMangaReaderLibToMangaViewModelMapper mangaReaderLibToMangaViewModelMapper,
            IChapterToChapterViewModelMapper mangaDexToChapterViewModelMapper,
            IMangaReaderLibToChapterViewModelMapper mangaReaderLibToChapterViewModelMapper,
            IMangaReaderLibToTagListResponseMapper mangaReaderLibToTagListResponseMapper,
            IMangaReaderLibToAtHomeServerResponseMapper mangaReaderLibToAtHomeServerResponseMapper,
            MangaDexMangaSourceStrategy mangaDexMangaStrategy,
            MangaDexChapterSourceStrategy mangaDexChapterStrategy,
            MangaDexCoverSourceStrategy mangaDexCoverStrategy,
            MangaDexTagSourceStrategy mangaDexTagStrategy,
            MangaDexApiStatusSourceStrategy mangaDexApiStatusStrategy,
            MangaReaderLibMangaSourceStrategy mangaReaderLibMangaStrategy,
            MangaReaderLibChapterSourceStrategy mangaReaderLibChapterStrategy,
            MangaReaderLibCoverSourceStrategy mangaReaderLibCoverStrategy,
            MangaReaderLibTagSourceStrategy mangaReaderLibTagStrategy,
            MangaReaderLibApiStatusSourceStrategy mangaReaderLibApiStatusStrategy)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;

            _mangaDexApiService = mangaDexApiService;
            _mangaDexChapterApiService = mangaDexChapterApiService;
            _mangaDexCoverApiService = mangaDexCoverApiService;
            _mangaDexTagApiService = mangaDexTagApiService;
            _mangaDexApiStatusService = mangaDexApiStatusService;

            _mangaReaderLibMangaClient = mangaReaderLibMangaClient;
            _mangaReaderLibChapterClient = mangaReaderLibChapterClient;
            _mangaReaderLibChapterPageClient = mangaReaderLibChapterPageClient;
            _mangaReaderLibCoverApiService = mangaReaderLibCoverApiService;
            _mangaReaderLibTagClient = mangaReaderLibTagClient;
            _mangaReaderLibTranslatedMangaClient = mangaReaderLibTranslatedMangaClient;

            _mangaDexToMangaViewModelMapper = mangaDexToMangaViewModelMapper;
            _mangaReaderLibToMangaViewModelMapper = mangaReaderLibToMangaViewModelMapper;
            _mangaDexToChapterViewModelMapper = mangaDexToChapterViewModelMapper;
            _mangaReaderLibToChapterViewModelMapper = mangaReaderLibToChapterViewModelMapper;
            _mangaReaderLibToTagListResponseMapper = mangaReaderLibToTagListResponseMapper;
            _mangaReaderLibToAtHomeServerResponseMapper = mangaReaderLibToAtHomeServerResponseMapper;

            _mangaDexMangaStrategy = mangaDexMangaStrategy;
            _mangaDexChapterStrategy = mangaDexChapterStrategy;
            _mangaDexCoverStrategy = mangaDexCoverStrategy;
            _mangaDexTagStrategy = mangaDexTagStrategy;
            _mangaDexApiStatusStrategy = mangaDexApiStatusStrategy;

            _mangaReaderLibMangaStrategy = mangaReaderLibMangaStrategy;
            _mangaReaderLibChapterStrategy = mangaReaderLibChapterStrategy;
            _mangaReaderLibCoverStrategy = mangaReaderLibCoverStrategy;
            _mangaReaderLibTagStrategy = mangaReaderLibTagStrategy;
            _mangaReaderLibApiStatusStrategy = mangaReaderLibApiStatusStrategy;
        }

        /// <summary>
        /// Đọc nguồn truyện hiện tại từ cookie "MangaSource". Mặc định là MangaDex.
        /// </summary>
        private MangaSource GetCurrentMangaSource()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Cookies.TryGetValue("MangaSource", out var sourceString))
            {
                if (Enum.TryParse(sourceString, true, out MangaSource source))
                {
                    _logger.LogInformation("MangaSourceManager: Current Manga Source from cookie: {Source}", source);
                    return source;
                }
            }
            _logger.LogInformation("MangaSourceManager: No Manga Source cookie found or invalid. Defaulting to MangaDex.");
            return MangaSource.MangaDex;
        }

        // IMangaApiService implementation
        public Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchMangaAsync called for source {Source}", source);
            return source == MangaSource.MangaDex
                ? _mangaDexMangaStrategy.FetchMangaAsync(limit, offset, sortManga)
                : _mangaReaderLibMangaStrategy.FetchMangaAsync(limit, offset, sortManga);
        }

        public Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)
        {
            var source = GetCurrentMangaSource();
             _logger.LogInformation("MangaSourceManager: FetchMangaByIdsAsync called for source {Source}", source);
            return source == MangaSource.MangaDex
                ? _mangaDexMangaStrategy.FetchMangaByIdsAsync(mangaIds)
                : _mangaReaderLibMangaStrategy.FetchMangaByIdsAsync(mangaIds);
        }

        public Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchMangaDetailsAsync called for source {Source}, MangaId: {MangaId}", source, mangaId);
            return source == MangaSource.MangaDex
                ? _mangaDexMangaStrategy.FetchMangaDetailsAsync(mangaId)
                : _mangaReaderLibMangaStrategy.FetchMangaDetailsAsync(mangaId);
        }

        // IChapterApiService implementation
        public Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order = "desc", int? maxChapters = null)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchChaptersAsync called for source {Source}, MangaId: {MangaId}", source, mangaId);
            return source == MangaSource.MangaDex
                ? _mangaDexChapterStrategy.FetchChaptersAsync(mangaId, languages, order, maxChapters)
                : _mangaReaderLibChapterStrategy.FetchChaptersAsync(mangaId, languages, order, maxChapters);
        }

        public Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchChapterInfoAsync called for source {Source}, ChapterId: {ChapterId}", source, chapterId);
            return source == MangaSource.MangaDex
                ? _mangaDexChapterStrategy.FetchChapterInfoAsync(chapterId)
                : _mangaReaderLibChapterStrategy.FetchChapterInfoAsync(chapterId);
        }

        public Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchChapterPagesAsync called for source {Source}, ChapterId: {ChapterId}", source, chapterId);
            return source == MangaSource.MangaDex
                ? _mangaDexChapterStrategy.FetchChapterPagesAsync(chapterId)
                : _mangaReaderLibChapterStrategy.FetchChapterPagesAsync(chapterId);
        }

        // ICoverApiService implementation
        public Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: GetAllCoversForMangaAsync called for source {Source}, MangaId: {MangaId}", source, mangaId);
            return source == MangaSource.MangaDex
                ? _mangaDexCoverStrategy.GetAllCoversForMangaAsync(mangaId)
                : _mangaReaderLibCoverStrategy.GetAllCoversForMangaAsync(mangaId);
        }

        public string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: GetProxiedCoverUrl called for source {Source}, MangaId: {MangaId}, FileName: {FileName}", source, mangaId, fileName);
            return source == MangaSource.MangaDex
                ? _mangaDexCoverStrategy.GetCoverUrl(mangaId, fileName, size)
                : _mangaReaderLibCoverStrategy.GetCoverUrl(mangaId, fileName, size);
        }

        // ITagApiService implementation
        public Task<TagListResponse?> FetchTagsAsync()
        {
            var source = GetCurrentMangaSource();
            _logger.LogInformation("MangaSourceManager: FetchTagsAsync called for source {Source}", source);
            return source == MangaSource.MangaDex
                ? _mangaDexTagStrategy.FetchTagsAsync()
                : _mangaReaderLibTagStrategy.FetchTagsAsync();
        }

        // IApiStatusService implementation
        public Task<bool> TestConnectionAsync()
        {
            var source = GetCurrentMangaSource();
             _logger.LogInformation("MangaSourceManager: TestConnectionAsync called for source {Source}", source);
            return source == MangaSource.MangaDex
                ? _mangaDexApiStatusStrategy.TestConnectionAsync()
                : _mangaReaderLibApiStatusStrategy.TestConnectionAsync();
        }
    }
} 