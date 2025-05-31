import { create } from 'zustand';
import chapterApi from '../api/chapterApi';
import { showSuccessToast } from '../components/common/Notification';
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants';

/**
 * @typedef {import('../types/manga').Chapter} Chapter
 * @typedef {import('../types/api').ApiCollectionResponse<Chapter>} ChapterCollectionResponse
 * @typedef {import('../types/manga').CreateChapterRequest} CreateChapterRequest
 * @typedef {import('../types/manga').UpdateChapterRequest} UpdateChapterRequest
 */

const useChapterStore = create((set, get) => ({
  /** @type {Chapter[]} */
  chapters: [],
  totalChapters: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  sort: {
    orderBy: 'chapterNumber', // Mặc định sắp xếp theo số chương
    ascending: true,
  },
  
  /**
   * Fetch chapters for a specific translated manga.
   * @param {string} translatedMangaId - The ID of the translated manga.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchChaptersByTranslatedMangaId: async (translatedMangaId, resetPagination = false) => {
    const { page, rowsPerPage, sort } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      orderBy: sort.orderBy,
      ascending: sort.ascending,
    }

    try {
      /** @type {ChapterCollectionResponse} */
      const response = await chapterApi.getChaptersByTranslatedManga(translatedMangaId, queryParams)
      set({
        chapters: response.data,
        totalChapters: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
      })
    } catch (error) {
      console.error('Failed to fetch chapters:', error)
      set({ chapters: [], totalChapters: 0 }) // Clear data on error
    }
  },

  /**
   * Set page for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   * @param {string} translatedMangaId - Current translated manga ID to refetch.
   */
  setPage: (event, newPage, translatedMangaId) => {
    set({ page: newPage }, () => get().fetchChaptersByTranslatedMangaId(translatedMangaId));
  },

  /**
   * Set rows per page for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   * @param {string} translatedMangaId - Current translated manga ID to refetch.
   */
  setRowsPerPage: (event, translatedMangaId) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 }, () =>
      get().fetchChaptersByTranslatedMangaId(translatedMangaId),
    );
  },

  /**
   * Set sort order.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   * @param {string} translatedMangaId - Current translated manga ID to refetch.
   */
  setSort: (orderBy, order, translatedMangaId) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 }, () =>
      get().fetchChaptersByTranslatedMangaId(translatedMangaId),
    );
  },

  /**
   * Delete a chapter.
   * @param {string} id - ID of the chapter to delete.
   * @param {string} translatedMangaId - ID of the translated manga to refetch list.
   */
  deleteChapter: async (id, translatedMangaId) => {
    try {
      await chapterApi.deleteChapter(id)
      showSuccessToast('Xóa chương thành công!')
      get().fetchChaptersByTranslatedMangaId(translatedMangaId) // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete chapter:', error)
      // Error is handled by apiClient interceptor
    }
  },
}));

export default useChapterStore; 