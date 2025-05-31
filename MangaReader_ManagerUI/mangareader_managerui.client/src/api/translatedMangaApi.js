import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').TranslatedManga>} TranslatedMangaCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').TranslatedManga>} TranslatedMangaSingleResponse
 * @typedef {import('../types/manga').CreateTranslatedMangaRequest} CreateTranslatedMangaRequest
 * @typedef {import('../types/manga').UpdateTranslatedMangaRequest} UpdateTranslatedMangaRequest
 */

const BASE_URL = '/TranslatedMangas';

const translatedMangaApi = {
  /**
   * Tạo bản dịch manga mới.
   * @param {CreateTranslatedMangaRequest} data - Dữ liệu bản dịch.
   * @returns {Promise<TranslatedMangaSingleResponse>}
   */
  createTranslatedManga: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  /**
   * Lấy thông tin bản dịch manga theo ID.
   * @param {string} id - ID của bản dịch manga.
   * @returns {Promise<TranslatedMangaSingleResponse>}
   */
  getTranslatedMangaById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Lấy danh sách bản dịch của một manga gốc.
   * @param {string} mangaId - ID của manga gốc.
   * @param {object} [params] - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<TranslatedMangaCollectionResponse>}
   */
  getMangaTranslations: async (mangaId, params) => {
    const response = await apiClient.get(`/mangas/${mangaId}/translations`, { params });
    return response.data;
  },

  /**
   * Cập nhật bản dịch manga.
   * @param {string} id - ID của bản dịch manga.
   * @param {UpdateTranslatedMangaRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateTranslatedManga: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa bản dịch manga.
   * @param {string} id - ID của bản dịch manga.
   * @returns {Promise<void>}
   */
  deleteTranslatedManga: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
};

export default translatedMangaApi; 