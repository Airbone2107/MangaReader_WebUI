import { create } from 'zustand';
import chapterApi from '../api/chapterApi';
import chapterPageApi from '../api/chapterPageApi';
import { showSuccessToast } from '../components/common/Notification';
import { DEFAULT_PAGE_LIMIT } from '../constants/appConstants';

/**
 * @typedef {import('../types/manga').ChapterPage} ChapterPage
 * @typedef {import('../types/api').ApiCollectionResponse<ChapterPage>} ChapterPageCollectionResponse
 * @typedef {import('../types/manga').CreateChapterPageEntryRequest} CreateChapterPageEntryRequest
 */

const useChapterPageStore = create((set, get) => ({
  /** @type {ChapterPage[]} */
  chapterPages: [],
  totalChapterPages: 0,
  page: 0,
  rowsPerPage: DEFAULT_PAGE_LIMIT,
  
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
    }

    try {
      /** @type {ChapterPageCollectionResponse} */
      const response = await chapterPageApi.getChapterPages(chapterId, queryParams)
      set({
        chapterPages: response.data,
        totalChapterPages: response.total,
        page: resetPagination ? 0 : response.offset / response.limit,
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
    set({ page: newPage }, () => get().fetchChapterPagesByChapterId(chapterId));
  },

  /**
   * Set rows per page for pagination.
   * @param {React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>} event
   * @param {string} chapterId - Current chapter ID to refetch.
   */
  setRowsPerPage: (event, chapterId) => {
    set({ rowsPerPage: parseInt(event.target.value, 10), page: 0 }, () =>
      get().fetchChapterPagesByChapterId(chapterId),
    );
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
      // Optionally open upload dialog directly
      // setPageEntryToUploadImage({ id: pageId, pageNumber: data.pageNumber });
      // setOpenUploadImageDialog(true);
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
   */
  uploadPageImage: async (pageId, file) => {
    try {
      await chapterPageApi.uploadChapterPageImage(pageId, file);
      showSuccessToast('Tải ảnh trang chương thành công!');
      // After upload, re-fetch pages to update pagesCount and publicId
      // We don't have chapterId easily here, so trigger a full refresh or rely on parent component
      // A more robust solution might pass chapterId back from API or have a more complex state.
      // For simplicity, we'll re-fetch in the component that calls this.
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
}));

export default useChapterPageStore; 