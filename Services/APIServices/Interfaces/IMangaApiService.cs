using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;

namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    public interface IMangaApiService
    {
        Task<MangaList?> FetchMangaAsync(int? limit = null, int? offset = null, SortManga? sortManga = null);
        Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId);
        Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds);
        // Có thể thêm các phương thức khác liên quan đến Manga nếu cần
    }
} 