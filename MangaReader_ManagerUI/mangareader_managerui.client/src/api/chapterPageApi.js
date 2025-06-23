import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').ChapterPage>} ChapterPageCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').ChapterPageAttributesDto[]>} ChapterPageSyncResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').ChapterPageAttributesDto[]>} ChapterPageBatchUploadResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/api').UploadResponseDto>} UploadPageImageResponse
 * @typedef {import('../types/manga').UpdateChapterPageDetailsRequest} UpdateChapterPageDetailsRequest
 */

const BASE_URL_CHAPTERS = '/Chapters'; // Dùng cho API mới
const BASE_URL_CHAPTER_PAGES = '/ChapterPages'; // Dùng cho các API cũ nếu còn

const chapterPageApi = {
  /**
   * Lấy danh sách các trang của một chương.
   * @param {string} chapterId - ID của chương.
   * @param {object} [params] - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @returns {Promise<ChapterPageCollectionResponse>}
   */
  getChapterPages: async (chapterId, params) => {
    const response = await apiClient.get(`${BASE_URL_CHAPTERS}/${chapterId}/pages`, { params });
    return response.data;
  },

  /**
   * Đồng bộ hóa toàn bộ trang của một chapter.
   * @param {string} chapterId - ID của chương.
   * @param {FormData} formData - FormData chứa pageOperationsJson và các file ảnh.
   * @returns {Promise<ChapterPageSyncResponse>}
   */
  syncChapterPages: async (chapterId, formData) => {
    const response = await apiClient.put(`${BASE_URL_CHAPTERS}/${chapterId}/pages`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Upload hàng loạt ảnh cho chapter.
   * @param {string} chapterId - ID của chương.
   * @param {FormData} formData - FormData chứa files và pageNumbers.
   * @returns {Promise<ChapterPageBatchUploadResponse>}
   */
  batchUploadChapterPages: async (chapterId, formData) => {
    const response = await apiClient.post(`${BASE_URL_CHAPTERS}/${chapterId}/pages/batch`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  // Các hàm cũ (có thể không cần thiết nữa hoặc cần điều chỉnh nếu vẫn dùng)
  /**
   * Tải lên ảnh cho một trang chương (API cũ, có thể được thay thế bằng syncChapterPages).
   * @param {string} pageId - ID của trang chương.
   * @param {File} file - File ảnh.
   * @returns {Promise<UploadPageImageResponse>}
   */
  uploadChapterPageImage: async (pageId, file) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post(`${BASE_URL_CHAPTER_PAGES}/${pageId}/image`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Cập nhật chi tiết trang chương (API cũ, có thể được thay thế bằng syncChapterPages).
   * @param {string} pageId - ID của trang chương.
   * @param {UpdateChapterPageDetailsRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateChapterPageDetails: async (pageId, data) => {
    await apiClient.put(`${BASE_URL_CHAPTER_PAGES}/${pageId}/details`, data);
  },

  /**
   * Xóa trang chương (API cũ, có thể được thay thế bằng syncChapterPages).
   * @param {string} pageId - ID của trang chương.
   * @returns {Promise<void>}
   */
  deleteChapterPage: async (pageId) => {
    // Endpoint này có thể không còn được sử dụng trực tiếp nếu sync xử lý việc xóa
    await apiClient.delete(`${BASE_URL_CHAPTER_PAGES}/${pageId}`);
  },
};

export default chapterPageApi; 