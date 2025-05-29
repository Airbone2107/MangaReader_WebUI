using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Tag API endpoints
    /// </summary>
    public interface ITagClient
    {
        /// <summary>
        /// Lấy danh sách các tag với các tùy chọn lọc và phân trang
        /// </summary>
        Task<LibApiCollectionResponse<LibResourceObject<LibTagAttributesDto>>?> GetTagsAsync(
            int? offset = null, 
            int? limit = null, 
            Guid? tagGroupId = null, 
            string? nameFilter = null, 
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một tag dựa trên ID
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibTagAttributesDto>>?> GetTagByIdAsync(
            Guid tagId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Tạo một tag mới
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibTagAttributesDto>>?> CreateTagAsync(
            LibCreateTagRequestDto request, 
            CancellationToken cancellationToken = default);
    }
} 