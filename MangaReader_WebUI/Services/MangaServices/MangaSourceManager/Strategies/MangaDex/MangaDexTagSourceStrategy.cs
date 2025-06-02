using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.APIServices.Services; // Namespace của TagApiService
using MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces;

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.MangaDex
{
    public class MangaDexTagSourceStrategy : ITagApiSourceStrategy
    {
        private readonly TagApiService _mangaDexTagApiService; // Sử dụng concrete class

        public MangaDexTagSourceStrategy(TagApiService mangaDexTagApiService)
        {
            _mangaDexTagApiService = mangaDexTagApiService;
        }

        public Task<TagListResponse?> FetchTagsAsync()
        {
            return _mangaDexTagApiService.FetchTagsAsync();
        }
    }
} 