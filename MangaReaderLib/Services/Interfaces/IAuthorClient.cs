using MangaReaderLib.DTOs.Authors;
using MangaReaderLib.DTOs.Common;

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
        Task<ApiCollectionResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorsAsync(
            int? offset = null, 
            int? limit = null, 
            string? nameFilter = null, 
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một tác giả dựa trên ID
        /// </summary>
        Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> GetAuthorByIdAsync(
            Guid authorId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tạo một tác giả mới
        /// </summary>
        Task<ApiResponse<ResourceObject<AuthorAttributesDto>>?> CreateAuthorAsync(
            CreateAuthorRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin của một tác giả
        /// </summary>
        Task UpdateAuthorAsync(
            Guid authorId, 
            UpdateAuthorRequestDto request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một tác giả
        /// </summary>
        Task DeleteAuthorAsync(
            Guid authorId, 
            CancellationToken cancellationToken = default);
    }
} 