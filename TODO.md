# TODO: Khắc phục lỗi tải ảnh bìa Manga

Việc hiển thị ảnh bìa cho danh sách Manga đang gặp vấn đề do hiểu sai trường `id` trong `relationships` của API. Trường `id` cho `cover_art` là GUID của bản ghi `CoverArt`, không phải `publicId` trực tiếp trên Cloudinary. Cần thực hiện thêm một request `GET /CoverArts/{id}` để lấy `publicId` thực sự.

## 1. Cập nhật cấu trúc dữ liệu Manga ở Frontend

**File:** `MangaReader_ManagerUI\mangareader_managerui.client\src\types\manga.ts`

Chúng ta sẽ thêm một trường `coverArtPublicId` kiểu `string` vào interface `Manga` để lưu trữ `publicId` của ảnh bìa sau khi được fetch.

```typescript
// MangaReader_ManagerUI\mangareader_managerui.client\src\types\manga.ts
// ... (các import và interface khác)

export interface Manga {
  id: string
  type: 'manga'
  attributes: MangaAttributes
  relationships?: RelationshipObject[]
  // Thêm trường mới để lưu trữ publicId của ảnh bìa sau khi được xử lý ở frontend
  coverArtPublicId?: string // <-- DÒNG NÀY ĐÃ ĐƯỢC THÊM
}

// ... (các interface Request DTOs và SelectedRelationship khác)
```

## 2. Sửa đổi logic fetch Manga trong `useMangaStore.js`

**File:** `MangaReader_ManagerUI\mangareader_managerui.client\src\stores\mangaStore.js`

Logic fetch manga cần được điều chỉnh để sau khi nhận được danh sách manga từ `mangaApi.getMangas()`, chúng ta sẽ duyệt qua từng manga. Nếu có mối quan hệ `cover_art`, chúng ta sẽ thực hiện một request riêng biệt đến `coverArtApi.getCoverArtById(CoverArtId)` để lấy `publicId` và cập nhật vào đối tượng `manga` trong state.

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\stores\mangaStore.js
import { create } from 'zustand'
import { persistStore } from '../utils/zustandPersist'
import mangaApi from '../api/mangaApi'
import coverArtApi from '../api/coverArtApi' // <-- IMPORT THÊM DÒNG NÀY
import { showSuccessToast } from '../components/common/Notification'
import { DEFAULT_PAGE_LIMIT, RELATIONSHIP_TYPES } from '../constants/appConstants' // <-- IMPORT THÊM RELATIONSHIP_TYPES

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

  // ... (các hàm setPage, setRowsPerPage, setSort, setFilter, applyFilters, resetFilters, deleteManga giữ nguyên)
})) // Tên duy nhất cho persistence

export default useMangaStore
```

**Lưu ý về `RELATIONSHIP_TYPES`**: Tôi đã thêm `RELATIONSHIP_TYPES` vào `constants/appConstants.js` để tránh các chuỗi cố định ('magic strings') trong code. Hãy đảm bảo bạn có `RELATIONSHIP_TYPES` trong file đó:

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\constants\appConstants.js
export const MANGA_STATUS_OPTIONS = [
  // ...
]

// Các loại mối quan hệ (type) trong API Response
export const RELATIONSHIP_TYPES = {
  AUTHOR: 'author',
  ARTIST: 'artist',
  TAG: 'tag',
  TAG_GROUP: 'tag_group',
  COVER_ART: 'cover_art', // <-- Đảm bảo dòng này có
  MANGA: 'manga',
  USER: 'user',
  CHAPTER: 'chapter',
  CHAPTER_PAGE: 'chapter_page',
  TRANSLATED_MANGA: 'translated_manga',
}

// Other constants
// ...
```

## 3. Điều chỉnh hiển thị ảnh bìa trong `MangaTable.jsx`

**File:** `MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx`

Thay đổi cách truy cập `publicId` để sử dụng trường `coverArtPublicId` mới đã được xử lý trong store.

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaTable.jsx
// ... (các import khác)

import { CLOUDINARY_BASE_URL, MANGA_STATUS_OPTIONS, CONTENT_RATING_OPTIONS, PUBLICATION_DEMOGRAPHIC_OPTIONS } from '../../../constants/appConstants'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 */

// ... (các props và state khác)

  const columns = [
    {
      id: 'title',
      label: 'Tiêu đề',
      minWidth: 180,
      sortable: true,
      format: (value, row) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {row.coverArtPublicId ? ( // <-- THAY ĐỔI TẠI ĐÂY
            <img
              src={`${CLOUDINARY_BASE_URL}w_40,h_60,c_fill/${row.coverArtPublicId}`} // <-- THAY ĐỔI TẠI ĐÂY
              alt="Cover"
              style={{ width: 40, height: 60, objectFit: 'cover', borderRadius: 4 }}
            />
          ) : ( // Thêm placeholder nếu không có ảnh bìa
            <img
              src="https://via.placeholder.com/40x60?text=No+Cover"
              alt="No Cover"
              style={{ width: 40, height: 60, objectFit: 'cover', borderRadius: 4, border: '1px solid #ddd' }}
            />
          )}
          <span>{value}</span>
        </Box>
      )
    },
    // ... (các cột khác giữ nguyên)
  ]

  // Format data for DataTableMUI
  const formatMangaDataForTable = (mangasData) => {
    if (!mangasData) return [];
    return mangasData.map(manga => {
      // Logic để thêm `coverArtPublicId` đã được chuyển vào useMangaStore.js,
      // nên ở đây chỉ cần đảm bảo nó được truyền qua `row`.
      return {
        ...manga.attributes,
        id: manga.id, 
        relationships: manga.relationships, // Vẫn giữ relationships nếu các cột khác cần
        coverArtPublicId: manga.coverArtPublicId, // <-- ĐẢM BẢO TRƯỜNG NÀY ĐƯỢC CHUYỂN QUA TỪ STORE
      };
    });
  };

  // ... (phần render giữ nguyên)
```

## Các bước triển khai:

1.  **Cập nhật `src/types/manga.ts`**: Thêm `coverArtPublicId?: string;` vào interface `Manga`.
2.  **Cập nhật `src/constants/appConstants.js`**: Đảm bảo `RELATIONSHIP_TYPES.COVER_ART` đã được định nghĩa.
3.  **Cập nhật `src/stores/mangaStore.js`**:
    *   Import `coverArtApi`.
    *   Thêm logic `Promise.all` và duyệt qua `response.data` để gọi `coverArtApi.getCoverArtById` cho mỗi manga có mối quan hệ `cover_art`.
    *   Cập nhật đối tượng manga với `coverArtPublicId` trước khi `set` state `mangas`.
4.  **Cập nhật `src/features/manga/components/MangaTable.jsx`**:
    *   Thay đổi phần `format` của cột `title` để sử dụng `row.coverArtPublicId` thay vì tìm kiếm trong `relationships`.
    *   Thêm xử lý placeholder nếu không có ảnh bìa (`coverArtPublicId` là null/undefined).
    *   Trong hàm `formatMangaDataForTable`, đảm bảo `manga.coverArtPublicId` được truyền vào đối tượng trả về cho `DataTableMUI`.

Sau khi hoàn thành các bước này, ảnh bìa cho danh sách manga sẽ được tải chính xác theo luồng API đã mô tả.