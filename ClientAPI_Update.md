Chào bạn,

Dưới đây là nội dung file `UpdateAPI.md` tổng hợp các thay đổi API để thông báo cho Client, cùng với mã nguồn đầy đủ của các file đã được cập nhật trong project sau khi áp dụng các thay đổi từ `Update1.md`, `Update2.md`, và `Update3.md`.

```markdown
<!-- UpdateAPI.md -->
# Thông Báo Cập Nhật API MangaReader

Chào quý vị Client,

Chúng tôi xin thông báo về một số cập nhật quan trọng đối với API MangaReader. Những thay đổi này nhằm cải thiện khả năng lọc dữ liệu, cấu trúc response và cung cấp thêm thông tin chi tiết cho các tài nguyên. Vui lòng xem kỹ các thay đổi dưới đây để có những điều chỉnh phù hợp cho ứng dụng của bạn.

## 1. Thay đổi Tham số Lọc cho API `GET /mangas`

API lấy danh sách manga (`GET /mangas`) đã được cập nhật với các thay đổi sau về tham số lọc:

### 1.1. Lọc theo Đối Tượng Độc Giả (Publication Demographic)

*   Tham số cũ `demographicFilter` (chấp nhận một giá trị `PublicationDemographic` duy nhất) đã được **loại bỏ**.
*   Tham số mới **`publicationDemographicsFilter[]`** được giới thiệu, cho phép bạn lọc theo **một hoặc nhiều** giá trị `PublicationDemographic`.
    *   **Cách sử dụng:** Truyền nhiều giá trị bằng cách lặp lại tham số trong query string.
    *   **Ví dụ:** `GET /mangas?publicationDemographicsFilter=Shounen&publicationDemographicsFilter=Seinen`
        *   Lưu ý: Tên tham số trong code là `PublicationDemographicsFilter`, khi gọi API client sẽ truyền dưới dạng `publicationDemographicsFilter[]` hoặc lặp lại tham số `publicationDemographicsFilter` tùy theo thư viện HTTP client. Model binder của ASP.NET Core sẽ xử lý việc này. Trong tài liệu `api_conventions.md` đã ghi là `publicationDemographicsFilter[]`.

### 1.2. Lọc Tag Nâng Cao

Các tham số mới đã được thêm vào để hỗ trợ lọc manga dựa trên tags một cách linh hoạt hơn:

*   **`includedTags[]`** (mảng các GUID): Lọc các manga PHẢI chứa các tag được chỉ định.
    *   Ví dụ: `GET /mangas?includedTags[]=tagId1&includedTags[]=tagId2`
*   **`includedTagsMode`** (chuỗi: `"AND"` | `"OR"`): Xác định logic kết hợp cho `includedTags[]`.
    *   `"AND"` (mặc định): Manga phải chứa **TẤT CẢ** các tag trong `includedTags[]`.
    *   `"OR"`: Manga phải chứa **ÍT NHẤT MỘT** tag trong `includedTags[]`.
*   **`excludedTags[]`** (mảng các GUID): Lọc các manga **KHÔNG ĐƯỢC** chứa các tag được chỉ định.
    *   Ví dụ: `GET /mangas?excludedTags[]=tagId3&excludedTags[]=tagId4`
*   **`excludedTagsMode`** (chuỗi: `"AND"` | `"OR"`): Xác định logic kết hợp cho `excludedTags[]`.
    *   `"OR"` (mặc định): Manga không được chứa **BẤT KỲ** tag nào trong `excludedTags[]`.
    *   `"AND"`: Manga không được chứa **TẤT CẢ** các tag trong `excludedTags[]` (nghĩa là, nó được phép chứa một số tag trong danh sách này, miễn là không phải tất cả).

**Ví dụ kết hợp:**
Lấy các manga chứa (tag "Action" **VÀ** tag "Adventure") **VÀ** (không chứa tag "Romance"):
`GET /mangas?includedTags[]=action-guid&includedTags[]=adventure-guid&includedTagsMode=AND&excludedTags[]=romance-guid&excludedTagsMode=OR`

## 2. Thay đổi Cấu trúc Response và Tham số `includes` cho API Manga

### 2.1. Cấu trúc `RelationshipObject` Mở rộng

Đối tượng `RelationshipObject` trong các response (cả chi tiết và danh sách) giờ đây có thể chứa thêm trường `attributes`. Trường này sẽ chứa các thuộc tính chi tiết của entity liên quan nếu client yêu cầu thông qua tham số `includes[]`.

```json
// Ví dụ một RelationshipObject có attributes
{
  "id": "author-guid-123",
  "type": "author",
  "attributes": { // Sẽ xuất hiện nếu client yêu cầu includes[]=author
    "name": "Oda Eiichiro",
    "biography": "Tác giả của One Piece...",
    // ... các thuộc tính khác của AuthorAttributesDto
  }
}
```

### 2.2. Thông tin Tags của Manga

*   **Luôn được nhúng vào `attributes`**: Đối với cả API lấy danh sách manga (`GET /mangas`) và API lấy chi tiết manga (`GET /mangas/{id}`), thông tin chi tiết của các tags liên quan đến manga sẽ **luôn được trả về** và được nhúng trực tiếp vào trường `data.attributes.tags`.
*   Mỗi tag trong danh sách `data.attributes.tags` sẽ là một `ResourceObject<TagAttributesDto>` đầy đủ, bao gồm `id`, `type: "tag"`, `attributes` (chứa `name`, `tagGroupName`, `createdAt`, `updatedAt` của tag), và `relationships` (chứa liên kết đến `tag_group` của tag đó).
*   **Tags không còn trong `relationships`**: Do tags đã được nhúng vào `attributes`, chúng sẽ không còn xuất hiện trong mảng `data.relationships` của đối tượng Manga nữa.

**Ví dụ cấu trúc `data.attributes` của Manga:**
```json
"attributes": {
  "title": "Komi Can't Communicate",
  // ... các thuộc tính khác của Manga ...
  "tags": [
    {
      "id": "tag-guid-comedy",
      "type": "tag",
      "attributes": {
        "name": "Comedy",
        "tagGroupId": "tag-group-genre-guid",
        "tagGroupName": "Genre",
        "createdAt": "2023-01-01T00:00:00Z",
        "updatedAt": "2023-01-01T00:00:00Z"
      },
      "relationships": [
        {
          "id": "tag-group-genre-guid",
          "type": "tag_group"
        }
      ]
    },
    // ... các tags khác ...
  ]
}
```

### 2.3. Tham số `includes[]` cho API Manga

#### API `GET /mangas` (Danh sách Manga):

*   **`includes[]=cover_art`**:
    *   Nếu được yêu cầu, `relationships` của mỗi Manga trong danh sách sẽ chứa một đối tượng `RelationshipObject` cho ảnh bìa chính (mới nhất).
    *   `id` của relationship này sẽ là **`PublicId`** của ảnh bìa (ví dụ: `mangas_v2/.../covers/...`).
    *   `type` sẽ là `"cover_art"`.
    *   Trường `attributes` của relationship này sẽ là `null` (không trả về chi tiết của CoverArt trong danh sách manga).
    *   **Ví dụ:** `{ "id": "mangas_v2/manga-guid/covers/volume1_abc123", "type": "cover_art" }`
*   **`includes[]=author`**:
    *   Nếu được yêu cầu, `relationships` của mỗi Manga sẽ chứa các `RelationshipObject` cho tác giả (những `MangaAuthor` có `Role` là `Author`).
    *   `id` là GUID của Author.
    *   `type` là `"author"`.
    *   `attributes` sẽ chứa đầy đủ `AuthorAttributesDto` của tác giả đó.
*   **`includes[]=artist`**:
    *   Tương tự như `author`, nhưng cho họa sĩ (`Role` là `Artist`), `type` sẽ là `"artist"`.

#### API `GET /mangas/{id}` (Chi tiết Manga):

*   **`includes[]=author`**:
    *   Tương tự như API danh sách, `relationships` của Manga sẽ chứa `RelationshipObject` cho tác giả, với `attributes` là `AuthorAttributesDto`.
*   **`includes[]=artist`**:
    *   Tương tự như API danh sách, `relationships` của Manga sẽ chứa `RelationshipObject` cho họa sĩ, với `attributes` là `AuthorAttributesDto`.
*   **Cover Art trong chi tiết Manga:**
    *   `relationships` của Manga sẽ luôn chứa một `RelationshipObject` cho ảnh bìa chính (mới nhất) nếu có.
    *   `id` của relationship này là **GUID** của `CoverArt` entity.
    *   `type` là `"cover_art"`.
    *   Trường `attributes` của relationship này là `null` (để lấy chi tiết cover art, client nên gọi API `GET /coverarts/{coverId}`).

## 3. Khuyến nghị

*   Vui lòng xem lại và cập nhật logic xử lý response phía client của bạn để phù hợp với cấu trúc `tags` mới trong `data.attributes` của Manga.
*   Điều chỉnh cách bạn gửi yêu cầu lọc theo `publicationDemographic` và tận dụng các tham số lọc tag nâng cao mới.
*   Kiểm tra cách bạn sử dụng tham số `includes[]` và xử lý trường `attributes` mới trong `RelationshipObject`.
*   Tham khảo tài liệu API (`docs/api_conventions.md`) đã được cập nhật để biết thêm chi tiết về các endpoint và cấu trúc dữ liệu.

Chúng tôi tin rằng những cập nhật này sẽ giúp ứng dụng của bạn tương tác với API một cách hiệu quả và mạnh mẽ hơn.

Trân trọng,
Đội ngũ phát triển MangaReader API.