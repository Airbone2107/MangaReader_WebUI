import { create } from 'zustand';
import { persistStore } from '../utils/zustandPersist';
import chapterApi from '../api/chapterApi';
import chapterPageApi from '../api/chapterPageApi';
import { showSuccessToast } from '../components/common/Notification';
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants';

/**
 * @typedef {import('../types/manga').ChapterPage} ChapterPage
 * @typedef {import('../types/api').ApiCollectionResponse<ChapterPage>} ChapterPageCollectionResponse
 * @typedef {import('../types/manga').CreateChapterPageEntryRequest} CreateChapterPageEntryRequest
 */

const useChapterPageStore = create(persistStore((set, get) => ({
  /** @type {ChapterPage[]} */
  chapterPages: [],
  totalChapterPages: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  filters: {
    // Không có filter riêng cho ChapterPages trên API, nên để trống
  },
  sort: {
    orderBy: 'pageNumber', // Mặc định sắp xếp theo số trang
    ascending: true,
  },
  
  /**
   * Fetch chapter pages for a specific chapter.
   * @param {string} chapterId - The ID of the chapter.
   * @param {boolean} [resetPagination=false] - Whether to reset page and offset.
   */
  fetchChapterPagesByChapterId: async (chapterId, resetPagination = false) => {
    const { page, rowsPerPage } = get()
    const offset = resetPagination ? 0 : page * rowsPerPage

    const queryParams = {
      offset: offset,
      limit: rowsPerPage,
      // API Chapters/{chapterId}/pages không hỗ trợ orderBy hay ascending,
      // nhưng chúng ta vẫn quản lý trong store để nhất quán và cho tương lai.
      // API response không trả về offset/limit cho ChapterPages nên việc tính page index sẽ đơn giản.
      // Mặc định API sắp xếp theo pageNumber, nên khớp với sort.orderBy.
    }

    try {
      /** @type {ChapterPageCollectionResponse} */
      const response = await chapterPageApi.getChapterPages(chapterId, queryParams)
      set({
        chapterPages: response.data,
        totalChapterPages: response.total,
        page: resetPagination ? 0 : response.offset / response.limit, // Cập nhật page
      })
    } catch (error) {
      console.error('Failed to fetch chapter pages:', error)
      set({ chapterPages: [], totalChapterPages: 0 }) // Clear data on error
    }
  },

  /**
   * Set page for pagination.
   * @param {React.MouseEvent<HTMLButtonElement> | null} event
   * @param {number} newPage
   * @param {string} chapterId - Current chapter ID to refetch.
   */
  setPage: (event, newPage, chapterId) => {
    set({ page: newPage });
    get().fetchChapterPagesByChapterId(chapterId);
  },

  /**
   * Set rows per page for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   * @param {string} chapterId - Current chapter ID to refetch.
   */
  setRowsPerPage: (event, chapterId) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 });
    get().fetchChapterPagesByChapterId(chapterId, true); // Reset page về 0 và fetch
  },

  /**
   * Set sort order.
   * @param {string} orderBy - The field to sort by.
   * @param {'asc' | 'desc'} order - The sort order.
   * @param {string} chapterId - Current chapter ID to refetch.
   */
  setSort: (orderBy, order, chapterId) => {
    set({ sort: { orderBy, ascending: order === 'asc' }, page: 0 });
    // API Chapters/{chapterId}/pages hiện không hỗ trợ sorting, nên sort này chỉ có ý nghĩa trên client-side nếu tự sắp xếp
    // hoặc là để chuẩn bị cho API hỗ trợ sau này.
    get().fetchChapterPagesByChapterId(chapterId, true); // Reset page về 0 và fetch
  },

  // Phương thức setFilter, applyFilters, resetFilters có thể thêm vào nếu ChapterPages có filter riêng trên UI
  setFilter: (filterName, value) => {
    set(state => ({
      filters: { ...state.filters, [filterName]: value }
    }));
  },
  applyFilters: (newFilters) => {
    set((state) => ({
      filters: { ...state.filters, ...newFilters },
      page: 0,
    }));
  },
  resetFilters: () => {
    set({
      filters: {},
      page: 0,
    });
  },

  /**
   * Create a chapter page entry.
   * @param {string} chapterId - The ID of the chapter.
   * @param {CreateChapterPageEntryRequest} data - Page entry data.
   * @returns {Promise<string | null>} - The pageId if successful, null otherwise.
   */
  createPageEntry: async (chapterId, data) => {
    try {
      const response = await chapterApi.createChapterPageEntry(chapterId, data);
      showSuccessToast('Tạo entry trang chương thành công!');
      get().fetchChapterPagesByChapterId(chapterId); // Refresh pages after creating new entry
      return response.data.pageId; // Return pageId for subsequent upload
    } catch (error) {
      console.error('Failed to create page entry:', error);
      // handleApiError already called by apiClient interceptor
      return null;
    }
  },

  /**
   * Upload image for a chapter page.
   * @param {string} pageId - The ID of the chapter page entry.
   * @param {File} file - The image file to upload.
   * @param {string} chapterId - The ID of the chapter to re-fetch pages.
   */
  uploadPageImage: async (pageId, file, chapterId) => { // Thêm chapterId vào đây
    try {
      await chapterPageApi.uploadChapterPageImage(pageId, file);
      showSuccessToast('Tải ảnh trang chương thành công!');
      get().fetchChapterPagesByChapterId(chapterId); // Refresh list after upload
    } catch (error) {
      console.error('Failed to upload page image:', error);
      // handleApiError already called
    }
  },

  /**
   * Delete a chapter page.
   * @param {string} pageId - ID of the chapter page to delete.
   * @param {string} chapterId - ID of the chapter to refetch list.
   */
  deleteChapterPage: async (pageId, chapterId) => {
    try {
      await chapterPageApi.deleteChapterPage(pageId);
      showSuccessToast('Xóa trang chương thành công!');
      get().fetchChapterPagesByChapterId(chapterId); // Refresh list after deletion
    } catch (error) {
      console.error('Failed to delete chapter page:', error);
      // Error is handled by apiClient interceptor
    }
  },
}), 'chapterPage')); // Tên duy nhất cho persistence

export default useChapterPageStore; 