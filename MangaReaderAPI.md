# EXTERNAL_API_GUIDE.md
# Hướng Dẫn Sử Dụng API MangaReader

Tài liệu này cung cấp hướng dẫn toàn diện cho các nhà phát triển muốn tích hợp hoặc tương tác với API MangaReader. Nó bao gồm các quy ước API chung, cấu trúc dữ liệu, và chi tiết về từng endpoint, giúp bạn dễ dàng hiểu và sử dụng các dịch vụ của chúng tôi mà không cần kiểm tra mã nguồn dự án.

## Mục Lục

1.  [Giới Thiệu](#1-giới-thiệu)
2.  [Cơ Sở URL](#2-cơ-sở-url)
3.  [Xác Thực & Ủy Quyền](#3-xác-thực--ủy-quyền)
4.  [Các Quy Ước Chung](#4-các-quy-ước-chung)
    *   [4.1. HTTP Methods](#41-http-methods)
    *   [4.2. HTTP Status Codes](#42-http-status-codes)
    *   [4.3. Pagination (Phân Trang)](#43-pagination-phân-trang)
    *   [4.4. Filtering (Lọc) & Sorting (Sắp Xếp)](#44-filtering-lọc--sorting-sắp-xếp)
    *   [4.5. Định Dạng Enum](#45-định-dạng-enum)
5.  [Cấu Trúc JSON Response](#5-cấu-trúc-json-response)
    *   [5.1. Response Thành Công - Đối Tượng Đơn Lẻ (`ApiResponse<TData>`)](#51-response-thành-công---đối-tượng-đơn-lẻ-apirisponsetdata)
    *   [5.2. Response Thành Công - Danh Sách Đối Tượng (`ApiCollectionResponse<TData>`)](#52-response-thành-công---danh-sách-đối-tượng-apicollectionresponsetdata)
    *   [5.3. Response Lỗi (`ApiErrorResponse`)](#53-response-lỗi-apierrorresponse)
    *   [5.4. Các Loại `Relationship Type` Phổ Biến](#54-các-loại-relationship-type-phổ-biến)
6.  [Chi Tiết Các API Endpoints](#6-chi-tiết-các-api-endpoints)
    *   [6.1. Authors (Tác Giả)](#61-authors-tác-giả)
    *   [6.2. Mangas](#62-mangas)
    *   [6.3. Tags](#63-tags)
    *   [6.4. TagGroups (Nhóm Tag)](#64-taggroups-nhóm-tag)
    *   [6.5. TranslatedMangas (Bản Dịch Manga)](#65-translatedmangas-bản-dịch-manga)
    *   [6.6. Chapters (Chương)](#66-chapters-chương)
    *   [6.7. ChapterPages (Trang Chương)](#67-chapterpages-trang-chương)
    *   [6.8. CoverArts (Ảnh Bìa)](#68-coverarts-ảnh-bìa)
7.  [Cập Nhật Quan Trọng: Thay Đổi Định Dạng Enum](#7-cập-nhật-quan-trọng-thay-đổi-định-dạng-enum)
    *   [7.1. Tóm Tắt Thay Đổi](#71-tóm-tắt-thay-đổi)
    *   [7.2. So Sánh Định Dạng Cũ và Mới](#72-so-sánh-định-dạng-cũ-và-mới)
    *   [7.3. Các Trường Enum Bị Ảnh Hưởng](#73-các-trường-enum-bị-ảnh-hưởng)
    *   [7.4. Tác Động Đến Frontend](#74-tác-động-đến-frontend)
    *   [7.5. Lý Do Thay Đổi](#75-lý-do-thay-đổi)
    *   [7.6. Hành Động Đề Xuất Cho Frontend](#76-hành-động-đề-xuất-cho-frontend)

---

## 1. Giới Thiệu

API MangaReader cho phép bạn quản lý và truy xuất thông tin về manga, tác giả, tag, chapter, bản dịch và ảnh bìa. API này được xây dựng theo kiến trúc Clean Architecture và mô hình CQRS.

## 2. Cơ Sở URL

Tất cả các API endpoints đều sử dụng đường dẫn gốc dựa trên tên Controller hoặc các đường dẫn tùy chỉnh tuyệt đối.
**Base URL:** `https://localhost:7262` (hoặc `http://localhost:5059` cho môi trường phát triển cục bộ). Trong môi trường Production, URL này sẽ khác.

Ví dụ:
*   `https://localhost:7262/Authors` (cho `AuthorsController`)
*   `https://localhost:7262/mangas/{mangaId}/covers`

## 3. Xác Thực & Ủy Quyền

Hiện tại, API MangaReader **KHÔNG** yêu cầu xác thực hoặc ủy quyền (Authorization) cho bất kỳ endpoint nào. Tất cả các endpoint đều có thể truy cập công khai.

**Lưu ý:** Trong một ứng dụng thực tế, việc thêm xác thực (ví dụ: JWT Bearer Token) và ủy quyền (phân quyền vai trò) là cần thiết để bảo mật các thao tác ghi (POST, PUT, DELETE) và kiểm soát truy cập dữ liệu.

## 4. Các Quy Ước Chung

### 4.1. HTTP Methods

| Method | Mục đích | Mô tả |
| :----- | :------- | :---- |
| `GET`  | Lấy dữ liệu | Truy xuất một hoặc nhiều tài nguyên. |
| `POST` | Tạo mới dữ liệu | Gửi dữ liệu để tạo một tài nguyên mới. |
| `PUT`  | Cập nhật toàn bộ dữ liệu | Cập nhật toàn bộ tài nguyên đã có. Cần gửi đầy đủ các trường của tài nguyên. |
| `DELETE` | Xóa dữ liệu | Xóa một tài nguyên cụ thể. |

### 4.2. HTTP Status Codes

| Status Code | Ý nghĩa | Mô tả |
| :---------- | :------ | :---- |
| `200 OK`    | Thành công | Request đã thành công. Phản hồi chứa dữ liệu được yêu cầu. |
| `201 Created` | Tạo mới thành công | Request đã thành công và một tài nguyên mới đã được tạo. Phản hồi chứa ID của tài nguyên mới và một `Location` header trỏ đến tài nguyên. |
| `204 No Content` | Thành công, không có nội dung | Request đã thành công nhưng không có nội dung nào để trả về trong body. Thường dùng cho các thao tác DELETE hoặc PUT không cần trả về dữ liệu. |
| `400 Bad Request` | Request không hợp lệ | Server không thể xử lý request do request có lỗi cú pháp hoặc vi phạm các quy tắc nghiệp vụ/validation. Chi tiết lỗi được cung cấp trong trường `errors` của response. |
| `404 Not Found` | Không tìm thấy tài nguyên | Tài nguyên được yêu cầu (dựa trên ID trong URL) không tồn tại. |
| `500 Internal Server Error` | Lỗi Server Nội Bộ | Lỗi xảy ra trên server không được mong đợi hoặc không được xử lý bởi các exception cụ thể. |

### 4.3. Pagination (Phân Trang)

Các endpoints trả về danh sách tài nguyên (collection) đều hỗ trợ phân trang thông qua các tham số query:

*   `offset`: (Số nguyên, mặc định: `0`) Số lượng bản ghi cần bỏ qua từ đầu kết quả.
*   `limit`: (Số nguyên, mặc định: `20`, tối đa: `100`) Số lượng bản ghi tối đa muốn lấy về trong một phản hồi.

**Ví dụ:**
```
GET /mangas?offset=20&limit=10
```
Phản hồi sẽ là một đối tượng `ApiCollectionResponse` chứa các trường `limit`, `offset`, và `total` để cung cấp thông tin về trạng thái phân trang.

### 4.4. Filtering (Lọc) & Sorting (Sắp Xếp)

Các endpoints trả về danh sách đều hỗ trợ các tham số query để lọc và sắp xếp dữ liệu. Các tham số này khác nhau tùy theo từng tài nguyên.

*   **Filtering**: Các tham số bắt đầu bằng tên trường và hậu tố `Filter` (ví dụ: `titleFilter`, `statusFilter`).
*   **Sorting**:
    *   `orderBy`: (Chuỗi) Tên trường muốn sắp xếp (ví dụ: `title`, `createdAt`).
    *   `ascending`: (Boolean, mặc định: `true`) `true` cho thứ tự tăng dần, `false` cho thứ tự giảm dần.

**Ví dụ:**
```
GET /mangas?statusFilter=Ongoing&orderBy=title&ascending=true
```

### 4.5. Định Dạng Enum

Tất cả các trường dữ liệu kiểu Enum trong API (cả trong request và response) đều được biểu diễn dưới dạng **chuỗi tên Enum** (string name) thay vì giá trị số nguyên (integer value). Điều này áp dụng cho cả JSON request body khi gửi dữ liệu lên và JSON response body khi nhận dữ liệu về.

#### Các trường Enum phổ biến trong API

1. **Manga**
   * `publicationDemographic`: Kiểu `PublicationDemographic` (ví dụ: "Shounen", "Shoujo", "Seinen", "Josei", "None")
   * `status`: Kiểu `MangaStatus` (ví dụ: "Ongoing", "Completed", "Hiatus", "Cancelled")
   * `contentRating`: Kiểu `ContentRating` (ví dụ: "Safe", "Suggestive", "Erotica", "Pornographic")

2. **MangaAuthorInputDto** (khi tạo/cập nhật Manga)
   * `role`: Kiểu `MangaStaffRole` (ví dụ: "Author", "Artist")

#### Ví dụ Request và Response

```json
// Request và Response mới (định dạng hiện tại)
{
  "status": "Ongoing",
  "contentRating": "Suggestive",
  "authors": [
    {
      "authorId": "...",
      "role": "Author"
    }
  ]
}
```

#### Lưu ý quan trọng
* Khi gửi dữ liệu lên API, hãy đảm bảo sử dụng đúng tên chuỗi của Enum.
* Nếu gửi một chuỗi không hợp lệ (không phải là tên của bất kỳ giá trị nào trong Enum tương ứng), API sẽ trả về lỗi `400 Bad Request` với thông báo lỗi chi tiết.
* Khi nhận dữ liệu từ API, các trường Enum sẽ luôn là chuỗi, không phải số nguyên.

## 5. Cấu Trúc JSON Response

Tất cả các phản hồi JSON đều tuân theo một cấu trúc thống nhất.

### 5.1. Response Thành Công - Đối Tượng Đơn Lẻ (`ApiResponse<TData>`)

```json
{
  "result": "ok",       // Luôn là "ok" cho response thành công
  "response": "entity", // Luôn là "entity" cho một đối tượng đơn lẻ
  "data": {
    "id": "string (GUID)",
    "type": "string (loại của resource, ví dụ: 'manga', 'author')",
    "attributes": {
      // Các thuộc tính cụ thể của resource (tương ứng với *AttributesDto)
    },
    "relationships": [
      {
        "id": "string (GUID của entity liên quan)",
        "type": "string (loại của MỐI QUAN HỆ hoặc VAI TRÒ, ví dụ: 'author', 'artist', 'cover_art')",
        "attributes": { // (Tùy chọn)
            // Thuộc tính chi tiết của thực thể liên quan, chỉ xuất hiện nếu được yêu cầu qua tham số `includes[]`.
        }
      }
      // ... các relationships khác ...
    ]
  }
}
```

*   **`data.id`**: Định danh duy nhất (GUID) của tài nguyên chính dưới dạng chuỗi.
*   **`data.type`**: Loại của tài nguyên chính. Được viết bằng `snake_case`, số ít (ví dụ: `"manga"`, `"author"`, `"tag"`, `"chapter_page"`).
*   **`data.attributes`**: Một đối tượng JSON chứa tất cả các thuộc tính của tài nguyên, trừ `id` và các mối quan hệ (tương ứng với các `*AttributesDto`).
*   **`data.relationships`**: (Tùy chọn, có thể không có nếu không có mối quan hệ nào được trả về hoặc không liên quan) Một mảng các đối tượng `RelationshipObject`.
    *   **`id`**: Định danh duy nhất (GUID) của thực thể liên quan.
    *   **`type`**: Mô tả vai trò hoặc bản chất của mối quan hệ đó đối với thực thể gốc.
    *   **`attributes`**: (Tùy chọn) Một đối tượng JSON chứa các thuộc tính chi tiết của thực thể liên quan. Trường này chỉ xuất hiện khi client yêu cầu thông qua tham số query `includes[]` (ví dụ: `?includes[]=author`).

### 5.2. Response Thành Công - Danh Sách Đối Tượng (`ApiCollectionResponse<TData>`)

```json
{
  "result": "ok",
  "response": "collection", // Luôn là "collection" cho danh sách đối tượng
  "data": [
    {
      "id": "string (GUID)",
      "type": "string (loại của resource)",
      "attributes": { /* ... */ },
      "relationships": [ /* ... */ ]
    },
    { /* ... */ }
  ],
  "limit": 10,  // Số lượng item được yêu cầu (tham số query 'limit')
  "offset": 0,  // Vị trí bắt đầu được yêu cầu (tham số query 'offset')
  "total": 100  // Tổng số item có sẵn trong collection mà không cần phân trang
}
```

*   Trường `data` là một mảng các `ResourceObject` như mô tả ở mục 5.1.
*   `limit`, `offset`, `total` cung cấp thông tin phân trang.

### 5.3. Response Lỗi (`ApiErrorResponse`)

Khi có lỗi xảy ra (ví dụ: lỗi validation, không tìm thấy tài nguyên), API sẽ trả về phản hồi với cấu trúc sau:

```json
{
  "result": "error", // Luôn là "error" cho response lỗi
  "errors": [
    {
      "status": 400,          // HTTP status code áp dụng cho lỗi này
      "title": "Validation Failed", // Tóm tắt ngắn gọn về vấn đề
      "detail": "Tiêu đề không được để trống.", // Giải thích chi tiết về vấn đề
      "id": "validation_error_code", // (Tùy chọn) Một định danh lỗi cụ thể
      "context": {              // (Tùy chọn) Thông tin bổ sung, ví dụ: tên trường lỗi
        "field": "Title"
      }
    }
    // ... có thể có nhiều đối tượng lỗi nếu có nhiều lỗi validation ...
  ]
}
```

*   **`errors`**: Một mảng các đối tượng `ApiError`.
    *   **`status`**: HTTP status code (ví dụ: `400` cho Bad Request, `404` cho Not Found, `500` cho Internal Server Error).
    *   **`title`**: Tiêu đề ngắn gọn mô tả loại lỗi.
    *   **`detail`**: Mô tả chi tiết hơn về lỗi.
    *   **`id`**: (Tùy chọn) Một mã định danh duy nhất cho loại lỗi cụ thể (ví dụ: `missing_field`, `invalid_format`).
    *   **`context`**: (Tùy chọn) Thông tin bổ sung liên quan đến lỗi, ví dụ: `{"field": "Name"}` cho lỗi validation trường `Name`. Trong môi trường phát triển, `context` có thể chứa stack trace cho lỗi 500.

### 5.4. Các Loại `Relationship Type` Phổ Biến

Bảng dưới đây liệt kê các giá trị `type` được sử dụng trong `ResourceObject` (cho chính tài nguyên) và `RelationshipObject` (cho mối quan hệ):

| Giá trị `type`  | Mô tả                                       | Nơi xuất hiện                                                     |
| :-------------- | :------------------------------------------ | :---------------------------------------------------------------- |
| `author`        | Tác giả của manga (người viết truyện)        | `ResourceObject` (cho Author); `RelationshipObject` (Manga -> Author) |
| `artist`        | Họa sĩ của manga (người vẽ truyện)           | `RelationshipObject` (Manga -> Author/Artist)                     |
| `tag`           | Thẻ gắn với manga                           | `ResourceObject` (cho Tag); `RelationshipObject` (Manga -> Tag)   |
| `tag_group`     | Nhóm chứa tag (ví dụ: "Genres", "Themes")   | `ResourceObject` (cho TagGroup); `RelationshipObject` (Tag -> TagGroup) |
| `cover_art`     | Ảnh bìa của manga                           | `ResourceObject` (cho CoverArt); `RelationshipObject` (Manga -> CoverArt) |
| `manga`         | Manga gốc                                   | `ResourceObject` (cho Manga); `RelationshipObject` (Chapter/TranslatedManga/CoverArt -> Manga) |
| `user`          | Người dùng tải lên chương                    | `ResourceObject` (cho User - nếu có API riêng); `RelationshipObject` (Chapter -> User) |
| `chapter`       | Chương của manga                            | `ResourceObject` (cho Chapter); `RelationshipObject` (ChapterPage -> Chapter) |
| `chapter_page`  | Trang của chương                             | `ResourceObject` (cho ChapterPage)                               |
| `translated_manga` | Bản dịch của manga (theo ngôn ngữ cụ thể) | `ResourceObject` (cho TranslatedManga); `RelationshipObject` (Chapter -> TranslatedManga) |

---

## 6. Chi Tiết Các API Endpoints

### 6.1. Authors (Tác Giả)

Tài nguyên Author đại diện cho các cá nhân là tác giả hoặc họa sĩ của một bộ truyện.

#### 6.1.1. `POST /Authors`

*   **Mô tả:** Tạo một tác giả mới.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Eiichiro Oda",      // Tên tác giả (Bắt buộc, tối đa 255 ký tự)
      "biography": "Một họa sĩ manga nổi tiếng người Nhật Bản, được biết đến nhiều nhất với tác phẩm One Piece." // Tiểu sử (Tùy chọn, tối đa 2000 ký tự)
    }
    ```
    *   **`name`**: Tên của tác giả.
    *   **`biography`**: Tiểu sử của tác giả.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "author",
            "attributes": {
              "name": "Eiichiro Oda",
              "biography": "Một họa sĩ manga nổi tiếng người Nhật Bản, được biết đến nhiều nhất với tác phẩm One Piece.",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": null // Không có mối quan hệ nào được trả về mặc định khi tạo
          }
        }
        ```
        `Location` Header: `/Authors/123e4567-e89b-12d3-a456-426614174000`
    *   `400 Bad Request` (Validation Failed nếu `name` trống/quá dài hoặc `biography` quá dài)
        ```json
        {
          "result": "error",
          "errors": [
            {
              "status": 400,
              "title": "Name",
              "detail": "Tên tác giả không được để trống.",
              "context": {
                "field": "Name"
              }
            }
          ]
        }
        ```

#### 6.1.2. `GET /Authors/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một tác giả bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tác giả.
*   **Responses:**
    *   `200 OK`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "author",
            "attributes": {
              "name": "Eiichiro Oda",
              "biography": "Một họa sĩ manga nổi tiếng người Nhật Bản, được biết đến nhiều nhất với tác phẩm One Piece.",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "423e4567-e89b-12d3-a456-426614174001", "type": "manga" }, // ID của manga mà tác giả này tham gia
              { "id": "523e4567-e89b-12d3-a456-426614174002", "type": "manga" }
            ]
          }
        }
        ```
    *   `404 Not Found` (Nếu không tìm thấy tác giả)

#### 6.1.3. `GET /Authors`

*   **Mô tả:** Lấy danh sách các tác giả có phân trang, lọc và sắp xếp.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`) Vị trí bắt đầu.
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`) Số lượng kết quả tối đa.
    *   `nameFilter`: (Chuỗi, Tùy chọn) Lọc theo tên tác giả (tìm kiếm chứa chuỗi con).
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `Name`) Tên trường để sắp xếp (`Name`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `true`) `true` cho tăng dần, `false` cho giảm dần.
*   **Responses:**
    *   `200 OK`
        ```json
        {
          "result": "ok",
          "response": "collection",
          "data": [
            {
              "id": "123e4567-e89b-12d3-a456-426614174000",
              "type": "author",
              "attributes": {
                "name": "Eiichiro Oda",
                "biography": "...",
                "createdAt": "2023-10-27T10:00:00Z",
                "updatedAt": "2023-10-27T10:00:00Z"
              },
              "relationships": [
                { "id": "423e4567-e89b-12d3-a456-426614174001", "type": "manga" }
              ]
            },
            {
              "id": "abcdef01-2345-6789-abcd-ef0123456789",
              "type": "author",
              "attributes": {
                "name": "Akira Toriyama",
                "biography": "...",
                "createdAt": "2023-01-15T09:00:00Z",
                "updatedAt": "2023-01-15T09:00:00Z"
              },
              "relationships": null
            }
          ],
          "limit": 20,
          "offset": 0,
          "total": 2
        }
        ```

#### 6.1.4. `PUT /Authors/{id}`

*   **Mô tả:** Cập nhật thông tin của một tác giả.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tác giả cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Eiichiro Oda (Updated)", // Tên tác giả (Bắt buộc, tối đa 255 ký tự)
      "biography": "Tác giả của One Piece." // Tiểu sử (Tùy chọn, tối đa 2000 ký tự)
    }
    ```
    *   **Lưu ý:** Cần gửi đầy đủ các trường `name` và `biography` ngay cả khi bạn không muốn thay đổi chúng, nếu không các trường đó sẽ được gán giá trị mặc định (string.Empty hoặc null).
*   **Responses:**
    *   `204 No Content` (Cập nhật thành công, không có nội dung trả về)
    *   `400 Bad Request` (Validation Failed nếu dữ liệu không hợp lệ)
    *   `404 Not Found` (Nếu không tìm thấy tác giả)

#### 6.1.5. `DELETE /Authors/{id}`

*   **Mô tả:** Xóa một tác giả bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tác giả cần xóa.
*   **Responses:**
    *   `204 No Content` (Xóa thành công)
    *   `404 Not Found` (Nếu không tìm thấy tác giả)

### 6.2. Mangas

Tài nguyên Manga đại diện cho các tác phẩm truyện tranh (manga, manhwa, manhua) gốc.

#### 6.2.1. `POST /Mangas`

*   **Mô tả:** Tạo một manga mới. Bạn có thể tùy chọn gán các tags và tác giả ngay khi tạo.
*   **Request Body:** `application/json`
    ```json
    {
      "title": "One Piece",
      "originalLanguage": "ja",
      "publicationDemographic": "Shounen", // Enum: Shounen, Shoujo, Josei, Seinen, None (string)
      "status": "Ongoing",     // Enum: Ongoing, Completed, Hiatus, Cancelled (string)
      "year": 1997,
      "contentRating": "Safe", // Enum: Safe, Suggestive, Erotica, Pornographic (string)
      "tagIds": [               // (Tùy chọn) Danh sách GUID của các Tags đã tồn tại
        "323e4567-e89b-12d3-a456-426614174000",
        "323e4567-e89b-12d3-a456-426614174001"
      ],
      "authors": [              // (Tùy chọn) Danh sách tác giả/họa sĩ với vai trò cụ thể
        {
          "authorId": "223e4567-e89b-12d3-a456-426614174000",
          "role": "Author"     // Enum: Author, Artist (string)
        },
        {
          "authorId": "223e4567-e89b-12d3-a456-426614174000", // Cùng tác giả nhưng vai trò khác
          "role": "Artist"
        }
      ]
    }
    ```
    *   **Lưu ý:** Các `TagId` và `AuthorId` trong request body phải là các GUID của các Tag và Author đã tồn tại trong hệ thống. Nếu không tồn tại, chúng sẽ bị bỏ qua khi tạo manga.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "manga",
            "attributes": {
              "title": "One Piece",
              "originalLanguage": "ja",
              "publicationDemographic": "Shounen",
              "status": "Ongoing",
              "year": 1997,
              "contentRating": "Safe",
              "isLocked": false,
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "223e4567-e89b-12d3-a456-426614174000", "type": "author" },
              { "id": "223e4567-e89b-12d3-a456-426614174000", "type": "artist" },
              { "id": "323e4567-e89b-12d3-a456-426614174000", "type": "tag" },
              { "id": "323e4567-e89b-12d3-a456-426614174001", "type": "tag" }
            ]
          }
        }
        ```
        `Location` Header: `/Mangas/123e4567-e89b-12d3-a456-426614174000`
    *   `400 Bad Request` (Validation Failed)

#### 6.2.2. `GET /Mangas/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một manga bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của manga.
*   **Request Parameters (Query):**
    *   `includes[]`: (Chuỗi, Tùy chọn) Một mảng các thông tin bổ sung muốn bao gồm trong kết quả. Các giá trị được hỗ trợ:
        *   `author`: Trả về thông tin đầy đủ (`attributes`) của các tác giả (`author`) và họa sĩ (`artist`) trong mảng `relationships`.
        *   `cover_art`: Trả về thông tin đầy đủ (`attributes`) của tất cả ảnh bìa (`cover_art`) liên quan trong mảng `relationships`.
*   **Responses:**
    *   `200 OK`
        ```json
        // Ví dụ response cho GET /Mangas/{id}?includes[]=author&includes[]=cover_art
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "manga",
            "attributes": { /* các thuộc tính của manga */ },
            "relationships": [
              { 
                "id": "author-guid-1", 
                "type": "author",
                "attributes": {
                  "name": "Eiichiro Oda",
                  "biography": "...",
                  "createdAt": "...",
                  "updatedAt": "..."
                }
              },
              {
                "id": "cover-art-guid-1",
                "type": "cover_art",
                "attributes": {
                  "volume": "1",
                  "publicId": "mangas_v2/manga-guid/covers/...",
                  "description": "Cover for volume 1",
                  "createdAt": "...",
                  "updatedAt": "..."
                }
              },
              {
                "id": "cover-art-guid-2",
                "type": "cover_art",
                "attributes": {
                  "volume": "2",
                  "publicId": "mangas_v2/manga-guid/covers/...",
                  "description": "Cover for volume 2",
                  "createdAt": "...",
                  "updatedAt": "..."
                }
              }
            ]
          }
        }
        ```
    *   `404 Not Found`

#### 6.2.3. `GET /Mangas`

*   **Mô tả:** Lấy danh sách các manga có phân trang, lọc và sắp xếp.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`)
    *   `titleFilter`: (Chuỗi, Tùy chọn) Lọc theo tiêu đề (tìm kiếm chứa chuỗi con).
    *   `statusFilter`: (Chuỗi, Tùy chọn) Lọc theo trạng thái (`Ongoing`, `Completed`, `Hiatus`, `Cancelled`).
    *   `contentRatingFilter`: (Chuỗi, Tùy chọn) Lọc theo đánh giá nội dung (`Safe`, `Suggestive`, `Erotica`, `Pornographic`).
    *   `demographicFilter`: (Chuỗi, Tùy chọn) Lọc theo đối tượng độc giả (`Shounen`, `Shoujo`, `Josei`, `Seinen`, `None`).
    *   `originalLanguageFilter`: (Chuỗi, Tùy chọn) Lọc theo ngôn ngữ gốc (mã ISO 639-1).
    *   `yearFilter`: (Số nguyên, Tùy chọn) Lọc theo năm xuất bản.
    *   `tagIdsFilter`: (Danh sách GUID, Tùy chọn) Lọc manga chứa **BẤT KỲ** tag nào trong danh sách cung cấp. Ví dụ: `?tagIdsFilter=guid1&tagIdsFilter=guid2`.
    *   `authorIdsFilter`: (Danh sách GUID, Tùy chọn) Lọc manga chứa **BẤT KỲ** tác giả/họa sĩ nào trong danh sách cung cấp.
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `UpdatedAt`) Tên trường để sắp xếp (`Title`, `Year`, `CreatedAt`, `UpdatedAt`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `false`) `true` cho tăng dần, `false` cho giảm dần (mặc định giảm dần cho `UpdatedAt`).
    *   `includes[]`: (Chuỗi, Tùy chọn) Một mảng các thông tin bổ sung muốn bao gồm trong kết quả. Các giá trị được hỗ trợ:
        *   `author`: Trả về thông tin đầy đủ (`attributes`) của các tác giả (`author`) và họa sĩ (`artist`) trong mảng `relationships`.
        *   `cover_art`: Trả về thông tin đầy đủ (`attributes`) của ảnh bìa **chính** (mới nhất) trong mảng `relationships`. ID của mối quan hệ này là `CoverId` (GUID).
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<MangaAttributesDto>`)

#### 6.2.4. `PUT /Mangas/{id}`

*   **Mô tả:** Cập nhật thông tin của một manga.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của manga cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "title": "One Piece (Updated Title)",
      "originalLanguage": "ja",
      "publicationDemographic": "Shounen",
      "status": "Ongoing",
      "year": 1997,
      "contentRating": "Safe",
      "isLocked": true,        // Có thể khóa manga
      "tagIds": [               // Cập nhật danh sách tags: ID có sẵn sẽ giữ lại, ID mới được thêm, ID không có sẽ bị xóa
        "323e4567-e89b-12d3-a456-426614174000" // Giả sử chỉ giữ lại tag này
      ],
      "authors": [              // Cập nhật danh sách tác giả: Tương tự tags
        {
          "authorId": "223e4567-e89b-12d3-a456-426614174000",
          "role": "Author"
        }
      ]
    }
    ```
    *   **Lưu ý:** Cần gửi đầy đủ các trường chính. Đối với `tagIds` và `authors`, danh sách gửi lên sẽ **thay thế hoàn toàn** danh sách hiện có. Nếu bạn gửi một danh sách rỗng, tất cả các tags/authors hiện có sẽ bị xóa.
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request`
    *   `404 Not Found`

#### 6.2.5. `DELETE /Mangas/{id}`

*   **Mô tả:** Xóa một manga và tất cả các dữ liệu liên quan (bản dịch, chapter, trang chapter, ảnh bìa, liên kết tác giả/tag).
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của manga cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

### 6.3. Tags

Tài nguyên Tag đại diện cho các thẻ mô tả thể loại, chủ đề hoặc đặc điểm của manga. Mỗi Tag thuộc về một `TagGroup`.

#### 6.3.1. `POST /Tags`

*   **Mô tả:** Tạo một tag mới và gán nó vào một `TagGroup` đã tồn tại.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Fantasy",        // Tên tag (Bắt buộc, tối đa 100 ký tự)
      "tagGroupId": "abcde123-4567-89ab-cdef-123456789012" // ID của TagGroup (Bắt buộc)
    }
    ```
    *   **Lưu ý:** `tagGroupId` phải là một GUID của `TagGroup` đã tồn tại. Tên tag phải là duy nhất trong cùng một `TagGroup`.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "tag",
            "attributes": {
              "name": "Fantasy",
              "tagGroupId": "abcde123-4567-89ab-cdef-123456789012",
              "tagGroupName": "Genres", // Tên của TagGroup mà tag này thuộc về
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "abcde123-4567-89ab-cdef-123456789012", "type": "tag_group" }
            ]
          }
        }
        ```
    *   `400 Bad Request` (Validation Failed hoặc tag đã tồn tại trong nhóm này)
    *   `404 Not Found` (Nếu `tagGroupId` không tồn tại)

#### 6.3.2. `GET /Tags/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một tag bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tag.
*   **Responses:**
    *   `200 OK` (Ví dụ tương tự response `201 Created` ở trên)
    *   `404 Not Found`

#### 6.3.3. `GET /Tags`

*   **Mô tả:** Lấy danh sách các tags có phân trang, lọc và sắp xếp.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `100`)
    *   `tagGroupId`: (GUID, Tùy chọn) Lọc tags chỉ trong một nhóm tag cụ thể.
    *   `nameFilter`: (Chuỗi, Tùy chọn) Lọc theo tên tag (tìm kiếm chứa chuỗi con).
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `Name`) Tên trường để sắp xếp (`Name`, `TagGroupName`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `true`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<TagAttributesDto>`)

#### 6.3.4. `PUT /Tags/{id}`

*   **Mô tả:** Cập nhật thông tin của một tag, bao gồm tên và nhóm tag.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tag cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Fantasy (Updated)", // Tên tag (Bắt buộc, tối đa 100 ký tự)
      "tagGroupId": "abcde123-4567-89ab-cdef-123456789012" // ID của TagGroup (Bắt buộc)
    }
    ```
    *   **Lưu ý:** Tên tag phải là duy nhất trong `TagGroup` mới (nếu `TagGroupId` thay đổi) hoặc trong `TagGroup` cũ (nếu chỉ thay đổi tên).
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request`
    *   `404 Not Found`

#### 6.3.5. `DELETE /Tags/{id}`

*   **Mô tả:** Xóa một tag bằng ID. Thao tác này sẽ tự động xóa tất cả các liên kết `MangaTag` liên quan đến tag này.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của tag cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

### 6.4. TagGroups (Nhóm Tag)

Tài nguyên TagGroup dùng để phân loại các Tags (ví dụ: "Genres", "Themes", "Content Warnings").

#### 6.4.1. `POST /TagGroups`

*   **Mô tả:** Tạo một nhóm tag mới.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Genres" // Tên nhóm tag (Bắt buộc, tối đa 100 ký tự)
    }
    ```
    *   **Lưu ý:** Tên nhóm tag phải là duy nhất.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "abcde123-4567-89ab-cdef-123456789012",
            "type": "tag_group",
            "attributes": {
              "name": "Genres",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": null
          }
        }
        ```
    *   `400 Bad Request` (Validation Failed hoặc tên nhóm tag đã tồn tại)

#### 6.4.2. `GET /TagGroups/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một nhóm tag bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của nhóm tag.
*   **Responses:**
    *   `200 OK` (Ví dụ tương tự response `201 Created` ở trên)
    *   `404 Not Found`

#### 6.4.3. `GET /TagGroups`

*   **Mô tả:** Lấy danh sách các nhóm tag có phân trang, lọc và sắp xếp.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `100`)
    *   `nameFilter`: (Chuỗi, Tùy chọn) Lọc theo tên nhóm tag (tìm kiếm chứa chuỗi con).
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `Name`) Tên trường để sắp xếp (`Name`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `true`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<TagGroupAttributesDto>`)

#### 6.4.4. `PUT /TagGroups/{id}`

*   **Mô tả:** Cập nhật thông tin của một nhóm tag.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của nhóm tag cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "name": "Categories" // Tên nhóm tag mới (Bắt buộc, tối đa 100 ký tự)
    }
    ```
    *   **Lưu ý:** Tên nhóm tag mới phải là duy nhất và không được trùng với các nhóm tag khác.
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request`
    *   `404 Not Found`

#### 6.4.5. `DELETE /TagGroups/{id}`

*   **Mô tả:** Xóa một nhóm tag bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của nhóm tag cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`
    *   `400 Bad Request` (Nếu nhóm tag còn chứa tags. Phải xóa hết tags con trước khi xóa nhóm.)

### 6.5. TranslatedMangas (Bản Dịch Manga)

Tài nguyên TranslatedManga đại diện cho một bản dịch của Manga gốc sang một ngôn ngữ cụ thể.

#### 6.5.1. `POST /TranslatedMangas`

*   **Mô tả:** Tạo một bản dịch mới cho một Manga gốc.
*   **Request Body:** `application/json`
    ```json
    {
      "mangaId": "123e4567-e89b-12d3-a456-426614174000", // ID của Manga gốc (Bắt buộc)
      "languageKey": "vi",       // Mã ngôn ngữ dịch (Bắt buộc, ví dụ: "en", "vi", "ko", tối đa 10 ký tự)
      "title": "One Piece (Tiếng Việt)", // Tiêu đề đã dịch (Bắt buộc, tối đa 500 ký tự)
      "description": "Câu chuyện về Monkey D. Luffy và đồng đội của anh ấy..." // Mô tả đã dịch (Tùy chọn, tối đa 4000 ký tự)
    }
    ```
    *   **Lưu ý:** `mangaId` phải là GUID của một Manga đã tồn tại. Mỗi Manga chỉ có thể có một bản dịch cho mỗi `languageKey`.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "translated_manga",
            "attributes": {
              "languageKey": "vi",
              "title": "One Piece (Tiếng Việt)",
              "description": "Câu chuyện về Monkey D. Luffy và đồng đội của anh ấy...",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "423e4567-e89b-12d3-a456-426614174000", "type": "manga" }
            ]
          }
        }
        ```
    *   `400 Bad Request` (Validation Failed hoặc bản dịch đã tồn tại cho Manga/LanguageKey)
    *   `404 Not Found` (Nếu `mangaId` không tồn tại)

#### 6.5.2. `GET /TranslatedMangas/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một bản dịch manga bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của bản dịch manga.
*   **Responses:**
    *   `200 OK` (Ví dụ tương tự response `201 Created` ở trên)
    *   `404 Not Found`

#### 6.5.3. `GET /mangas/{mangaId}/translations`

*   **Mô tả:** Lấy danh sách các bản dịch của một Manga cụ thể, có phân trang, lọc và sắp xếp.
*   **Request Parameters:**
    *   `mangaId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của Manga gốc.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`)
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `LanguageKey`) Tên trường để sắp xếp (`LanguageKey`, `Title`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `true`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<TranslatedMangaAttributesDto>`)
    *   `404 Not Found` (Nếu `mangaId` không tồn tại)

#### 6.5.4. `PUT /TranslatedMangas/{id}`

*   **Mô tả:** Cập nhật thông tin của một bản dịch manga.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của bản dịch manga cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "languageKey": "vi",       // Mã ngôn ngữ (Bắt buộc, tối đa 10 ký tự). Cân nhắc việc thay đổi LanguageKey sau khi tạo.
      "title": "One Piece (Bản Dịch Mới)",
      "description": "Mô tả cập nhật của bản dịch tiếng Việt."
    }
    ```
    *   **Lưu ý:** Nếu `languageKey` thay đổi, nó phải là duy nhất cho Manga gốc.
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request`
    *   `404 Not Found`

#### 6.5.5. `DELETE /TranslatedMangas/{id}`

*   **Mô tả:** Xóa một bản dịch manga và tất cả các chapter, trang chapter liên quan đến bản dịch đó.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của bản dịch manga cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

### 6.6. Chapters (Chương)

Tài nguyên Chapter đại diện cho một chương cụ thể của một `TranslatedManga`.

#### 6.6.1. `POST /Chapters`

*   **Mô tả:** Tạo một chương mới cho một bản dịch manga.
*   **Request Body:** `application/json`
    ```json
    {
      "translatedMangaId": "123e4567-e89b-12d3-a456-426614174000", // ID của TranslatedManga (Bắt buộc)
      "uploadedByUserId": 1,      // ID của User đã upload (Bắt buộc, giả định User ID 1 tồn tại)
      "volume": "Vol. 1",         // Số tập (Tùy chọn, tối đa 50 ký tự)
      "chapterNumber": "1",       // Số chương (Tùy chọn, tối đa 50 ký tự)
      "title": "Bắt Đầu Cuộc Phiêu Lưu", // Tiêu đề chương (Tùy chọn, tối đa 255 ký tự)
      "publishAt": "2023-01-01T00:00:00Z", // Thời điểm xuất bản (Bắt buộc)
      "readableAt": "2023-01-01T00:00:00Z" // Thời điểm có thể đọc (Bắt buộc)
    }
    ```
    *   **Lưu ý:** `translatedMangaId` phải là GUID của một `TranslatedManga` đã tồn tại. `uploadedByUserId` phải là ID của một `User` đã tồn tại.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "chapter",
            "attributes": {
              "volume": "Vol. 1",
              "chapterNumber": "1",
              "title": "Bắt Đầu Cuộc Phiêu Lưu",
              "pagesCount": 0, // Mặc định 0 khi mới tạo, pages sẽ được thêm sau
              "publishAt": "2023-01-01T00:00:00Z",
              "readableAt": "2023-01-01T00:00:00Z",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "876e4567-e89b-12d3-a456-426614174000", "type": "manga" }, // ID của manga gốc
              { "id": "1", "type": "user" } // ID của user upload
            ]
          }
        }
        ```
    *   `400 Bad Request`
    *   `404 Not Found` (Nếu `translatedMangaId` hoặc `uploadedByUserId` không tồn tại)

#### 6.6.2. `GET /Chapters/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một chương bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của chương.
*   **Responses:**
    *   `200 OK` (Ví dụ tương tự response `201 Created` ở trên, `pagesCount` sẽ thể hiện số trang thực tế)
    *   `404 Not Found`

#### 6.6.3. `GET /translatedmangas/{translatedMangaId}/chapters`

*   **Mô tả:** Lấy danh sách các chapter của một bản dịch manga cụ thể, có phân trang, lọc và sắp xếp.
*   **Request Parameters:**
    *   `translatedMangaId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của TranslatedManga.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`)
    *   `orderBy`: (Chuỗi, Tùy chọn, mặc định: `ChapterNumber`) Tên trường để sắp xếp (`Volume`, `ChapterNumber`, `PublishAt`).
    *   `ascending`: (Boolean, Tùy chọn, mặc định: `true`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<ChapterAttributesDto>`)
    *   `404 Not Found` (Nếu `translatedMangaId` không tồn tại)

#### 6.6.4. `PUT /Chapters/{id}`

*   **Mô tả:** Cập nhật thông tin của một chương.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của chương cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "volume": "Vol. 2",
      "chapterNumber": "100",
      "title": "Cuộc Chiến Vĩ Đại",
      "publishAt": "2024-01-01T00:00:00Z",
      "readableAt": "2024-01-05T00:00:00Z"
    }
    ```
    *   **Lưu ý:** Các trường không được cung cấp sẽ không được cập nhật. Bạn không thể cập nhật `TranslatedMangaId` hoặc `UploadedByUserId` qua endpoint này.
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request`
    *   `404 Not Found`

#### 6.6.5. `DELETE /Chapters/{id}`

*   **Mô tả:** Xóa một chương và tất cả các trang ảnh (`ChapterPage`) liên quan đến chương đó (bao gồm cả việc xóa ảnh trên Cloudinary).
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của chương cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

#### 6.6.6. `POST /Chapters/{chapterId}/pages/entry`

*   **Mô tả:** Tạo một entry (bản ghi) cho một trang chương mới trong cơ sở dữ liệu. Sau khi tạo entry này, bạn sẽ nhận được `PageId` để upload ảnh cho trang đó.
*   **Request Parameters:**
    *   `chapterId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của chương mà trang này thuộc về.
*   **Request Body:** `application/json`
    ```json
    {
      "pageNumber": 1 // Số trang (Bắt buộc, phải lớn hơn 0)
    }
    ```
    *   **Lưu ý:** `pageNumber` phải là duy nhất trong một chương cụ thể.
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "pageId": "dcb7e89b-12d3-a456-426614174000" // ID của trang mới được tạo
          }
        }
        ```
        `Location` Header: `/chapterpages/dcb7e89b-12d3-a456-426614174000/image` (URL để upload ảnh)
    *   `400 Bad Request` (Validation Failed hoặc `pageNumber` đã tồn tại trong chương)
    *   `404 Not Found` (Nếu `chapterId` không tồn tại)

#### 6.6.7. `GET /Chapters/{chapterId}/pages`

*   **Mô tả:** Lấy danh sách các trang (`ChapterPage`) của một chương cụ thể, có phân trang.
*   **Request Parameters:**
    *   `chapterId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của chương.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<ChapterPageAttributesDto>`)
        ```json
        {
          "result": "ok",
          "response": "collection",
          "data": [
            {
              "id": "dcb7e89b-12d3-a456-426614174000",
              "type": "chapter_page",
              "attributes": {
                "pageNumber": 1,
                "publicId": "chapters/chapter_guid/pages/1.jpg" // Public ID trên Cloudinary
              },
              "relationships": [
                { "id": "123e4567-e89b-12d3-a456-426614174000", "type": "chapter" }
              ]
            }
          ],
          "limit": 20,
          "offset": 0,
          "total": 1
        }
        ```
    *   `404 Not Found` (Nếu `chapterId` không tồn tại)

### 6.7. ChapterPages (Trang Chương)

Tài nguyên ChapterPage đại diện cho một trang ảnh cụ thể trong một chương.

#### 6.7.1. `POST /chapterpages/{pageId}/image`

*   **Mô tả:** Upload ảnh cho một `ChapterPage` entry đã tồn tại. Public ID trên Cloudinary sẽ được tạo dựa trên ChapterId và PageNumber. Nếu ảnh đã tồn tại cho PageId đó, ảnh cũ sẽ bị xóa và ảnh mới sẽ được ghi đè.
*   **Request Parameters:**
    *   `pageId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của `ChapterPage` entry đã được tạo trước đó.
*   **Request Body:** `multipart/form-data`
    *   `file`: (File, Bắt buộc) File ảnh cần upload. Hỗ trợ `.jpg`, `.jpeg`, `.png`, `.webp`. Kích thước tối đa `5MB`.
*   **Responses:**
    *   `200 OK`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "publicId": "chapters/123e4567-e89b-12d3-a456-426614174000/pages/1.jpg" // Public ID của ảnh trên Cloudinary
          }
        }
        ```
    *   `400 Bad Request` (Nếu file không hợp lệ, quá lớn, hoặc không đúng định dạng)
    *   `404 Not Found` (Nếu `pageId` không tồn tại)
    *   `500 Internal Server Error` (Nếu có lỗi khi upload lên Cloudinary)

#### 6.7.2. `PUT /chapterpages/{pageId}/details`

*   **Mô tả:** Cập nhật thông tin chi tiết của một trang chương (ví dụ: số trang).
*   **Request Parameters:**
    *   `pageId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của trang cần cập nhật.
*   **Request Body:** `application/json`
    ```json
    {
      "pageNumber": 5 // Số trang mới (Bắt buộc, phải lớn hơn 0)
    }
    ```
    *   **Lưu ý:** `pageNumber` mới phải là duy nhất trong chapter mà trang này thuộc về.
*   **Responses:**
    *   `204 No Content`
    *   `400 Bad Request` (Validation Failed hoặc `pageNumber` đã tồn tại)
    *   `404 Not Found`

#### 6.7.3. `DELETE /chapterpages/{pageId}`

*   **Mô tả:** Xóa một trang chương bằng ID. Thao tác này sẽ xóa ảnh trên Cloudinary và bản ghi trong database.
*   **Request Parameters:**
    *   `pageId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của trang cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

### 6.8. CoverArts (Ảnh Bìa)

Tài nguyên CoverArt đại diện cho một ảnh bìa của một Manga.

#### 6.8.1. `POST /mangas/{mangaId}/covers`

*   **Mô tả:** Upload một ảnh bìa mới cho một Manga. Public ID trên Cloudinary sẽ được tạo dựa trên MangaId và thông tin volume.
*   **Request Parameters:**
    *   `mangaId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của Manga mà ảnh bìa này thuộc về.
*   **Request Body:** `multipart/form-data`
    *   `file`: (File, Bắt buộc) File ảnh bìa cần upload. Hỗ trợ `.jpg`, `.jpeg`, `.png`, `.webp`. Kích thước tối đa `5MB`.
    *   `volume`: (Chuỗi, Tùy chọn) Số tập mà ảnh bìa này đại diện (tối đa 50 ký tự).
    *   `description`: (Chuỗi, Tùy chọn) Mô tả ngắn về ảnh bìa (tối đa 512 ký tự).
*   **Responses:**
    *   `201 Created`
        ```json
        {
          "result": "ok",
          "response": "entity",
          "data": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "type": "cover_art",
            "attributes": {
              "volume": "Vol. 1",
              "publicId": "mangas_v2/manga_guid/covers/Vol._1_uniqueid.jpg", // Public ID trên Cloudinary
              "description": "Ảnh bìa tập 1.",
              "createdAt": "2023-10-27T10:00:00Z",
              "updatedAt": "2023-10-27T10:00:00Z"
            },
            "relationships": [
              { "id": "423e4567-e89b-12d3-a456-426614174000", "type": "manga" }
            ]
          }
        }
        ```
    *   `400 Bad Request` (Validation Failed hoặc lỗi file)
    *   `404 Not Found` (Nếu `mangaId` không tồn tại)
    *   `500 Internal Server Error` (Nếu có lỗi khi upload lên Cloudinary)

#### 6.8.2. `GET /CoverArts/{id}`

*   **Mô tả:** Lấy thông tin chi tiết của một ảnh bìa bằng ID.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của ảnh bìa.
*   **Responses:**
    *   `200 OK` (Ví dụ tương tự response `201 Created` ở trên)
    *   `404 Not Found`

#### 6.8.3. `GET /mangas/{mangaId}/covers`

*   **Mô tả:** Lấy danh sách các ảnh bìa của một Manga cụ thể, có phân trang.
*   **Request Parameters:**
    *   `mangaId`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của Manga.
*   **Request Parameters (Query):**
    *   `offset`: (Số nguyên, Tùy chọn, mặc định: `0`)
    *   `limit`: (Số nguyên, Tùy chọn, mặc định: `20`)
*   **Responses:**
    *   `200 OK` (Trả về `ApiCollectionResponse` với danh sách `ResourceObject<CoverArtAttributesDto>`)
    *   `404 Not Found` (Nếu `mangaId` không tồn tại)

#### 6.8.4. `DELETE /CoverArts/{id}`

*   **Mô tả:** Xóa một ảnh bìa bằng ID. Thao tác này sẽ xóa ảnh trên Cloudinary và bản ghi trong database.
*   **Request Parameters:**
    *   `id`: (Path Parameter, GUID, Bắt buộc) Định danh duy nhất của ảnh bìa cần xóa.
*   **Responses:**
    *   `204 No Content`
    *   `404 Not Found`

---

**Tài liệu OpenAPI (Swagger) chi tiết:**

Bạn có thể tìm thấy định nghĩa OpenAPI (Swagger) của API này tại `MangaReaderApi.yaml` (trong thư mục gốc của dự án nếu bạn được cung cấp toàn bộ mã nguồn) hoặc truy cập trực tiếp qua `/swagger/v1/swagger.json` trên máy chủ đang chạy API. Tài liệu OpenAPI cung cấp mô tả chính xác và đầy đủ về tất cả các endpoint, tham số, request/response schemas và có thể được sử dụng để tự động tạo mã client trong nhiều ngôn ngữ lập trình.

**Cách truy cập giao diện tài liệu:**
*   **Swagger UI:** Truy cập `https://localhost:7262/swagger` (trong môi trường phát triển)
*   **ReDoc:** Truy cập `https://localhost:7262/docs` (trong môi trường phát triển)

---

## 7. Cập Nhật Quan Trọng: Thay Đổi Định Dạng Enum

### 7.1. Tóm Tắt Thay Đổi

Từ phiên bản hiện tại của API, tất cả các trường dữ liệu kiểu Enum trong **JSON request body** (khi gửi dữ liệu lên) và **JSON response body** (khi nhận dữ liệu về) đều sử dụng **tên chuỗi** (string name) thay vì giá trị số nguyên (integer value) như trước đây.

### 7.2. So Sánh Định Dạng Cũ và Mới

#### Định dạng cũ (không còn được hỗ trợ)

```json
// Request hoặc Response cũ
{
  "status": 0, // 0 có thể là "Ongoing"
  "contentRating": 1, // 1 có thể là "Suggestive"
  "authors": [
    {
      "authorId": "...",
      "role": 0 // 0 là "Author"
    }
  ]
}
```

#### Định dạng mới (hiện tại)

```json
// Request và Response mới
{
  "status": "Ongoing",
  "contentRating": "Suggestive",
  "authors": [
    {
      "authorId": "...",
      "role": "Author"
    }
  ]
}
```

### 7.3. Các Trường Enum Bị Ảnh Hưởng

Các trường sau đây trong các DTO/Model sẽ bị ảnh hưởng bởi thay đổi này:

#### Manga
* `publicationDemographic`: Kiểu `PublicationDemographic` (ví dụ: "Shounen", "Shoujo", "Seinen", "Josei", "None")
* `status`: Kiểu `MangaStatus` (ví dụ: "Ongoing", "Completed", "Hiatus", "Cancelled")
* `contentRating`: Kiểu `ContentRating` (ví dụ: "Safe", "Suggestive", "Erotica", "Pornographic")

#### MangaAuthorInputDto (khi tạo/cập nhật Manga)
* `role`: Kiểu `MangaStaffRole` (ví dụ: "Author", "Artist")

### 7.4. Tác Động Đến Frontend

1. **Gửi dữ liệu (Requests):**
   * Khi tạo hoặc cập nhật Manga, hoặc bất kỳ thao tác nào gửi DTO có chứa các trường Enum, Frontend cần gửi giá trị là **chuỗi tên Enum** thay vì số.
   * Nếu gửi một chuỗi không hợp lệ (không phải là tên của bất kỳ giá trị nào trong Enum tương ứng), API sẽ trả về lỗi `400 Bad Request` với thông báo lỗi chi tiết.

2. **Nhận dữ liệu (Responses):**
   * Khi nhận dữ liệu từ API, Frontend cần đọc các trường Enum dưới dạng **chuỗi tên Enum**.
   * Hãy đảm bảo logic parse JSON ở phía Frontend của bạn có thể xử lý các giá trị chuỗi này.

### 7.5. Lý Do Thay Đổi

Thay đổi này được thực hiện để:
* **Tăng tính đọc hiểu của API:** Giá trị chuỗi rõ ràng hơn và dễ hiểu hơn cho cả người dùng và lập trình viên.
* **Đồng nhất với các API tiêu chuẩn:** Nhiều API hiện đại sử dụng định dạng chuỗi cho Enum.
* **Cải thiện validation:** Mặc dù hệ thống backend vẫn có validation mạnh mẽ, việc hiển thị và nhận chuỗi giúp dễ dàng phát hiện lỗi đầu vào hơn ở cả client và server.

### 7.6. Hành Động Đề Xuất Cho Frontend

* Kiểm tra và cập nhật tất cả các nơi trong code Frontend đang gửi hoặc nhận các trường Enum liên quan.
* Đảm bảo các model hoặc interface ở Frontend được cập nhật để phản ánh rằng các trường này giờ đây là `string` thay vì `number`.
* Thực hiện kiểm thử kỹ lưỡng các luồng dữ liệu liên quan đến Manga và các thực thể có sử dụng Enum.
```