import { create } from 'zustand'
import translatedMangaApi from '../api/translatedMangaApi'
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants'

/**
 * @typedef {import('../types/manga').TranslatedManga} TranslatedManga
 * @typedef {import('../types/api').ApiCollectionResponse<TranslatedManga>} TranslatedMangaCollectionResponse
 * @typedef {import('../types/manga').CreateTranslatedMangaRequest} CreateTranslatedMangaRequest
 * @typedef {import('../types/manga').UpdateTranslatedMangaRequest} UpdateTranslatedMangaRequest
 */

const useTranslatedMangaStore = create((set, get) => ({
  /** @type {TranslatedManga[]} */
  translatedMangas: [],
  totalTranslatedMangas: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    // Không có filter cho TranslatedManga List vì nó luôn theo mangaId
    // Có thể thêm languageKeyFilter nếu cần ở màn hình tổng quan, nhưng ở đây dùng Path Param
  },
  sort: {
    orderBy: 'languageKey', // Mặc định sắp xếp theo ngôn ngữ
    ascending: true,
  },
  
  /**
   * Fetch translated mangas for a specific mangaId.
   * @param {string} mangaId - The ID of the original manga.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchTranslatedMangasByMangaId: async (mangaId, resetPagination = false) => {
    const { page, rowsPerPage, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
    }

    try {
      /** @type {TranslatedMangaCollectionResponse} */
      const response = await translatedMangaApi.getMangaTranslations(mangaId, queryParams)
      set({
        translatedMangas: response.data,
        totalTranslatedMangas: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch translated mangas:', error)
      set({ translatedMangas: [], totalTranslatedMangas: 0 }) // Clear data on error
    }
  },

  /**
   * Set page for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   * @param {string} mangaId - Current manga ID to refetch.
   */
  setPage: (event, newPage, mangaId) => {
    set({ page: newPage }, () => get().fetchTranslatedMangasByMangaId(mangaId));
  },

  /**
   * Set rows per page for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   * @param {string} mangaId - Current manga ID to refetch.
   */
  setRowsPerPage: (event, mangaId) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 }, () =>
      get().fetchTranslatedMangasByMangaId(mangaId),
    );
  },

  /**
   * Set sort order.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   * @param {string} mangaId - Current manga ID to refetch.
   */
  setSort: (orderBy, order, mangaId) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 }, () =>
      get().fetchTranslatedMangasByMangaId(mangaId),
    );
  },

  /**
   * Delete a translated manga.
   * @param {string} id - ID of the translated manga to delete.
   * @param {string} mangaId - ID of the original manga to refetch list.
   */
  deleteTranslatedManga: async (id, mangaId) => {
    try {
      await translatedMangaApi.deleteTranslatedManga(id)
      showSuccessToast('Xóa bản dịch manga thành công!')
      get().fetchTranslatedMangasByMangaId(mangaId) // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete translated manga:', error)
      // Error is handled by apiClient interceptor
    }
  },
}))

export default useTranslatedMangaStore 