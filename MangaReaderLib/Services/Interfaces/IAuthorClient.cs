using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Author API endpoints
    /// </summary>
    public interface IAuthorClient
    {
        /// <summary>
        /// Lấy danh sách tác giả với các tùy chọn lọc và phân trang
        /// </summary>
        Task<LibApiCollectionResponse<LibResourceObject<LibAuthorAttributesDto>>?> GetAuthorsAsync(
            int? offset = null, 
            int? limit = null, 
            string? nameFilter = null, 
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một tác giả dựa trên ID
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibAuthorAttributesDto>>?> GetAuthorByIdAsync(
            Guid authorId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tạo một tác giả mới
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibAuthorAttributesDto>>?> CreateAuthorAsync(
            LibCreateAuthorRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một tác giả
        /// </summary>
        Task UpdateAuthorAsync(
            Guid authorId, 
            LibUpdateAuthorRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một tác giả
        /// </summary>
        Task DeleteAuthorAsync(
            Guid authorId, 
            CancellationToken cancellationToken = default);
    }
} 