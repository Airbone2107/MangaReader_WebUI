using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với CoverArt API endpoints
    /// </summary>
    public interface ICoverArtClient
    {
        /// <summary>
        /// Lấy thông tin chi tiết của một ảnh bìa dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> GetCoverArtByIdAsync(
            Guid coverId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một ảnh bìa dựa trên ID
        /// </summary>
        Task DeleteCoverArtAsync(Guid coverId, CancellationToken cancellationToken = default);
    }
} 