using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Read-only client for Author endpoints.
    /// </summary>
    public interface IAuthorReader : IReadClient
    {
        Task<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorsAsync(
            int? offset = null,
            int? limit = null,
            string? nameFilter = null,
            string? orderBy = null,
            bool? ascending = null,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorByIdAsync(
            Guid authorId,
            CancellationToken cancellationToken = default);
    }
} 