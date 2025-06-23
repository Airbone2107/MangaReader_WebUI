using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using MangaReaderLib.DTOs.Mangas;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for Manga endpoints.
    /// </summary>
    public interface IMangaWriter : IWriteClient
    {
        /// <summary>
        /// Tạo một manga mới
        /// </summary>
        Task<ApiResponse<ResourceObject<MangaAttributesDto>>?> CreateMangaAsync(
            CreateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Cập nhật thông tin của một manga
        /// </summary>
        Task UpdateMangaAsync(
            Guid mangaId, 
            UpdateMangaRequestDto request, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Xóa một manga
        /// </summary>
        Task DeleteMangaAsync(
            Guid mangaId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tải lên bìa mới cho một manga
        /// </summary>
        Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> UploadMangaCoverAsync(
            Guid mangaId, 
            Stream imageStream, 
            string fileName, 
            string? volume = null, 
            string? description = null, 
            CancellationToken cancellationToken = default);
    }
} 