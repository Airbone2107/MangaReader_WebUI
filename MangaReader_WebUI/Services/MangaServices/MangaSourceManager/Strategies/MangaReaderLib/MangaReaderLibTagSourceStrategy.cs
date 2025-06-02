// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\MangaReaderLib\MangaReaderLibTagSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaReaderLib
{
    public class MangaReaderLibTagSourceStrategy : ITagApiSourceStrategy
    {
        private readonly IMangaReaderLibTagClient _tagClient;
        private readonly IMangaReaderLibToTagListResponseMapper _tagListResponseMapper;
        private readonly ILogger<MangaReaderLibTagSourceStrategy> _logger;

        public MangaReaderLibTagSourceStrategy(
            IMangaReaderLibTagClient tagClient,
            IMangaReaderLibToTagListResponseMapper tagListResponseMapper,
            ILogger<MangaReaderLibTagSourceStrategy> logger)
        {
            _tagClient = tagClient;
            _tagListResponseMapper = tagListResponseMapper;
            _logger = logger;
        }

        public async Task<TagListResponse?> FetchTagsAsync()
        {
            _logger.LogInformation("[MRLib Strategy->FetchTagsAsync] Fetching all tags.");
            var libResponse = await _tagClient.GetTagsAsync(limit: 500); // Lấy tối đa 500 tags
            if (libResponse == null) return null;

            return _tagListResponseMapper.MapToTagListResponse(libResponse);
        }
    }
} 