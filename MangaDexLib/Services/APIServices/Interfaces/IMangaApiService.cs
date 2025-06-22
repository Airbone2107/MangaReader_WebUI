using MangaDexLib.Models;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface IMangaApiService
    {
        Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null);
        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);
        Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId);
    }
} 