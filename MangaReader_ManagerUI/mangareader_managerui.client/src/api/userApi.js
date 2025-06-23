import apiClient from './apiClient';

/**
 * @typedef {import('../types/user').UserDto} UserDto
 * @typedef {import('../types/api').ApiCollectionResponse<UserDto>} UserCollectionResponse
 * @typedef {import('../types/user').CreateUserRequestDto} CreateUserRequestDto
 * @typedef {import('../types/user').UpdateUserRolesRequestDto} UpdateUserRolesRequestDto
 */

const BASE_URL = '/users'; // Sửa: Bỏ '/api' để tránh lặp lại tiền tố

const userApi = {
  /**
   * Lấy danh sách người dùng.
   * @param {object} params
   * @param {number} [params.offset]
   * @param {number} [params.limit]
   * @returns {Promise<UserCollectionResponse>}
   */
  getUsers: async (params) => {
    const response = await apiClient.get(BASE_URL, { params });
    return response.data;
  },

  /**
   * Tạo người dùng mới.
   * @param {CreateUserRequestDto} data
   * @returns {Promise<void>}
   */
  createUser: async (data) => {
    await apiClient.post(BASE_URL, data);
  },

  /**
   * Cập nhật vai trò cho người dùng.
   * @param {string} userId
   * @param {UpdateUserRolesRequestDto} data
   * @returns {Promise<void>}
   */
  updateUserRoles: async (userId, data) => {
    await apiClient.put(`${BASE_URL}/${userId}/roles`, data);
  },
};

export default userApi; 