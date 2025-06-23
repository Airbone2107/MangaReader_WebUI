import { create } from 'zustand';
import roleApi from '../api/roleApi';

/**
 * @typedef {import('../types/user').RoleDto} RoleDto
 */

const useRoleStore = create((set) => ({
  /** @type {RoleDto[]} */
  roles: [],
  isLoading: false,

  fetchRoles: async () => {
    set({ isLoading: true });
    try {
      const response = await roleApi.getRoles();
      set({ roles: response || [], isLoading: false });
    } catch (error) {
      console.error('Failed to fetch roles:', error);
      set({ isLoading: false });
    }
  },
}));

export default useRoleStore; 