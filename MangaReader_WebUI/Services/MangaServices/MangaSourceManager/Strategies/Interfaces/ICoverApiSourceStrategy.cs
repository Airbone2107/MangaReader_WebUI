// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\Interfaces\ICoverApiSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex; // Sử dụng model của MangaDex cho kiểu trả về thống nhất

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces
{
    public interface ICoverApiSourceStrategy
    {
        Task<CoverList?> GetAllCoversForMangaAsync(string mangaId);
        string GetCoverUrl(string mangaId, string fileName, int size = 512); // Đổi tên từ GetProxiedCoverUrl
    }
} 