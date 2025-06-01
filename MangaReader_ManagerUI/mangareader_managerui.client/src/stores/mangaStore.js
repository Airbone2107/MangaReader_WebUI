import { create } from 'zustand'
import mangaApi from '../api/mangaApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Manga} Manga
 * @typedef {import('../types/api').ApiCollectionResponse<Manga>} MangaCollectionResponse
 */

const useMangaStore = create((set, get) => ({
  /** @type {Manga[]} */
  mangas: [],
  totalMangas: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    titleFilter: '',
    statusFilter: '',
    contentRatingFilter: '',
    demographicFilter: '',
    originalLanguageFilter: '',
    yearFilter: null,
    tagIdsFilter: [],
    authorIdsFilter: [],
  },
  sort: {
    orderBy: 'updatedAt',
    ascending: false, // Default to descending for updatedAt
  },

  /**
   * Fetch mangas from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchMangas: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      titleFilter: filters.titleFilter || undefined,
      statusFilter: filters.statusFilter || undefined,
      contentRatingFilter: filters.contentRatingFilter || undefined,
      demographicFilter: filters.demographicFilter || undefined,
      originalLanguageFilter: filters.originalLanguageFilter || undefined,
      yearFilter: filters.yearFilter || undefined,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
    }

    // Handle array filters explicitly for Axios (Axios params will stringify arrays correctly)
    if (filters.tagIdsFilter && filters.tagIdsFilter.length > 0) {
      queryParams['tagIdsFilter[]'] = filters.tagIdsFilter;
    }
    if (filters.authorIdsFilter && filters.authorIdsFilter.length > 0) {
      queryParams['authorIdsFilter[]'] = filters.authorIdsFilter;
    }

    try {
      /** @type {MangaCollectionResponse} */
      const response = await mangaApi.getMangas(queryParams)
      set({
        mangas: response.data,
        totalMangas: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch mangas:', error)
      set({ mangas: [], totalMangas: 0 }) // Clear data on error
    }
  },

  /**
   * Handle page change from DataTableMUI.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage }, () => get().fetchMangas())
  },

  /**
   * Handle rows per page change from DataTableMUI.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 }, () =>
      get().fetchMangas(),
    )
  },

  /**
   * Handle sort change from DataTableMUI.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 }, () =>
      get().fetchMangas(),
    )
  },

  /**
   * Apply filters and refetch mangas.
   * @param {object} newFilters - New filter values.
   */
  applyFilters: (newFilters) => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0, // Reset page on filter change
    }), () => get().fetchMangas());
  },

  /**
   * Reset all filters to their initial state.
   */
  resetFilters: () => {
    set({
      filters: {
        titleFilter: '',
        statusFilter: '',
        contentRatingFilter: '',
        demographicFilter: '',
        originalLanguageFilter: '',
        yearFilter: null,
        tagIdsFilter: [],
        authorIdsFilter: [],
      },
      page: 0,
    }, () => get().fetchMangas());
  },

  /**
   * Delete a manga.
   * @param {string} id - ID of the manga to delete.
   */
  deleteManga: async (id) => {
    try {
      await mangaApi.deleteManga(id)
      showSuccessToast('Xóa manga thành công!')
      get().fetchMangas() // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete manga:', error)
      // Error is handled by apiClient interceptor
    }
  },
}))

export default useMangaStore 