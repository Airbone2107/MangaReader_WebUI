using MangaReaderLib.DTOs.Attributes;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với TagGroup API endpoints
    /// </summary>
    public interface ITagGroupClient
    {
        /// <summary>
        /// Lấy danh sách các nhóm tag với các tùy chọn lọc và phân trang
        /// </summary>
        Task<LibApiCollectionResponse<LibResourceObject<LibTagGroupAttributesDto>>?> GetTagGroupsAsync(
            int? offset = null, 
            int? limit = null, 
            string? nameFilter = null, 
            string? orderBy = null, 
            bool? ascending = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm tag dựa trên ID
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>?> GetTagGroupByIdAsync(
            Guid tagGroupId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Tạo một nhóm tag mới
        /// </summary>
        Task<LibApiResponse<LibResourceObject<LibTagGroupAttributesDto>>?> CreateTagGroupAsync(
            LibCreateTagGroupRequestDto request, 
            CancellationToken cancellationToken = default);
    }
} 