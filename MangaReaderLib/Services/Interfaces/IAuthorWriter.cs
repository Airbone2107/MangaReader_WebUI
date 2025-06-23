using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Write-only client for Author endpoints.
    /// </summary>
    public interface IAuthorWriter : IWriteClient
    {
        Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> CreateAuthorAsync(
            CreateAuthorRequestDto request,
            CancellationToken cancellationToken = default);

        Task UpdateAuthorAsync(
            Guid authorId,
            UpdateAuthorRequestDto request,
            CancellationToken cancellationToken = default);

        Task DeleteAuthorAsync(
            Guid authorId,
            CancellationToken cancellationToken = default);
    }
} 