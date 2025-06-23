import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import mangaApi from '../api/mangaApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT, RELATIONSHIP_TYPES } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').Manga} Manga
 * @typedef {import('../types/api').ApiCollectionResponse<Manga>} MangaCollectionResponse
 * @typedef {import('../types/api').AuthorInRelationshipAttributes} AuthorInRelationshipAttributes
 * @typedef {import('../types/manga').CoverArtAttributes} CoverArtAttributes
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
    publicationDemographicsFilter: [],
    originalLanguageFilter: '',
    yearFilter: null,
    includedTags: [],
    includedTagsMode: 'AND',
    excludedTags: [],
    excludedTagsMode: 'OR',
    authorIdsFilter: [],
  },
  sort: {
    orderBy: 'updatedAt',
    ascending: false,
  },

  /**
   * Fetch mangas from API.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchMangas: async (resetPagination = false) => {
    const { page, rowsPerPage, filters, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    /** @type {import('../types/manga').GetMangasParams} */
    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      titleFilter: filters.titleFilter || undefined,
      statusFilter: filters.statusFilter || undefined,
      contentRatingFilter: filters.contentRatingFilter || undefined,
      publicationDemographicsFilter: filters.publicationDemographicsFilter?.length > 0 ? filters.publicationDemographicsFilter : undefined,
      originalLanguageFilter: filters.originalLanguageFilter || undefined,
      yearFilter: filters.yearFilter === null || filters.yearFilter === undefined ? undefined : filters.yearFilter,
      includedTags: filters.includedTags?.length > 0 ? filters.includedTags : undefined,
      includedTagsMode: filters.includedTags?.length > 0 ? filters.includedTagsMode : undefined,
      excludedTags: filters.excludedTags?.length > 0 ? filters.excludedTags : undefined,
      excludedTagsMode: filters.excludedTags?.length > 0 ? filters.excludedTagsMode : undefined,
      authorIdsFilter: filters.authorIdsFilter?.length > 0 ? filters.authorIdsFilter : undefined,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
      includes: ['cover_art', 'author'],
    }
    
    // Xóa các trường undefined để query string sạch hơn
    Object.keys(queryParams).forEach(key => queryParams[key] === undefined && delete queryParams[key]);

    try {
      /** @type {MangaCollectionResponse} */
      const response = await mangaApi.getMangas(queryParams)
      
      const mangasWithProcessedInfo = response.data.map(manga => {
        let coverArtPublicId = null;
        const coverArtRel = manga.relationships?.find(rel => rel.type === RELATIONSHIP_TYPES.COVER_ART);
        
        // CẬP NHẬT LOGIC: Lấy publicId từ attributes của relationship
        if (coverArtRel && coverArtRel.attributes) {
            const coverAttributes = /** @type {CoverArtAttributes} */ (coverArtRel.attributes);
            if (coverAttributes && typeof coverAttributes.publicId === 'string') {
                coverArtPublicId = coverAttributes.publicId;
            }
        }

        return { 
          ...manga, 
          coverArtPublicId,
        };
      });

      set({
        mangas: mangasWithProcessedInfo,
        totalMangas: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch mangas:', error)
      set({ mangas: [], totalMangas: 0 })
    }
  },

  /**
   * Handle page change from DataTableMUI.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   */
  setPage: (event, newPage) => {
    set({ page: newPage });
    get().fetchMangas(false); 
  },

  /**
   * Handle rows per page change from DataTableMUI.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   */
  setRowsPerPage: (event) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchMangas(true); 
  },

  /**
   * Handle sort change from DataTableMUI.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   */
  setSort: (orderBy, order) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    get().fetchMangas(true); 
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
      page: 0, 
    }));
    // fetchMangas sẽ được gọi riêng sau khi applyFilters trong component
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
        publicationDemographicsFilter: [],
        originalLanguageFilter: '',
        yearFilter: null,
        includedTags: [],
        includedTagsMode: 'AND',
        excludedTags: [],
        excludedTagsMode: 'OR',
        authorIdsFilter: [],
      },
      page: 0,
    });
    get().fetchMangas(true);
  },

  /**
   * Delete a manga.
   * @param {string} id - ID of the manga to delete.
   */
  deleteManga: async (id) => {
    try {
      await mangaApi.deleteManga(id)
      showSuccessToast('Xóa manga thành công!')
      get().fetchMangas() 
    } catch (error) {
      console.error('Failed to delete manga:', error)
    }
  },
}), 'manga'))

export default useMangaStore 