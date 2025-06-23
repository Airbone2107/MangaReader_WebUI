<!-- Update.md -->
# Thông Báo Cập Nhật API (Tổng Hợp)

Tài liệu này tổng hợp các thay đổi API quan trọng dành cho Frontend/Client, được áp dụng theo thứ tự.

## Mục lục

1.  [Thay đổi chung về cấu trúc Response](#1-thay-đổi-chung-về-cấu-trúc-response)
2.  [Endpoint `GET /mangas` (Lấy danh sách Manga)](#2-endpoint-get-mangas-lấy-danh-sách-manga)
3.  [Endpoint `GET /mangas/{id}` (Lấy chi tiết Manga)](#3-endpoint-get-mangasid-lấy-chi-tiết-manga)
4.  [Endpoint `GET /tags` và `GET /tags/{id}` (Lấy thông tin Tag)](#4-endpoint-get-tags-và-get-tagsid-lấy-thông-tin-tag)
5.  [Endpoint `GET /authors` và `GET /authors/{id}` (Lấy thông tin Author)](#5-endpoint-get-authors-và-get-authorsid-lấy-thông-tin-author)

---

## 1. Thay đổi chung về cấu trúc Response

Đối tượng `RelationshipObject` trong các response API có thể chứa thêm thuộc tính `attributes` nếu client yêu cầu "include" thông tin chi tiết của thực thể liên quan.

**Cấu trúc `RelationshipObject` mới:**

```json
{
  "id": "string (ID của thực thể liên quan)",
  "type": "string (loại của mối quan hệ hoặc vai trò)",
  "attributes": { 
    // (Tùy chọn, chỉ có nếu được include)
    // Thuộc tính chi tiết của thực thể liên quan 
  }
}
```

---

## 2. Endpoint `GET /mangas` (Lấy danh sách Manga)

### 2.1. Tham số Lọc mới

*   **`publicationDemographicsFilter[]`**: (Thay thế cho `demographicFilter` cũ)
    *   **Kiểu**: Array of `PublicationDemographic` (enum dạng chuỗi, ví dụ: "Shounen", "Shoujo").
    *   **Mô tả**: Lọc manga theo một hoặc nhiều đối tượng độc giả.
    *   **Cách dùng**: Lặp lại tham số trên query string.
    *   **Ví dụ**: `GET /mangas?publicationDemographicsFilter=Shounen&publicationDemographicsFilter=Seinen`

*   **Lọc Tag Nâng Cao**:
    *   **`includedTags[]`**:
        *   **Kiểu**: Array of GUID (dạng chuỗi).
        *   **Mô tả**: Lọc các manga PHẢI chứa các tag được chỉ định.
    *   **`includedTagsMode`**:
        *   **Kiểu**: String ("AND" hoặc "OR").
        *   **Mặc định**: "AND".
        *   **Mô tả**:
            *   `AND`: Manga phải chứa TẤT CẢ các tag trong `includedTags[]`.
            *   `OR`: Manga phải chứa ÍT NHẤT MỘT tag trong `includedTags[]`.
    *   **`excludedTags[]`**:
        *   **Kiểu**: Array of GUID (dạng chuỗi).
        *   **Mô tả**: Lọc các manga KHÔNG ĐƯỢC chứa các tag được chỉ định.
    *   **`excludedTagsMode`**:
        *   **Kiểu**: String ("AND" hoặc "OR").
        *   **Mặc định**: "OR".
        *   **Mô tả**:
            *   `OR`: Manga không được chứa BẤT KỲ tag nào trong `excludedTags[]`.
            *   `AND`: Manga không được chứa TẤT CẢ các tag trong `excludedTags[]` (nghĩa là, nó được phép chứa một số tag trong danh sách này, miễn là không phải tất cả).
    *   **Ví dụ**: `GET /mangas?includedTags[]=tagId1&includedTagsMode=AND&excludedTags[]=tagId2&excludedTagsMode=OR`

*   **Xóa tham số `tagIdsFilter`**: Tham số `tagIdsFilter` cũ đã bị **XÓA**. Vui lòng sử dụng `includedTags[]` với `includedTagsMode="OR"` để đạt được chức năng tương tự (lọc manga chứa BẤT KỲ tag nào trong danh sách).

### 2.2. Tham số `includes[]`

*   **Kiểu**: Array of String.
*   **Mô tả**: Yêu cầu API trả về thêm dữ liệu liên quan trong mục `relationships` của mỗi Manga.
*   **Các giá trị được hỗ trợ**:
    *   `"cover_art"`:
        *   Nếu được include, mục `relationships` của mỗi đối tượng Manga sẽ chứa một `RelationshipObject` với:
            *   `type: "cover_art"`
            *   `id: "<PublicId_cua_anh_bia_moi_nhat>"` (Public ID của ảnh bìa từ Cloudinary)
            *   `attributes: null`
    *   `"author"`: (Áp dụng cho cả Author và Artist)
        *   Nếu được include, mục `relationships` của mỗi đối tượng Manga sẽ chứa các `RelationshipObject` cho các tác giả và họa sĩ liên quan.
        *   Đối với tác giả: `type: "author"`, `id: "<AuthorGuid>"`.
        *   Đối với họa sĩ: `type: "artist"`, `id: "<AuthorGuid>"`.
        *   Thuộc tính `attributes` của các `RelationshipObject` này sẽ chứa:
            ```json
            {
              "name": "string (Tên tác giả/họa sĩ)",
              "biography": "string|null (Tiểu sử)"
            }
            ```
            (Lưu ý: `createdAt` và `updatedAt` của Author/Artist **không** được bao gồm trong `attributes` này).

### 2.3. Cấu trúc `attributes.tags` của Manga

*   Thuộc tính `tags` trong `attributes` của mỗi đối tượng Manga giờ đây là một mảng các `ResourceObject`.
*   Mỗi `ResourceObject` trong mảng `tags` có cấu trúc:
    ```json
    {
      "id": "string (GUID của Tag)",
      "type": "tag",
      "attributes": {
        "name": "string (Tên Tag)",
        "tagGroupName": "string (Tên Nhóm Tag)"
      },
      "relationships": null 
    }
    ```
    (Lưu ý: `createdAt`, `updatedAt` của Tag và `relationships` trỏ về `TagGroup` không còn nằm trong phần `attributes` của Manga nữa).

---

## 3. Endpoint `GET /mangas/{id}` (Lấy chi tiết Manga)

### 3.1. Tham số `includes[]`

*   **Kiểu**: Array of String.
*   **Mô tả**: Yêu cầu API trả về thêm dữ liệu liên quan trong mục `relationships` của Manga.
*   **Các giá trị được hỗ trợ**:
    *   `"author"`: (Áp dụng cho cả Author và Artist)
        *   Tương tự như trong `GET /mangas`, nếu được include, mục `relationships` của đối tượng Manga sẽ chứa các `RelationshipObject` cho tác giả và họa sĩ.
        *   Thuộc tính `attributes` của các `RelationshipObject` này sẽ chứa:
            ```json
            {
              "name": "string (Tên tác giả/họa sĩ)",
              "biography": "string|null (Tiểu sử)"
            }
            ```

### 3.2. Cấu trúc `attributes.tags` của Manga

*   Tương tự như trong `GET /mangas`, thuộc tính `tags` trong `attributes` của Manga là một mảng các `ResourceObject` với cấu trúc `TagInMangaAttributesDto` như đã mô tả ở mục 2.3.

### 3.3. `relationships` của Manga

*   **Primary Cover Art**: Mục `relationships` của Manga sẽ luôn chứa một `RelationshipObject` cho ảnh bìa chính (mới nhất), nếu có:
    ```json
    {
      "id": "string (GUID của CoverArt entity)",
      "type": "cover_art",
      "attributes": null 
    }
    ```

---

## 4. Endpoint `GET /tags` và `GET /tags/{id}` (Lấy thông tin Tag)

### 4.1. Cấu trúc `attributes` của Tag

*   Thuộc tính `attributes` của mỗi `ResourceObject` Tag sẽ chứa:
    ```json
    {
      "name": "string (Tên Tag)",
      "tagGroupName": "string (Tên Nhóm Tag của Tag này)",
      "createdAt": "string (ISO 8601 datetime)",
      "updatedAt": "string (ISO 8601 datetime)"
    }
    ```
    (Lưu ý: `tagGroupId` không còn trong `attributes` này).

### 4.2. Cấu trúc `relationships` của Tag

*   Mục `relationships` của mỗi `ResourceObject` Tag sẽ chứa một `RelationshipObject` trỏ về `TagGroup` chứa nó:
    ```json
    [
      {
        "id": "string (GUID của TagGroup)",
        "type": "tag_group",
        "attributes": null
      }
    ]
    ```

---

## 5. Endpoint `GET /authors` và `GET /authors/{id}` (Lấy thông tin Author)

Không có thay đổi lớn về cấu trúc response cho các endpoint này từ các cập nhật đã nêu. `attributes` của Author vẫn bao gồm `name`, `biography`, `createdAt`, và `updatedAt`.

**Ví dụ `attributes` của Author:**
```json
{
  "name": "Oda Eiichiro",
  "biography": "Japanese manga artist...",
  "createdAt": "2023-01-10T10:00:00Z",
  "updatedAt": "2024-03-15T12:30:00Z"
}
```