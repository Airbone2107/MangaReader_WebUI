import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import mangaApi from '../api/mangaApi'
import coverArtApi from '../api/coverArtApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT, RELATIONSHIP_TYPES } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Manga} Manga
 * @typedef {import('../types/api').ApiCollectionResponse<Manga>} MangaCollectionResponse
 */

const useMangaStore = create(persistStore((set, get) => ({
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
      
      // Bắt đầu logic mới để fetch publicId cho ảnh bìa
      const mangasWithCovers = await Promise.all(
        response.data.map(async (manga) => {
          const coverArtRelationship = manga.relationships?.find(
            (rel) => rel.type === RELATIONSHIP_TYPES.COVER_ART
          )

          if (coverArtRelationship) {
            try {
              // Lấy CoverArtId từ mối quan hệ
              const coverArtId = coverArtRelationship.id
              // Thực hiện request GET /CoverArts/{id} để lấy đối tượng CoverArt đầy đủ
              const coverArtResponse = await coverArtApi.getCoverArtById(coverArtId)
              // Trích xuất publicId và thêm vào đối tượng manga
              if (coverArtResponse && coverArtResponse.data?.attributes?.publicId) {
                return { ...manga, coverArtPublicId: coverArtResponse.data.attributes.publicId }
              }
            } catch (coverError) {
              console.warn(
                `Failed to fetch cover art publicId for manga ${manga.id}. CoverArtId: ${coverArtRelationship.id}`,
                coverError
              )
              // Nếu có lỗi khi lấy publicId, vẫn trả về manga gốc nhưng không có coverArtPublicId
              return manga 
            }
          }
          return manga // Trả về manga gốc nếu không có mối quan hệ cover_art hoặc có lỗi
        })
      )
      // Kết thúc logic mới

      set({
        mangas: mangasWithCovers, // Cập nhật state với danh sách manga đã có publicId
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
    set({ page: newPage });
    get().fetchMangas(false); // Không reset pagination, chỉ fetch với page mới
  },

  /**
   * Handle rows per page change from DataTableMUI.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchMangas(true); // Reset page về 0 và fetch
  },

  /**
   * Handle sort change from DataTableMUI.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    get().fetchMangas(true); // Reset page về 0 và fetch
  },

  /**
   * Update a specific filter value in the store.
   * This does NOT trigger a fetch immediately.
   * @param {string} filterName - The name of the filter property (e.g., 'titleFilter').
   * @param {any} value - The new value for the filter.
   */
  setFilter: (filterName, value) => {
    set(state => ({
      filters: { ...state.filters, [filterName]: value }
    }));
  },

  /**
   * Apply filters and refetch mangas.
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
    }, () => get().fetchMangas(true)); // Pass true to reset pagination implicitly before fetch
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
}), 'manga')) // Tên duy nhất cho persistence

export default useMangaStore 