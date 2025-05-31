import apiClient from './apiClient';

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').Tag>} TagCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').Tag>} TagSingleResponse
 * @typedef {import('../types/manga').CreateTagRequest} CreateTagRequest
 * @typedef {import('../types/manga').UpdateTagRequest} UpdateTagRequest
 */

const BASE_URL = '/Tags';

const tagApi = {
  /**
   * Lấy danh sách tag.
   * @param {object} params - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.tagGroupId]
   * @param {string} [params.nameFilter]
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<TagCollectionResponse>}
   */
  getTags: async (params) => {
    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Lấy thông tin tag theo ID.
   * @param {string} id - ID của tag.
   * @returns {Promise<TagSingleResponse>}
   */
  getTagById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Tạo tag mới.
   * @param {CreateTagRequest} data - Dữ liệu tag.
   * @returns {Promise<TagSingleResponse>}
   */
  createTag: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  /**
   * Cập nhật tag.
   * @param {string} id - ID của tag.
   * @param {UpdateTagRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateTag: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa tag.
   * @param {string} id - ID của tag.
   * @returns {Promise<void>}
   */
  deleteTag: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
};

export default tagApi; 