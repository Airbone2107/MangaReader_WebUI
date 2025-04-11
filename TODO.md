# TODO.md: Triển khai Tính năng Lịch sử Đọc Truyện (Phương án HTMX Trigger Riêng)

## Mục tiêu

Thêm chức năng cho phép người dùng xem lại lịch sử các chương truyện đã đọc, bao gồm thông tin về truyện, chương cuối cùng đọc, và thời gian đọc. Sử dụng trigger HTMX riêng để lưu tiến độ khi trang đọc được tải.

## Các Bước Thực Hiện

### 1. Frontend: Kích Hoạt Lưu Tiến Độ Đọc Bằng HTMX Trigger

**File cần sửa:** `manga_reader_web\Views\Chapter\Read.cshtml`

**Công việc:**

1.  **Thêm thẻ `div` ẩn vào cuối file `Read.cshtml`:** Thẻ này sẽ tự động gửi request POST đến server khi trang được tải hoặc swap vào DOM.
    *   Sử dụng `hx-post` để chỉ định Action Controller sẽ xử lý việc lưu tiến độ (ví dụ: `@Url.Action("SaveReadingProgress", "Chapter")`).
    *   Sử dụng `hx-trigger="load"` để request được gửi ngay khi phần tử này được tải.
    *   Sử dụng `hx-vals` để gửi kèm `mangaId` và `chapterId` lấy từ `@Model`. Ví dụ: `hx-vals='{"mangaId": "@Model.MangaId", "chapterId": "@Model.ChapterId"}'`.
    *   Sử dụng `hx-swap="none"` vì chúng ta không cần cập nhật giao diện từ response của request này.
    *   Ví dụ thẻ div:
        ```html
        @* Thêm vào cuối file Views/Chapter/Read.cshtml *@
        <div hx-post="@Url.Action("SaveReadingProgress", "Chapter")" 
             hx-trigger="load" 
             hx-vals='{"mangaId": "@Model.MangaId", "chapterId": "@Model.ChapterId"}'
             hx-swap="none" 
             aria-hidden="true" 
             style="display: none;">
             <!-- Trigger lưu tiến độ đọc -->
        </div>
        ```
### 2. Backend (Frontend Project): Tạo Action Controller `SaveReadingProgress`

**File cần sửa:** `manga_reader_web\Controllers\ChapterController.cs`

**Công việc:**

1.  **Inject `IHttpClientFactory` và `IUserService`** vào `ChapterController` nếu chưa có.
2.  **Tạo Action `SaveReadingProgress`:**
    *   Đánh dấu Action với `[HttpPost]`.
    *   Action nhận tham số `string mangaId`, `string chapterId` (HTMX sẽ gửi chúng từ `hx-vals`).
    *   **Kiểm tra xác thực:** Dùng `_userService.IsAuthenticated()` để kiểm tra người dùng đã đăng nhập chưa. Nếu chưa, trả về `Unauthorized()` hoặc `Forbid()`.
    *   **Lấy Token:** Lấy token xác thực từ `_userService.GetToken()` (Service này cần được cập nhật để đọc token từ Cookie nếu bạn đã chuyển sang dùng Cookie).
    *   **Gọi Backend API:**
        *   Tạo `HttpClient` từ `_httpClientFactory.CreateClient("BackendApiClient")`.
        *   Thêm header `Authorization: Bearer <token>` vào request.
        *   Tạo request body JSON: `{ "mangaId": mangaId, "lastChapter": chapterId }`.
        *   Gửi request `POST` đến endpoint backend `/api/users/reading-progress`.
        *   Log kết quả (thành công/thất bại).
    *   **Trả về kết quả:** Trả về `Ok()` hoặc `NoContent()` nếu thành công. Trả về `StatusCode(500)` hoặc mã lỗi phù hợp nếu gọi backend thất bại. **Quan trọng:** Không cần trả về nội dung HTML vì `hx-swap="none"`.

### 3. Backend (Frontend Project): Tạo Service Lấy Thông Tin Chapter

**File cần tạo/sửa:** `manga_reader_web\Services\MangaServices\ChapterServices\ChapterInfoService.cs` (Tạo mới) hoặc thêm phương thức vào `ChapterService.cs`.

**Công việc:**

1.  Định nghĩa Model `ChapterInfo` (chứa `Title`, `PublishedAt`).
2.  Tạo Service `ChapterInfoService` hoặc thêm phương thức vào `ChapterService`.
3.  Tạo phương thức `GetChapterInfoAsync(string chapterId)` để gọi API backend proxy (`/mangadex/chapter/{chapterId}`), parse JSON và trả về `ChapterInfo`. (Tham khảo code trong `MangaIdServices.cs`)
4.  Đăng ký Service trong `Program.cs`.

### 4. Frontend: Tạo Model `LastReadMangaViewModel`

**File cần tạo:** `manga_reader_web\Services\MangaServices\Models\LastReadMangaViewModel.cs`

**Công việc:**

1.  Tạo class `LastReadMangaViewModel` với các thuộc tính: `MangaId`, `MangaTitle`, `CoverUrl`, `ChapterId`, `ChapterTitle`, `ChapterPublishedAt`, `LastReadAt`.

### 5. Frontend: Tạo Service Lấy và Xử Lý Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Services\MangaServices\ReadingHistoryService.cs` và Interface `IReadingHistoryService.cs`.

**Công việc:**
1.  Định nghĩa Interface `IReadingHistoryService`.
2.  Tạo Service `ReadingHistoryService`.
3.  Inject các dependency cần thiết (`IHttpClientFactory`, `IUserService`, `MangaTitleService`, `MangaDexService`, `ChapterInfoService`, `IConfiguration`, `ILogger`).
4.  Triển khai `GetReadingHistoryAsync()`:
    *   Kiểm tra xác thực, lấy token.
    *   Gọi API backend `/users/reading-history` để lấy danh sách lịch sử cơ bản (`List<BackendHistoryItem>`).
    *   Lặp qua từng item lịch sử:
        *   **Áp dụng `Task.Delay(_rateLimitDelay)`** trước mỗi lần gọi API MangaDex (lấy tên truyện, ảnh bìa, info chapter).
        *   Lấy thông tin chi tiết (tên truyện, ảnh bìa, tên chương, ngày đăng) từ các service tương ứng. Tên truyện và ảnh bìa lấy giống như `FollowedMangaViewModel`.
        *   Tạo `LastReadMangaViewModel`.
        *   Thêm vào danh sách kết quả.
    *   Sắp xếp kết quả theo `LastReadAt` giảm dần. (Truyện mới đọc sẽ được hiển thị đầu tiên)
    *   Trả về danh sách.
    *   Xử lý lỗi cẩn thận.
5.  Đăng ký Service trong `Program.cs`.

### 6. Frontend: Tạo Partial View cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Views\Shared\_ReadingHistoryItemPartial.cshtml`

**Công việc:** (Giữ nguyên như hướng dẫn trước)

1.  Copy nội dung từ `_FollowedMangaItemPartial.cshtml`.
(Optional: Copy và đổi tên file `_ReadingHistoryItemPartial.cshtml` mới thành `_FollowedMangaItemPartial.cshtml`)
2.  Đổi `@model` thành `LastReadMangaViewModel`.
3.  Đổi tên các class CSS để tránh xung đột.
4.  Cập nhật các thẻ để hiển thị đúng dữ liệu từ `Model`.
5.  Thay thế phần chapter mới nhất bằng thông tin chapter đã đọc cuối cùng.
6.  Thêm phần hiển thị thời gian đọc lần cuối.

### 7. Frontend: Tạo CSS Mới cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\wwwroot\css\pages\history\reading-history-item.css`

**Công việc:** (Giữ nguyên như hướng dẫn trước)

1.  Tạo thư mục và file CSS.
2.  Copy nội dung từ `followed-item.css`.
3.  Tìm và thay thế các class CSS đã đổi tên.
4.  Thêm style cho các phần tử mới (thông tin chapter, thời gian đọc).
5.  Import file CSS mới vào `main.css`.

### 8. Frontend: Tạo Controller Action và View cho Trang Lịch Sử

**File cần sửa:** `manga_reader_web\Controllers\MangaController.cs`
**File cần tạo:**
*   `manga_reader_web\Views\Manga\History.cshtml`
*   `manga_reader_web\Views\Shared\_ReadingHistoryListPartial.cshtml`

**Công việc:**

1.  **Trong `MangaController.cs`:**
    *   Inject `IReadingHistoryService`.
    *   Tạo Action `History()` trả về `View()`.
    *   Tạo Action `GetReadingHistoryPartial()` gọi `_readingHistoryService.GetReadingHistoryAsync()` và trả về `PartialView("_ReadingHistoryListPartial", history)`. Đảm bảo kiểm tra xác thực.
2.  **Tạo View `History.cshtml`:** Chứa container `div#reading-history-container` với các thuộc tính HTMX `hx-get`, `hx-trigger="load"`, `hx-swap="innerHTML"` để tải partial view.
3.  **Tạo Partial View `_ReadingHistoryListPartial.cshtml`:** Hiển thị danh sách lịch sử đọc bằng cách lặp qua Model và render `_ReadingHistoryItemPartial` cho mỗi mục. Hiển thị thông báo nếu danh sách trống.

### 9. Frontend: Thêm Link vào Sidebar

**File cần sửa:** `manga_reader_web\Views\Shared\_Layout.cshtml`

**Công việc:**

1.  Thêm một `<li>` và `<a>` mới vào `ul.navbar-nav` trong sidebar.
2.  Thẻ `<a>` trỏ đến `Manga/History` và sử dụng các thuộc tính HTMX (`hx-get`, `hx-target`, `hx-push-url`) để tải nội dung vào `#main-content`.

### 10. Cập nhật `UserService` (Nếu dùng Cookie)

**File cần sửa:** `manga_reader_web\Services\AuthServices\UserService.cs`

**Công việc:**

1.  Sửa hàm `GetToken()` để đọc giá trị token từ `HttpContext.Request.Cookies["YourTokenCookieName"]`.
2.  Sửa hàm `IsAuthenticated()` để kiểm tra sự tồn tại của cookie hoặc dựa vào `HttpContext.User.Identity.IsAuthenticated` nếu bạn cấu hình Cookie Authentication đúng cách.
3.  Hàm `SaveToken()` và `RemoveToken()` sẽ cần được điều chỉnh hoặc thay thế bằng logic xử lý cookie trong các Action Controller liên quan đến đăng nhập/đăng xuất.

### 11. Kiểm Tra và Hoàn Thiện

**Công việc:**

1.  Chạy ứng dụng, đăng nhập.
2.  Đọc truyện. Kiểm tra Network tab trong DevTools xem request `POST` đến `SaveReadingProgress` có được gửi khi trang đọc tải không. Kiểm tra log backend.
3.  Truy cập trang Lịch sử đọc.
4.  Kiểm tra hiển thị, CSS, link HTMX, tốc độ tải, và responsive.