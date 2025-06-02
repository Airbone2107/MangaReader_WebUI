using MangaReaderLib.Services.Interfaces;

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces
{
    // Đổi tên để tránh xung đột với ICoverApiService của MangaDex
    public interface IMangaReaderLibCoverApiService : ICoverArtClient
    {
        // Thêm phương thức để lấy URL ảnh bìa từ PublicId của MangaReaderLib
        string GetCoverArtUrl(string coverArtId, string publicId, int? width = null, int? height = null);
    }
} 