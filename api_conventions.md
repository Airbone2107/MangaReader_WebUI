# API Conventions

## 1. Base URL

Tất cả các API endpoints đều sử dụng tên của controller làm đường dẫn gốc. Ví dụ:

- `https://api.mangareader.com/mangas` (cho controller `MangasController`)
- `https://api.mangareader.com/authors` (cho controller `AuthorsController`)

Một số endpoint có thể có đường dẫn tùy chỉnh (absolute path) bắt đầu bằng `/` để tạo mối quan hệ rõ ràng hơn giữa các tài nguyên. Ví dụ:

- `https://api.mangareader.com/mangas/{mangaId}/covers`
- `https://api.mangareader.com/translatedmangas/{translatedMangaId}/chapters`

## 2. HTTP Methods

| Method | Mục đích |
|--------|----------|
| GET    | Lấy dữ liệu |
| POST   | Tạo mới dữ liệu |
| PUT    | Cập nhật toàn bộ dữ liệu |
| DELETE | Xóa dữ liệu |

## 3. Status Codes

| Status Code | Ý nghĩa |
|-------------|---------|
| 200 OK | Request thành công |
| 201 Created | Tạo mới thành công |
| 204 No Content | Request thành công, không có dữ liệu trả về |
| 400 Bad Request | Request không hợp lệ (lỗi validation) |
| 404 Not Found | Không tìm thấy tài nguyên |
| 500 Internal Server Error | Lỗi server |

## 4. Pagination

Các endpoints trả về danh sách đều hỗ trợ phân trang với các tham số:

- `offset`: Vị trí bắt đầu (mặc định: 0)
- `limit`: Số lượng tối đa kết quả trả về (mặc định: 20, tối đa: 100)

Ví dụ:

```
GET /mangas?offset=20&limit=10
```

## 5. Filtering, Sorting và Includes

### 5.1. Endpoint `GET /mangas` (Danh sách Manga)

-   **Filtering:**
    -   `titleFilter` (string): Lọc theo tiêu đề.
    -   `statusFilter` (enum `MangaStatus`): Lọc theo trạng thái.
    -   `contentRatingFilter` (enum `ContentRating`): Lọc theo đánh giá nội dung.
    -   `publicationDemographicsFilter[]` (list of enum `PublicationDemographic`): Lọc theo một hoặc nhiều đối tượng độc giả.
    -   `originalLanguageFilter` (string): Lọc theo ngôn ngữ gốc.
    -   `yearFilter` (int): Lọc theo năm xuất bản.
    -   `authorIdsFilter[]` (list of GUID): Lọc manga chứa BẤT KỲ author nào trong danh sách ID.
    -   **Lọc Tag Nâng Cao:**
        *   `includedTags[]` (array of GUIDs): Lọc các manga PHẢI chứa các tag được chỉ định.
        *   `includedTagsMode` (string: "AND" | "OR", mặc định "AND"): Chế độ cho `includedTags[]`.
        *   `excludedTags[]` (array of GUIDs): Lọc các manga KHÔNG ĐƯỢC chứa các tag được chỉ định.
        *   `excludedTagsMode` (string: "AND" | "OR", mặc định "OR"): Chế độ cho `excludedTags[]`.
-   **Sorting:** Sử dụng tham số `orderBy` (ví dụ: `updatedAt`, `title`, `year`, `createdAt`) và `ascending` (boolean, `true` hoặc `false`).
-   **Includes:** Sử dụng tham số `includes[]` để yêu cầu thêm dữ liệu liên quan. Các giá trị hỗ trợ:
    -   `cover_art`: Trả về `PublicId` của ảnh bìa mới nhất trong `relationships` của mỗi Manga.
    -   `author`: Trả về thông tin chi tiết (attributes chỉ gồm `name` và `biography`) của tác giả (role 'Author') và họa sĩ (role 'Artist') trong `relationships` của mỗi Manga.
    *Ví dụ:* `GET /mangas?includes[]=cover_art&includes[]=author`

### 5.2. Endpoint `GET /mangas/{id}` (Chi tiết Manga)

-   **Includes:** Sử dụng tham số `includes[]` để yêu cầu thêm dữ liệu liên quan. Các giá trị hỗ trợ:
    -   `author`: Trả về thông tin chi tiết (attributes chỉ gồm `name` và `biography`) của tác giả (role 'Author') và họa sĩ (role 'Artist') trong `relationships` của Manga.
    *Ví dụ:* `GET /mangas/{id}?includes[]=author`

## 6. Cấu Trúc Response Body (JSON)

Tất cả các response thành công (200 OK, 201 Created) trả về dữ liệu sẽ tuân theo cấu trúc sau:

### 6.1. Response Cho Một Đối Tượng Đơn Lẻ

```json
{
  "result": "ok",
  "response": "entity",
  "data": {
    "id": "string (GUID)",
    "type": "string (loại của resource, ví dụ: 'manga', 'author')",
    "attributes": {
      // Các thuộc tính cụ thể của resource.
      // Đối với Manga, trường "tags" sẽ chứa danh sách các TagInMangaAttributesDto chi tiết.
      // Ví dụ cho MangaAttributesDto:
      //   "title": "One Piece",
      //   "tags": [
      //     {
      //       "id": "tag-guid-1",
      //       "type": "tag",
      //       "attributes": { "name": "Action", "tagGroupName": "Genre" },
      //       "relationships": null
      //     }
      //   ],
      //   ...
    },
    "relationships": [ // (Tùy chọn)
      {
        "id": "string (ID của entity liên quan, hoặc PublicId cho cover_art)",
        "type": "string (loại của MỐI QUAN HỆ hoặc VAI TRÒ)",
        "attributes": { // (Tùy chọn, chỉ có nếu được include)
          // Thuộc tính chi tiết của entity liên quan (chỉ name và biography cho author/artist)
        }
      }
      // ... các relationships khác ...
    ]
  }
}
```

*   **`data.id`**: ID của tài nguyên chính.
*   **`data.type`**: Loại của tài nguyên chính.
*   **`data.attributes`**: Object chứa các thuộc tính của tài nguyên.
    *   **Đối với Manga (`type: "manga"`)**: `data.attributes` sẽ chứa một mảng `tags`. Mỗi phần tử trong mảng `tags` là một `ResourceObject<TagInMangaAttributesDto>` đơn giản, bao gồm `id`, `type: "tag"`, `attributes` (chỉ chứa `name` và `tagGroupName`), và `relationships` luôn là `null`.
*   **`data.relationships`**: Mảng các đối tượng `RelationshipObject`.
    *   **`id`**: ID của thực thể liên quan. **Đặc biệt:** Nếu `type` là `"cover_art"` (cho danh sách manga), `id` ở đây sẽ là `PublicId` của ảnh bìa.
    *   **`type`**: Mô tả vai trò/bản chất của mối quan hệ.
    *   **`attributes`**: (Tùy chọn) Nếu client yêu cầu `includes` (ví dụ: `includes[]=author`), trường này sẽ chứa object attributes của entity liên quan (ví dụ: `{ name: "Author Name", biography: "..." }`). Chỉ có `name` và `biography` được bao gồm cho `author` và `artist`, không có `createdAt` và `updatedAt`. Nếu không include, trường này sẽ không có hoặc là `null`.

### 6.2. Response Cho Danh Sách Đối Tượng (Collection)

```json
{
  "result": "ok",
  "response": "collection",
  "data": [
    {
      "id": "string (GUID)",
      "type": "string (loại của resource)",
      "attributes": { /* ... xem mục 6.1 ... */ },
      "relationships": [ /* ... xem mục 6.1 ... */ ]
    }
    // ... các resource objects khác ...
  ],
  "limit": 10,
  "offset": 0,
  "total": 100
}
```
*   Trường `data` là một mảng các `ResourceObject` như mô tả ở mục 6.1.
*   `limit`, `offset`, `total` là các thông tin phân trang.

### 6.3. Ví dụ Response Cho Manga (Chi tiết hoặc trong danh sách)

```json
{
  "result": "ok",
  "response": "entity", // Hoặc "collection" nếu là danh sách
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "type": "manga",
    "attributes": {
      "title": "Komi Can't Communicate",
      "originalLanguage": "ja",
      "publicationDemographic": "Shounen",
      "status": "Ongoing",
      "year": 2016,
      "contentRating": "Safe",
      "isLocked": false,
      "createdAt": "2023-01-01T00:00:00Z",
      "updatedAt": "2023-06-01T00:00:00Z",
      "tags": [
        {
          "id": "tag-guid-comedy",
          "type": "tag",
          "attributes": {
            "name": "Comedy",
            "tagGroupName": "Genre"
          },
          "relationships": null
        },
        {
          "id": "tag-guid-school-life",
          "type": "tag",
          "attributes": {
            "name": "School Life",
            "tagGroupName": "Theme"
          },
          "relationships": null
        }
      ]
    },
    "relationships": [
      {
        "id": "author-artist-guid-1", // ID của Author/Artist
        "type": "author", // hoặc "artist"
        // "attributes" sẽ có ở đây nếu client yêu cầu includes[]=author (hoặc artist)
        // Ví dụ, nếu includes[]=author:
        // "attributes": {
        //   "name": "Tomohito Oda",
        //   "biography": null
        //   // KHÔNG bao gồm CreatedAt, UpdatedAt
        // }
      },
      {
        "id": "cover-art-public-id-xyz", // Public ID của cover nếu GET /mangas và includes[]=cover_art
                                        // Hoặc GUID của CoverArt entity nếu là GET /mangas/{id}
        "type": "cover_art"
        // "attributes" của cover_art không được include mặc định trong relationship này
      }
    ]
  }
}
```

## 7. Cấu Trúc Error Response

```json
{
  "result": "error",
  "errors": [
    {
      "status": 404, // HTTP status code
      "title": "Not Found", // Tóm tắt lỗi
      "detail": "Manga with ID '123...' was not found." // Chi tiết lỗi (có thể null)
      // "id": "unique-error-code", // (Tùy chọn) Mã lỗi duy nhất
      // "context": { ... } // (Tùy chọn) Thông tin bổ sung
    }
  ]
}
```
*Lưu ý: trường `code` trong ví dụ cũ đã được đổi thành `status` để nhất quán với JSON:API spec và HTTP status codes.*

## 8. Validation Errors

```json
{
  "result": "error",
  "errors": [
    {
      "status": 400,
      "title": "Title", // Tên trường gây lỗi (hoặc "Validation Error" chung)
      "detail": "The Title field is required." 
      // "context": { "field": "Title" } // Có thể dùng context để chỉ rõ trường
    },
    {
      "status": 400,
      "title": "OriginalLanguage",
      "detail": "The OriginalLanguage field is required."
    }
  ]
}
```

## 9. Các Loại Relationship Type

| Giá trị `type` | Mô tả                                       | Nơi xuất hiện                                  | ID trong Relationship |
|----------------|---------------------------------------------|------------------------------------------------|-----------------------|
| `author`       | Tác giả của manga                            | `RelationshipObject` (Manga -> Author)         | GUID của Author        |
| `artist`       | Họa sĩ của manga                             | `RelationshipObject` (Manga -> Author)         | GUID của Author        |
| `tag_group`    | Nhóm chứa tag                               | `RelationshipObject` (Tag -> TagGroup)         | GUID của TagGroup      |
| `cover_art`    | Ảnh bìa của manga                            | `RelationshipObject` (Manga -> CoverArt)       | `PublicId` (nếu từ list + include) hoặc GUID của CoverArt (nếu từ detail manga) |
| `manga`        | Manga gốc                                   | `RelationshipObject` (Chapter/TranslatedManga/CoverArt -> Manga) | GUID của Manga |
| `user`         | Người dùng tải lên                           | `RelationshipObject` (Chapter -> User)         | ID của User (int)     |
| `chapter`      | Chương của manga                             | `RelationshipObject` (ChapterPage -> Chapter)  | GUID của Chapter       |
| `translated_manga` | Bản dịch của manga                       | `RelationshipObject` (Chapter -> TranslatedManga) | GUID của TranslatedManga |
*Lưu ý: `tag` không còn là relationship type của Manga, mà được nhúng vào `attributes`.*

## 10. Các Endpoints Chính (Cập nhật cho Manga)

### Mangas

- `GET /mangas`: Lấy danh sách manga.
    - **Hỗ trợ các tham số lọc `titleFilter`, `statusFilter`, `contentRatingFilter`, `demographicFilter[]`, `originalLanguageFilter`, `yearFilter`, `authorIdsFilter[]`, `includedTags[]`, `includedTagsMode`, `excludedTags[]`, `excludedTagsMode`.**
    - **Hỗ trợ `includes[]` với các giá trị: `cover_art`, `author`, `artist`.**
- `GET /mangas/{id}`: Lấy thông tin chi tiết manga.
    - **Hỗ trợ `includes[]` với các giá trị: `author`, `artist`.**
    - **Thông tin chi tiết Tags luôn được trả về trong `attributes.tags`.**
- `POST /mangas`: Tạo manga mới (bao gồm cả tags và authors)
- `PUT /mangas/{id}`: Cập nhật manga (bao gồm cả tags và authors)
- `DELETE /mangas/{id}`: Xóa manga

### Authors

- `GET /authors`: Lấy danh sách tác giả
- `GET /authors/{id}`: Lấy thông tin chi tiết tác giả
- `POST /authors`: Tạo tác giả mới
- `PUT /authors/{id}`: Cập nhật tác giả
- `DELETE /authors/{id}`: Xóa tác giả

### Tags

- `GET /tags`: Lấy danh sách tag
- `GET /tags/{id}`: Lấy thông tin chi tiết tag
- `POST /tags`: Tạo tag mới
- `PUT /tags/{id}`: Cập nhật tag
- `DELETE /tags/{id}`: Xóa tag

### TagGroups

- `GET /taggroups`: Lấy danh sách nhóm tag
- `GET /taggroups/{id}`: Lấy thông tin chi tiết nhóm tag
- `POST /taggroups`: Tạo nhóm tag mới
- `PUT /taggroups/{id}`: Cập nhật nhóm tag
- `DELETE /taggroups/{id}`: Xóa nhóm tag

### Chapters

- `GET /chapters/{id}`: Lấy thông tin chi tiết chapter
- `GET /translatedmangas/{translatedMangaId}/chapters`: Lấy danh sách chapter của một bản dịch
- `POST /chapters`: Tạo chapter mới
- `PUT /chapters/{id}`: Cập nhật chapter
- `DELETE /chapters/{id}`: Xóa chapter
- `GET /chapters/{chapterId}/pages`: Lấy danh sách trang của chapter
- `POST /chapters/{chapterId}/pages/entry`: Tạo entry cho trang mới

### ChapterPages

- `POST /chapterpages/{pageId}/image`: Upload ảnh cho trang
- `PUT /chapterpages/{pageId}/details`: Cập nhật thông tin trang
- `DELETE /chapterpages/{pageId}`: Xóa trang

### CoverArts

- `GET /coverarts/{id}`: Lấy thông tin chi tiết ảnh bìa
- `GET /mangas/{mangaId}/covers`: Lấy danh sách ảnh bìa của manga
- `POST /mangas/{mangaId}/covers`: Upload ảnh bìa mới
- `DELETE /coverarts/{id}`: Xóa ảnh bìa

### TranslatedMangas

- `GET /translatedmangas/{id}`: Lấy thông tin chi tiết bản dịch
- `GET /mangas/{mangaId}/translations`: Lấy danh sách bản dịch của manga
- `POST /translatedmangas`: Tạo bản dịch mới
- `PUT /translatedmangas/{id}`: Cập nhật bản dịch
- `DELETE /translatedmangas/{id}`: Xóa bản dịch