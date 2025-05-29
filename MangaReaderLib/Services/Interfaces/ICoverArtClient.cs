using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với CoverArt API endpoints
    /// </summary>
    public interface ICoverArtClient
    {
        /// <summary>
        /// Xóa một ảnh bìa dựa trên ID
        /// </summary>
        Task DeleteCoverArtAsync(Guid coverId, CancellationToken cancellationToken = default);
    }
} 