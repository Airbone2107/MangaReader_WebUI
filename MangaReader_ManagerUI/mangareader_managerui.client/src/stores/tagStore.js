import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import tagApi from '../api/tagApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Tag} Tag
 * @typedef {import('../types/api').ApiCollectionResponse<Tag>} TagCollectionResponse
 * @typedef {import('../types/manga').CreateTagRequest} CreateTagRequest
 * @typedef {import('../types/manga').UpdateTagRequest} UpdateTagRequest
 */

const useTagStore = create(persistStore((set, get) => ({
  /** @type {Tag[]} */
  tags: [],
  totalTags: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    nameFilter: '',
    tagGroupId: undefined, // Optional: filter by tag group
  },
  sort: {
    orderBy: 'name',
    ascending: true,
  },

  /**
   * Fetch tags from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchTags: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      nameFilter: filters.nameFilter || undefined,
      tagGroupId: filters.tagGroupId || undefined,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
    }

    try {
      /** @type {TagCollectionResponse} */
      const response = await tagApi.getTags(queryParams)
      set({
        tags: response.data,
        totalTags: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch tags:', error)
      set({ tags: [], totalTags: 0 }) // Clear data on error
    }
  },

  /**
   * Handle page change for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage });
    get().fetchTags(false); // Không reset pagination, chỉ fetch với page mới
  },

  /**
   * Handle rows per page change for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchTags(true); // Reset page về 0 và fetch
  },

  /**
   * Handle sort change.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    get().fetchTags(true); // Reset page về 0 và fetch
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
   * Apply filters and refetch tags.
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
        tagGroupId: undefined,
      },
      page: 0,
    });
    get().fetchTags(true);
  },

  /**
   * Delete a tag.
   * @param {string} id - ID of the tag to delete.
   */
  deleteTag: async (id) => {
    try {
      await tagApi.deleteTag(id)
      showSuccessToast('Xóa tag thành công!')
      get().fetchTags() // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete tag:', error)
      // Error is handled by apiClient interceptor
    }
  },
}), 'tag'))

export default useTagStore 