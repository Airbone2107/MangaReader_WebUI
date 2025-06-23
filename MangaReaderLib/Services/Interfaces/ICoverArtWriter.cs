using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for CoverArt endpoints.
    /// </summary>
    public interface ICoverArtWriter : IWriteClient
    {
        /// <summary>
        /// Xóa một ảnh bìa dựa trên ID
        /// </summary>
        Task DeleteCoverArtAsync(Guid coverId, CancellationToken cancellationToken = default);
    }
} 