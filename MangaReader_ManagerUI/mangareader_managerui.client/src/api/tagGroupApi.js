import apiClient from './apiClient'

/**
 * @typedef {import('../types/api').ApiCollectionResponse<import('../types/manga').TagGroup>} TagGroupCollectionResponse
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').TagGroup>} TagGroupSingleResponse
 * @typedef {import('../types/manga').CreateTagGroupRequest} CreateTagGroupRequest
 * @typedef {import('../types/manga').UpdateTagGroupRequest} UpdateTagGroupRequest
 */

const BASE_URL = '/TagGroups';

const tagGroupApi = {
  /**
   * Lấy danh sách nhóm tag.
   * @param {object} params - Tham số truy vấn.
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @param {string} [params.nameFilter]
   * @param {string} [params.orderBy]
   * @param {boolean} [params.ascending]
   * @returns {Promise<TagGroupCollectionResponse>}
   */
  getTagGroups: async (params) => {
    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Lấy thông tin nhóm tag theo ID.
   * @param {string} id - ID của nhóm tag.
   * @returns {Promise<TagGroupSingleResponse>}
   */
  getTagGroupById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Tạo nhóm tag mới.
   * @param {CreateTagGroupRequest} data - Dữ liệu nhóm tag.
   * @returns {Promise<TagGroupSingleResponse>}
   */
  createTagGroup: async (data) => {
    const response = await apiClient.post(BASE_URL, data);
    return response.data;
  },

  /**
   * Cập nhật nhóm tag.
   * @param {string} id - ID của nhóm tag.
   * @param {UpdateTagGroupRequest} data - Dữ liệu cập nhật.
   * @returns {Promise<void>}
   */
  updateTagGroup: async (id, data) => {
    await apiClient.put(`${BASE_URL}/${id}`, data);
  },

  /**
   * Xóa nhóm tag.
   * @param {string} id - ID của nhóm tag.
   * @returns {Promise<void>}
   */
  deleteTagGroup: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
};

export default tagGroupApi; 