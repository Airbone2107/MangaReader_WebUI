import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import tagGroupApi from '../api/tagGroupApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').TagGroup} TagGroup
 * @typedef {import('../types/api').ApiCollectionResponse<TagGroup>} TagGroupCollectionResponse
 * @typedef {import('../types/manga').CreateTagGroupRequest} CreateTagGroupRequest
 * @typedef {import('../types/manga').UpdateTagGroupRequest} UpdateTagGroupRequest
 */

const useTagGroupStore = create(persistStore((set, get) => ({
  /** @type {TagGroup[]} */
  tagGroups: [],
  totalTagGroups: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    nameFilter: '',
  },
  sort: {
    orderBy: 'name',
    ascending: true,
  },

  /**
   * Fetch tag groups from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchTagGroups: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      nameFilter: filters.nameFilter || undefined,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
    }

    try {
      /** @type {TagGroupCollectionResponse} */
      const response = await tagGroupApi.getTagGroups(queryParams)
      set({
        tagGroups: response.data,
        totalTagGroups: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch tag groups:', error)
      set({ tagGroups: [], totalTagGroups: 0 }) // Clear data on error
    }
  },

  /**
   * Handle page change for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage });
    get().fetchTagGroups(false); // Không reset pagination, chỉ fetch với page mới
  },

  /**
   * Handle rows per page change for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchTagGroups(true); // Reset page về 0 và fetch
  },

  /**
   * Handle sort change.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    get().fetchTagGroups(true); // Reset page về 0 và fetch
  },

  /**
   * Update a specific filter value in the store.
   * This does NOT trigger a fetch immediately.
   * @param {string} filterName - The name of the filter property (e.g., 'nameFilter').
   * @param {any} value - The new value for the filter.
   */
  setFilter: (filterName, value) => {
    set(state => ({
      filters: { ...state.filters, [filterName]: value }
    }));
  },

  /**
   * Apply filters and refetch tag groups.
   * @param {object} newFilters - New filter values.
   */
  applyFilters: (newFilters) => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0, // Reset page on filter change
    }));
  },

  /**
   * Reset all filters to their initial state.
   */
  resetFilters: () => {
    set({
      filters: {
        nameFilter: '',
      },
      page: 0,
    });
    get().fetchTagGroups(true);
  },

  /**
   * Delete a tag group.
   * @param {string} id - ID of the tag group to delete.
   */
  deleteTagGroup: async (id) => {
    try {
      await tagGroupApi.deleteTagGroup(id)
      showSuccessToast('Xóa nhóm tag thành công!')
      get().fetchTagGroups() // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete tag group:', error)
      // Error is handled by apiClient interceptor
    }
  },
}), 'tagGroup'))

export default useTagGroupStore 