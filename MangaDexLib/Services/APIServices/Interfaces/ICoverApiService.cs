using MangaDexLib.Models;

namespace MangaDexLib.Services.APIServices.Interfaces
{
    public interface ICoverApiService
    {
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);
        string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512);
    }
} 