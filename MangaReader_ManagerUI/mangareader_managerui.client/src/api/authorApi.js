import apiClient from './apiClient'

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').Author>} AuthorCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').Author>} AuthorSingleResponse
 * @typedef {import('../types/manga').CreateAuthorRequest} CreateAuthorRequest
 * @typedef {import('../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

const BASE_URL = '/Authors';

const authorApi = {
  /**
   * Lấy danh sách tác giả.
   * @param {object} params - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.nameFilter]
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<AuthorCollectionResponse>}
   */
  getAuthors: async (params) => {
    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Lấy thông tin tác giả theo ID.
   * @param {string} id - ID của tác giả.
   * @returns {Promise<AuthorSingleResponse>}
   */
  getAuthorById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Tạo tác giả mới.
   * @param {CreateAuthorRequest} data - Dữ liệu tác giả.
   * @returns {Promise<AuthorSingleResponse>}
   */
  createAuthor: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  /**
   * Cập nhật tác giả.
   * @param {string} id - ID của tác giả.
   * @param {UpdateAuthorRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateAuthor: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa tác giả.
   * @param {string} id - ID của tác giả.
   * @returns {Promise<void>}
   */
  deleteAuthor: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
};

export default authorApi; 