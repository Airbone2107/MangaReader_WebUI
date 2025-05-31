import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').ChapterPage>} ChapterPageCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').ChapterPage>} ChapterPageSingleResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/api').UploadResponseDto>} UploadPageImageResponse
 * @typedef {import('../types/manga').UpdateChapterPageDetailsRequest} UpdateChapterPageDetailsRequest
 */

const BASE_URL = '/ChapterPages';

const chapterPageApi = {
  /**
   * Tải lên ảnh cho một trang chương.
   * @param {string} pageId - ID của trang chương.
   * @param {File} file - File ảnh.
   * @returns {Promise<UploadPageImageResponse>}
   */
  uploadChapterPageImage: async (pageId, file) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post(`${BASE_URL}/${pageId}/image`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Lấy danh sách các trang của một chương.
   * @param {string} chapterId - ID của chương.
   * @param {object} [params] - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @returns {Promise<ChapterPageCollectionResponse>}
   */
  getChapterPages: async (chapterId, params) => {
    const response = await apiClient.get(`/chapters/${chapterId}/pages`, { params });
    return response.data;
  },

  /**
   * Cập nhật chi tiết trang chương.
   * @param {string} pageId - ID của trang chương.
   * @param {UpdateChapterPageDetailsRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateChapterPageDetails: async (pageId, data) => {
    await apiClient.put(`${BASE_URL}/${pageId}/details`, data);
  },

  /**
   * Xóa trang chương.
   * @param {string} pageId - ID của trang chương.
   * @returns {Promise<void>}
   */
  deleteChapterPage: async (pageId) => {
    await apiClient.delete(`${BASE_URL}/${pageId}`);
  },
};

export default chapterPageApi; 