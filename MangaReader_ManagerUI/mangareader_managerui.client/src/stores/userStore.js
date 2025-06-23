import { create } from 'zustand';
import userApi from '../api/userApi';
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants';
import { persistStore } from '../utils/zustandPersist';

/**
 * @typedef {import('../types/user').UserDto} UserDto
 * @typedef {import('../types/user').PagedResult<UserDto>} PagedResult
 */

const useUserStore = create(
  persistStore((set, get) => ({
    /** @type {UserDto[]} */
    users: [],
    totalUsers: 0,
    page: 0,
    rowsPerPage: DEFAULT_PAGE_LIMIT,
    // Hiện tại không có filter và sort cho user list trên API, nhưng giữ lại cấu trúc để mở rộng sau
    filters: {},
    sort: {
      orderBy: 'userName',
      ascending: true,
    },

    /**
     * @param {boolean} [resetPagination=false]
     */
    fetchUsers: async (resetPagination = false) => {
      const { page, rowsPerPage } = get();
      const offset = resetPagination ? 0 : page * rowsPerPage;

      const queryParams = {
        offset: offset,
        limit: rowsPerPage,
      };

      try {
        /** @type {PagedResult} */
        const response = await userApi.getUsers(queryParams);
        set({
          users: response.items,
          totalUsers: response.total,
          page: resetPagination ? 0 : response.offset / response.limit,
        });
      } catch (error) {
        console.error('Failed to fetch users:', error);
        set({ users: [], totalUsers: 0 });
      }
    },

    /**
     * @param {React.MouseEvent<HTMLButtonElement> | null} event
     * @param {number} newPage
     */
    setPage: (event, newPage) => {
      set({ page: newPage });
      get().fetchUsers(false);
    },

    /**
     * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
     */
    setRowsPerPage: (event) => {
      set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
      get().fetchUsers(true);
    },
    
    // NOTE: Sorting is not supported by the current User API endpoint.
    // This function is a placeholder for future implementation.
    setSort: () => {
       console.warn("Sorting is not currently supported for users.");
       // set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
       // get().fetchUsers(true);
    },
  }), 'user')
);

export default useUserStore; 