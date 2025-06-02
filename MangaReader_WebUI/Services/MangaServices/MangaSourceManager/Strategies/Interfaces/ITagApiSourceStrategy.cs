// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\Interfaces\ITagApiSourceStrategy.cs
using MangaReader.WebUI.Models.Mangadex; // Sử dụng model của MangaDex cho kiểu trả về thống nhất

namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces
{
    public interface ITagApiSourceStrategy
    {
        Task<TagListResponse?> FetchTagsAsync();
    }
} 