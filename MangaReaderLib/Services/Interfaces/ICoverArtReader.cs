using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for CoverArt endpoints.
    /// </summary>
    public interface ICoverArtReader : IReadClient
    {
        /// <summary>
        /// Lấy thông tin chi tiết của một ảnh bìa dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<CoverArtAttributesDto>>?> GetCoverArtByIdAsync(
            Guid coverId,
            CancellationToken cancellationToken = default);
    }
} 