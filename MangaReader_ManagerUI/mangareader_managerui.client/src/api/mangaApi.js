import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').Manga>} MangaCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').Manga>} MangaSingleResponse
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').CoverArt>} CoverArtCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').CoverArt>} CoverArtSingleResponse
 * @typedef {import('../types/manga').CreateMangaRequest} CreateMangaRequest
 * @typedef {import('../types/manga').UpdateMangaRequest} UpdateMangaRequest
 * @typedef {import('../types/manga').UploadCoverArtRequest} UploadCoverArtRequest
 */

const BASE_URL = '/Mangas';

const mangaApi = {
  /**
   * Lấy danh sách manga.
   * @param {object} params - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.titleFilter]
   * @param {string} [params.statusFilter]
   * @param {string} [params.contentRatingFilter]
   * @param {string} [params.demographicFilter]
   * @param {string} [params.originalLanguageFilter]
   * @param {number} [params.yearFilter]
   * @param {string[]} [params.tagIdsFilter] - array of GUIDs
   * @param {string[]} [params.authorIdsFilter] - array of GUIDs
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<MangaCollectionResponse>}
   */
  getMangas: async (params) => {
    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Lấy thông tin manga theo ID.
   * @param {string} id - ID của manga.
   * @returns {Promise<MangaSingleResponse>}
   */
  getMangaById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Tạo manga mới.
   * @param {CreateMangaRequest} data - Dữ liệu manga.
   * @returns {Promise<MangaSingleResponse>}
   */
  createManga: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  /**
   * Cập nhật manga.
   * @param {string} id - ID của manga.
   * @param {UpdateMangaRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateManga: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa manga.
   * @param {string} id - ID của manga.
   * @returns {Promise<void>}
   */
  deleteManga: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },

  /**
   * Lấy danh sách ảnh bìa của một manga.
   * @param {string} mangaId - ID của manga.
   * @param {object} [params] - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @returns {Promise<CoverArtCollectionResponse>}
   */
  getMangaCovers: async (mangaId, params) => {
    const response = await apiClient.get(`/mangas/${mangaId}/covers`, { params });
    return response.data;
  },

  /**
   * Upload ảnh bìa mới cho manga.
   * @param {string} mangaId - ID của manga.
   * @param {UploadCoverArtRequest} data - Dữ liệu ảnh bìa.
   * @returns {Promise<CoverArtSingleResponse>}
   */
  uploadMangaCover: async (mangaId, data) => {
    const formData = new FormData();
    formData.append('file', data.file);
    if (data.volume) {
      formData.append('volume', data.volume);
    }
    if (data.description) {
      formData.append('description', data.description);
    }

    const response = await apiClient.post(`/mangas/${mangaId}/covers`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

export default mangaApi; 