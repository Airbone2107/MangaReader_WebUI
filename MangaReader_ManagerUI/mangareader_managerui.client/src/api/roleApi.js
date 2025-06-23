import apiClient from './apiClient';

/**
 * @typedef {import('../types/user').RoleDto} RoleDto
 * @typedef {import('../types/user').RoleDetailsDto} RoleDetailsDto
 * @typedef {import('../types/user').UpdateRolePermissionsRequestDto} UpdateRolePermissionsRequestDto
 */

const BASE_URL = '/roles';

const roleApi = {
  /**
   * @returns {Promise<RoleDto[]>}
   */
  getRoles: async () => {
    const response = await apiClient.get(BASE_URL);
    return response.data;
  },

  /**
   * @param {string} roleId
   * @returns {Promise<RoleDetailsDto>}
   */
  getRolePermissions: async (roleId) => {
    const response = await apiClient.get(`${BASE_URL}/${roleId}/permissions`);
    return response.data;
  },

  /**
   * @param {string} roleId
   * @param {UpdateRolePermissionsRequestDto} data
   * @returns {Promise<void>}
   */
  updateRolePermissions: async (roleId, data) => {
    await apiClient.put(`${BASE_URL}/${roleId}/permissions`, data);
  },
};

export default roleApi; 