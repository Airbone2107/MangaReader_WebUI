import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').Chapter>} ChapterCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').Chapter>} ChapterSingleResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/api').CreateChapterPageEntryResponseDto>} ChapterPageEntryResponse
 * @typedef {import('../types/manga').CreateChapterRequest} CreateChapterRequest
 * @typedef {import('../types/manga').UpdateChapterRequest} UpdateChapterRequest
 * @typedef {import('../types/manga').CreateChapterPageEntryRequest} CreateChapterPageEntryRequest
 */

const BASE_URL = '/Chapters';

const chapterApi = {
  /**
   * Tạo chương mới.
   * @param {CreateChapterRequest} data - Dữ liệu chương.
   * @returns {Promise<ChapterSingleResponse>}
   */
  createChapter: async (data) => {
    // Note: uploadedByUserId is hardcoded as 1 for now as per Plan.md,
    // in a real app this would come from user authentication context.
    const payload = { ...data, uploadedByUserId: 1 }; 
    const response = await apiClient.post(BASE_URL, payload);
    return response.data;
  },

  /**
   * Lấy thông tin chương theo ID.
   * @param {string} id - ID của chương.
   * @returns {Promise<ChapterSingleResponse>}
   */
  getChapterById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Lấy danh sách chương của một bản dịch manga.
   * @param {string} translatedMangaId - ID của bản dịch manga.
   * @param {object} [params] - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<ChapterCollectionResponse>}
   */
  getChaptersByTranslatedManga: async (translatedMangaId, params) => {
    const response = await apiClient.get(`/translatedmangas/${translatedMangaId}/chapters`, { params });
    return response.data;
  },

  /**
   * Cập nhật chương.
   * @param {string} id - ID của chương.
   * @param {UpdateChapterRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateChapter: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa chương.
   * @param {string} id - ID của chương.
   * @returns {Promise<void>}
   */
  deleteChapter: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },

  /**
   * Tạo entry (bản ghi) cho một trang chương mới.
   * @param {string} chapterId - ID của chương.
   * @param {CreateChapterPageEntryRequest} data - Dữ liệu trang (chủ yếu là pageNumber).
   * @returns {Promise<ChapterPageEntryResponse>}
   */
  createChapterPageEntry: async (chapterId, data) => {
    const response = await apiClient.post(`${BASE_URL}/${chapterId}/pages/entry`, data);
    return response.data;
  },
};

export default chapterApi; 