// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\Interfaces\IMangaApiSourceStrategy.cs
using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex; // Sử dụng model của MangaDex cho kiểu trả về thống nhất

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces
{
    public interface IMangaApiSourceStrategy
    {
        Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null);
        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);
        Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId);
    }
} 