import apiClient from './apiClient'

/**
 * @typedef {import('../types/api').ApiResponse<import('../types/manga').CoverArt>} CoverArtSingleResponse
 */

const BASE_URL = '/CoverArts';

const coverArtApi = {
  /**
   * Lấy thông tin ảnh bìa theo ID.
   * @param {string} id - ID của ảnh bìa.
   * @returns {Promise<CoverArtSingleResponse>}
   */
  getCoverArtById: async (id) => {
    const response = await apiClient.get(`${BASE_URL}/${id}`);
    return response.data;
  },

  /**
   * Xóa ảnh bìa.
   * @param {string} id - ID của ảnh bìa.
   * @returns {Promise<void>}
   */
  deleteCoverArt: async (id) => {
    await apiClient.delete(`${BASE_URL}/${id}`);
  },
};

export default coverArtApi; 