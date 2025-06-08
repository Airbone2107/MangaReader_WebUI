# Thông Báo Cập Nhật API MangaReader

Chào đội ngũ Frontend,

Tài liệu này thông báo về các cập nhật quan trọng đối với MangaReader API mà các bạn cần lưu ý để tích hợp. Các thay đổi này nhằm cải thiện khả năng quản lý trang truyện và cung cấp thêm các tính năng hữu ích.

## 1. Thay Đổi Quan Trọng: Logic Tạo `PublicId` Cho Ảnh Trang (`ChapterPage`)

*   **Thay đổi:** `PublicId` của ảnh trang (`ChapterPage`) trên Cloudinary giờ đây sẽ được tạo dựa trên `ChapterId` và `PageId` của trang đó, thay vì `ChapterId` và `PageNumber` như trước.
*   **Định dạng mới:** `chapters/{ChapterId}/pages/{PageId}` (không bao gồm đuôi file).
*   **Ảnh hưởng:** Nếu Frontend đang tự xây dựng URL ảnh Cloudinary dựa trên `PublicId` và `PageNumber`, logic này cần được cập nhật để sử dụng `PageId`.
*   **Ví dụ:**
    *   **Cũ (ví dụ):** `chapters/guid-chapter-id/pages/1.jpg`
    *   **Mới:** `chapters/guid-chapter-id/pages/guid-page-id` (Client sẽ tự thêm phần mở rộng `.jpg`, `.webp`... khi hiển thị nếu cần, hoặc Cloudinary tự xử lý định dạng dựa trên URL)

    Khi nhận `ChapterPageAttributesDto`, trường `publicId` sẽ có định dạng mới này.

## 2. API Mới

Chúng tôi đã thêm các API mới để quản lý trang truyện một cách linh hoạt hơn:

### 2.1. Upload Hàng Loạt Trang Ảnh (Batch Upload)

*   **Endpoint:** `POST /Chapters/{chapterId}/pages/batch`
*   **Mục đích:** Cho phép người dùng upload nhiều file ảnh cùng lúc để tạo các trang mới cho một chương truyện.
*   **Request Body:** `multipart/form-data`
    *   `files`: (`IFormFile[]`, Bắt buộc) Một mảng các file ảnh.
    *   `pageNumbers`: (`int[]`, Bắt buộc) Một mảng các số trang tương ứng với từng file trong `files`. Thứ tự và số lượng phần tử phải khớp với `files`. Số trang phải > 0 và chưa tồn tại trong chương.
*   **Ví dụ Curl (Minh họa):**
    ```bash
    curl -X POST "https://localhost:7262/Chapters/{chapterId}/pages/batch" \
         -H "Content-Type: multipart/form-data" \
         -F "files=@/path/to/image1.jpg" \
         -F "files=@/path/to/image2.png" \
         -F "pageNumbers=1" \
         -F "pageNumbers=2"
    ```
*   **Response (201 Created):** `ApiResponse<List<ChapterPageAttributesDto>>` chứa danh sách các trang vừa được tạo, mỗi trang có `pageNumber` và `publicId` mới.
    ```json
    {
      "result": "ok",
      "response": "entity", // Hoặc "collection" tùy theo cách BaseApiController xử lý List<T>
      "data": [
        {
          "pageNumber": 1,
          "publicId": "chapters/guid-chapter-id/pages/guid-moi-cua-trang-1"
        },
        {
          "pageNumber": 2,
          "publicId": "chapters/guid-chapter-id/pages/guid-moi-cua-trang-2"
        }
      ]
    }
    ```
*   **Lưu ý:**
    *   Server sẽ tự tạo `PageId` cho mỗi trang mới.
    *   `PublicId` sẽ được tạo theo định dạng `chapters/{chapterId}/pages/{newPageId}`.

### 2.2. Đồng Bộ Hóa Toàn Bộ Trang Ảnh (Sync Pages)

*   **Endpoint:** `PUT /Chapters/{chapterId}/pages`
*   **Mục đích:** Cho phép cập nhật toàn diện danh sách các trang của một chương, bao gồm:
    *   **Xóa** các trang không có trong yêu cầu.
    *   **Cập nhật** các trang hiện có (thay đổi `pageNumber` hoặc thay thế ảnh).
    *   **Thêm mới** các trang.
*   **Request Body:** `multipart/form-data`
    *   `pageOperationsJson`: (String, Bắt buộc) Một chuỗi JSON chứa mảng các đối tượng `PageOperationDto`.
        *   **Cấu trúc `PageOperationDto`:**
            ```json
            {
              "pageId": "guid-cua-trang-neu-la-update", // (GUID, Tùy chọn) ID của trang hiện tại. Để null hoặc bỏ qua nếu là trang mới.
              "pageNumber": 1, // (Số nguyên, Bắt buộc) Số trang mong muốn (thứ tự mới). Phải > 0 và duy nhất trong chapter.
              "fileIdentifier": "file_key_for_page_1" // (Chuỗi, Tùy chọn) Tên key của file trong form-data nếu trang này là mới hoặc cần thay thế ảnh.
                                                    // Client sẽ dùng key này khi gửi IFormFileCollection.
            }
            ```
    *   `files`: (`IFormFileCollection`, Tùy chọn) Các file ảnh mới hoặc cần thay thế. **Tên (key) của mỗi file trong `IFormFileCollection` phải khớp với giá trị `fileIdentifier`** trong `PageOperationDto` tương ứng.
*   **Ví dụ JSON cho `pageOperationsJson`:**
    ```json
    [
      { // Trang 1: Cập nhật trang đã có, thay ảnh
        "pageId": "existing-page-id-1",
        "pageNumber": 1,
        "fileIdentifier": "image_for_page_1"
      },
      { // Trang 2: Thêm trang mới
        "pageId": null, // Hoặc bỏ qua trường này
        "pageNumber": 2,
        "fileIdentifier": "image_for_page_2"
      },
      { // Trang 3: Cập nhật trang đã có, chỉ đổi số trang, không đổi ảnh
        "pageId": "existing-page-id-3",
        "pageNumber": 3,
        "fileIdentifier": null // Hoặc bỏ qua
      }
    ]
    ```
*   **Ví dụ Curl (Minh họa):**
    ```bash
    curl -X PUT "https://localhost:7262/Chapters/{chapterId}/pages" \
         -H "Content-Type: multipart/form-data" \
         -F "pageOperationsJson=[{\"pageId\":\"existing-id-1\",\"pageNumber\":1,\"fileIdentifier\":\"new_image_1\"}, {\"pageNumber\":2,\"fileIdentifier\":\"new_image_2\"}]" \
         -F "new_image_1=@/path/to/updated_image1.jpg" \
         -F "new_image_2=@/path/to/new_image2.png"
    ```
*   **Response (200 OK):** `ApiResponse<List<ChapterPageAttributesDto>>` chứa danh sách các trang của chapter sau khi đã đồng bộ, đã sắp xếp theo `pageNumber`.
    ```json
    {
      "result": "ok",
      "response": "entity",
      "data": [
        {
          "pageNumber": 1,
          "publicId": "chapters/guid-chapter-id/pages/existing-id-1" // Ảnh đã được thay thế
        },
        {
          "pageNumber": 2,
          "publicId": "chapters/guid-chapter-id/pages/guid-moi-cua-trang-2" // Trang mới
        }
        // ... các trang khác sau khi đồng bộ
      ]
    }
    ```
*   **Lưu ý quan trọng cho Frontend khi dùng API Sync:**
    *   **`pageId`**:
        *   Đối với trang hiện có muốn giữ lại hoặc cập nhật, Frontend **PHẢI** gửi `pageId` hiện tại của trang đó.
        *   Đối với trang mới muốn thêm, Frontend có thể gửi `pageId` là `null` (hoặc bỏ qua trường `pageId` trong JSON). Backend sẽ tự tạo `PageId` mới.
        *   **Khuyến nghị:** Để đơn giản cho việc quản lý file, Client có thể tự tạo một GUID mới ở phía Client cho các trang mới và gửi `pageId` đó lên. Server sẽ sử dụng `PageId` này nếu nó chưa tồn tại. Điều này giúp Client dễ dàng liên kết `FileIdentifier` với `PageOperationDto` trước khi gửi request.
    *   **`fileIdentifier`**:
        *   Nếu một `PageOperationDto` có `fileIdentifier`, Frontend PHẢI gửi một file trong `IFormFileCollection` với **tên (key) trùng với `fileIdentifier` đó**.
        *   Nếu không có `fileIdentifier` (hoặc là null/empty) cho một `PageOperationDto` của một trang hiện có (`pageId` được cung cấp), ảnh của trang đó sẽ không bị thay đổi (chỉ `pageNumber` có thể thay đổi).
        *   Nếu `pageId` là `null` (trang mới), thì `fileIdentifier` (và file tương ứng) là bắt buộc.
    *   Các trang không được liệt kê trong `pageOperationsJson` sẽ bị xóa khỏi chapter.

## 3. Hành Động Đề Xuất Cho Frontend

1.  **Cập nhật logic hiển thị ảnh `ChapterPage`:** Nếu client đang tự xây dựng URL ảnh Cloudinary, hãy đảm bảo sử dụng `PageId` (từ trường `publicId` đã được điều chỉnh) thay vì `PageNumber`.
2.  **Tích hợp API Upload Hàng Loạt Trang Ảnh:** Cung cấp giao diện cho người dùng chọn nhiều file và nhập số trang tương ứng.
3.  **Tích hợp API Đồng Bộ Hóa Toàn Bộ Trang Ảnh:**
    *   Đây là API mạnh mẽ cho phép quản lý toàn bộ trang của một chapter. Giao diện người dùng cần cho phép:
        *   Sắp xếp lại thứ tự các trang.
        *   Xóa các trang hiện có.
        *   Thêm các trang mới (upload ảnh).
        *   Thay thế ảnh của các trang hiện có.
    *   Khi chuẩn bị request:
        *   Tạo danh sách `PageOperationDto`.
        *   Đối với mỗi trang mới hoặc trang cần thay ảnh, quyết định một `fileIdentifier` duy nhất (ví dụ: `new_page_1`, `replace_page_abc`).
        *   Thêm các file ảnh vào `IFormFileCollection` với tên (key) chính là các `fileIdentifier` đã quyết định.
    *   Khi `PageId` cho một trang mới là `null` (để server tự sinh), client cần có cách theo dõi file nào tương ứng với DTO nào để gửi `FileIdentifier` đúng. Việc client tự tạo GUID cho `PageId` của trang mới có thể đơn giản hóa việc này.

Vui lòng xem lại tài liệu `MangaReaderAPI.md` đã được cập nhật để có mô tả đầy đủ và chi tiết hơn về các API này cũng như các API khác.

Nếu có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với đội ngũ Backend.

Trân trọng,
Đội ngũ Backend.
```