import { create } from 'zustand'
import authorApi from '../api/authorApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Author} Author
 * @typedef {import('../types/api').ApiCollectionResponse<Author>} AuthorCollectionResponse
 * @typedef {import('../types/manga').CreateAuthorRequest} CreateAuthorRequest
 * @typedef {import('../types/manga').UpdateAuthorRequest} UpdateAuthorRequest
 */

const useAuthorStore = create((set, get) => ({
  /** @type {Author[]} */
  authors: [],
  totalAuthors: 0,
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
   * Fetch authors from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchAuthors: async (resetPagination = false) => {
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
      /** @type {AuthorCollectionResponse} */
      const response = await authorApi.getAuthors(queryParams)
      set({
        authors: response.data,
        totalAuthors: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch authors:', error)
      set({ authors: [], totalAuthors: 0 }) // Clear data on error
    }
  },

  /**
   * Handle page change for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage }, () => get().fetchAuthors())
  },

  /**
   * Handle rows per page change for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 }, () =>
      get().fetchAuthors(),
    )
  },

  /**
   * Handle sort change.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 }, () =>
      get().fetchAuthors(),
    )
  },

  /**
   * Apply filters and refetch authors.
   * @param {object} newFilters - New filter values.
   */
  applyFilters: (newFilters) => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0, // Reset page on filter change
    }), () => get().fetchAuthors());
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
    }, () => get().fetchAuthors());
  },

  /**
   * Delete an author.
   * @param {string} id - ID of the author to delete.
   */
  deleteAuthor: async (id) => {
    try {
      await authorApi.deleteAuthor(id)
      showSuccessToast('Xóa tác giả thành công!')
      get().fetchAuthors() // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete author:', error)
      // Error is handled by apiClient interceptor
    }
  },
}))

export default useAuthorStore 