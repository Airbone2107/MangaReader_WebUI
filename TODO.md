# TODO.md: Triển khai Tính năng Lịch sử Đọc Truyện

## Mục tiêu

Thêm chức năng cho phép người dùng xem lại lịch sử các chương truyện đã đọc, bao gồm thông tin về truyện, chương cuối cùng đọc, và thời gian đọc.

## Các Bước Thực Hiện

### 1. Frontend: Lưu Tiến Độ Đọc Khi Truy Cập Trang Đọc Chapter

**File cần sửa:** `manga_reader_web\wwwroot\js\modules\read-page.js`

**Công việc:**

1.  **Trong hàm khởi tạo của trang đọc (`initReadPage` hoặc tương đương):**
    *   Tìm cách lấy `mangaId` và `chapterId` của chương đang đọc. Dữ liệu này cần được truyền từ `ChapterController` vào `Read.cshtml` và đặt vào DOM (ví dụ: sử dụng data attributes trên một thẻ HTML bao quanh).
    *   Gọi một hàm mới (ví dụ: `saveReadingProgress(mangaId, chapterId)`) để gửi thông tin này lên backend.
    *   Thêm kiểm tra để đảm bảo chỉ gọi hàm lưu khi lấy được cả `mangaId` và `chapterId`.

2.  **Tạo hàm `saveReadingProgress(mangaId, chapterId)`:**
    *   Hàm này sẽ thực hiện một request `fetch` hoặc `axios` với phương thức `POST` đến endpoint `/api/users/reading-progress` của backend.
    *   Kiểm tra xem người dùng đã đăng nhập chưa (cần có cơ chế lấy token xác thực). Nếu chưa đăng nhập, không gửi request.
    *   Trong phần `headers` của request, đặt `Content-Type` là `application/json` và thêm header `Authorization` với giá trị `Bearer <your_auth_token>`. (Cần có hàm helper để lấy token).
    *   Trong phần `body` của request, gửi một đối tượng JSON chứa `mangaId` và `lastChapter` (sử dụng `chapterId` lấy được từ trang đọc).
    *   Xử lý kết quả trả về từ backend: log thành công hoặc lỗi, xử lý trường hợp lỗi 401 (Unauthorized).

### 2. Backend (Frontend Project): Tạo Service Lấy Thông Tin Chapter

**File cần tạo/sửa:** `manga_reader_web\Services\MangaServices\ChapterServices\ChapterInfoService.cs` (Tạo mới) hoặc thêm phương thức vào `ChapterService.cs`.

**Công việc:**

1.  **Định nghĩa Model `ChapterInfo`:** Tạo một class đơn giản `ChapterInfo` trong file service hoặc thư mục Models để chứa `Title` (string) và `PublishedAt` (DateTime).
2.  **Tạo Service `ChapterInfoService` (hoặc thêm vào `ChapterService`):**
    *   Inject `IHttpClientFactory`, `IConfiguration`, `ILogger`, `JsonConversionService`.
    *   Sử dụng `httpClientFactory.CreateClient("BackendApiClient")` để lấy client đã cấu hình.
    *   Lấy `BaseUrl` của backend API từ `IConfiguration`.
3.  **Tạo phương thức `GetChapterInfoAsync(string chapterId)`:**
    *   Kiểm tra `chapterId` có hợp lệ không.
    *   Xây dựng URL để gọi endpoint backend proxy lấy thông tin chapter (ví dụ: `{baseUrl}/mangadex/chapter/{chapterId}`).
    *   Thực hiện request `GET` đến URL trên.
    *   Kiểm tra `response.IsSuccessStatusCode`. Nếu lỗi, log và trả về `null`.
    *   Đọc nội dung response dưới dạng chuỗi JSON.
    *   Sử dụng `JsonSerializer.Deserialize<JsonElement>` để parse JSON.
    *   Kiểm tra cấu trúc JSON trả về (dựa trên `MangaIDData.md`): `result` phải là "ok", phải có `data.attributes`.
    *   Trích xuất `title` từ `data.attributes.title`. Nếu `title` rỗng hoặc null, thử lấy `chapter` từ `data.attributes.chapter` và tạo title dạng "Chương X". Nếu vẫn không có, đặt title mặc định.
    *   Trích xuất `publishAt` từ `data.attributes.publishAt` và chuyển đổi thành `DateTime`.
    *   Tạo và trả về đối tượng `ChapterInfo` với dữ liệu đã trích xuất. Nếu có lỗi parse hoặc thiếu dữ liệu, log và trả về `null`.
4.  **Đăng ký Service:** Đăng ký `ChapterInfoService` trong `Program.cs` (ví dụ: `builder.Services.AddScoped<ChapterInfoService>();`).

### 3. Frontend: Tạo Model `LastReadMangaViewModel`

**File cần tạo:** `manga_reader_web\Services\MangaServices\Models\LastReadMangaViewModel.cs`

**Công việc:**

1.  Tạo class `LastReadMangaViewModel` với các thuộc tính sau:
    *   `string MangaId`
    *   `string MangaTitle`
    *   `string CoverUrl`
    *   `string ChapterId`
    *   `string ChapterTitle`
    *   `DateTime ChapterPublishedAt`
    *   `DateTime LastReadAt`

### 4. Frontend: Tạo Service Lấy và Xử Lý Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Services\MangaServices\ReadingHistoryService.cs` và Interface `IReadingHistoryService.cs`.

**Công việc:**

1.  **Định nghĩa Interface `IReadingHistoryService`:** Khai báo phương thức `Task<List<LastReadMangaViewModel>> GetReadingHistoryAsync();`.
2.  **Tạo Service `ReadingHistoryService`:**
    *   Inject `IHttpClientFactory`, `IUserService`, `MangaTitleService`, `MangaDexService`, `ChapterInfoService`, `IConfiguration`, `ILogger`.
    *   Lấy `BaseUrl` backend API từ `IConfiguration`.
    *   Định nghĩa hằng số `_rateLimitDelay` (ví dụ: `TimeSpan.FromMilliseconds(550)`).
3.  **Triển khai phương thức `GetReadingHistoryAsync()`:**
    *   Kiểm tra người dùng đã đăng nhập chưa bằng `_userService.IsAuthenticated()`. Nếu chưa, trả về danh sách rỗng.
    *   Lấy token xác thực từ `_userService.GetToken()`. Nếu không có token, log lỗi và trả về danh sách rỗng.
    *   Tạo `HttpClient` từ `_httpClientFactory.CreateClient("BackendApiClient")`.
    *   Thêm header `Authorization` với token vào `httpClient`.
    *   Gọi API `GET` đến endpoint backend lấy lịch sử đọc (ví dụ: `{baseUrl}/users/reading-history`).
    *   Kiểm tra `response.IsSuccessStatusCode`. Nếu lỗi (đặc biệt là 401), log lỗi, xử lý token (nếu cần) và trả về danh sách rỗng.
    *   Đọc nội dung response JSON.
    *   Deserialize JSON thành `List<BackendHistoryItem>` (cần tạo class `BackendHistoryItem` tạm thời để khớp với cấu trúc JSON trả về từ backend: `MangaId`, `ChapterId`, `LastReadAt`).
    *   Kiểm tra nếu danh sách `backendHistory` rỗng hoặc null, trả về danh sách rỗng.
    *   Khởi tạo `List<LastReadMangaViewModel> historyViewModels`.
    *   **Lặp qua từng `item` trong `backendHistory`:**
        *   **Áp dụng Rate Limit:** Gọi `await Task.Delay(_rateLimitDelay);` **trước mỗi** lần gọi API đến MangaDex (qua các service khác).
        *   Gọi `_mangaTitleService.GetMangaTitleFromIdAsync(item.MangaId)` để lấy tên truyện.
        *   Gọi `_mangaDexService.FetchCoverUrlAsync(item.MangaId)` để lấy ảnh bìa.
        *   Gọi `_chapterInfoService.GetChapterInfoAsync(item.ChapterId)` để lấy thông tin chapter (tên, ngày đăng).
        *   Kiểm tra xem `chapterInfo` có `null` không. Nếu `null`, log warning và bỏ qua item này (`continue`).
        *   Tạo một đối tượng `LastReadMangaViewModel` mới, điền dữ liệu đã lấy được (tên truyện, ảnh bìa, chapterId, tên chapter, ngày đăng chapter, `item.LastReadAt`).
        *   Thêm `viewModel` vào danh sách `historyViewModels`.
        *   Log thông tin xử lý (debug).
    *   Log số lượng item đã xử lý thành công.
    *   **Sắp xếp `historyViewModels` theo `LastReadAt` giảm dần.**
    *   Trả về danh sách `historyViewModels`.
    *   Bọc toàn bộ logic trong `try-catch` để xử lý lỗi (đặc biệt là `JsonException` khi deserialize).
4.  **Đăng ký Service:** Đăng ký `IReadingHistoryService` và `ReadingHistoryService` trong `Program.cs` (ví dụ: `builder.Services.AddScoped<IReadingHistoryService, ReadingHistoryService>();`).

### 5. Frontend: Tạo Partial View cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\Views\Shared\_ReadingHistoryItemPartial.cshtml`

**Công việc:**

1.  Copy nội dung từ `_FollowedMangaItemPartial.cshtml`.
2.  Đổi `@model` thành `manga_reader_web.Services.MangaServices.Models.LastReadMangaViewModel`.
3.  **Đổi tên các class CSS** để tránh xung đột (ví dụ: `.custom-followed-manga-container` -> `.custom-history-manga-container`, `.followed-cover` -> `.history-cover`, v.v.).
4.  Cập nhật các thẻ `<a>` và `<img>` để sử dụng đúng thuộc tính từ `LastReadMangaViewModel` (`Model.MangaId`, `Model.MangaTitle`, `Model.CoverUrl`).
5.  Thay thế phần hiển thị danh sách chapter mới nhất bằng phần hiển thị thông tin chapter đã đọc lần cuối:
    *   Tạo một thẻ `<a>` trỏ đến `Chapter/Read` với `asp-route-id="@Model.ChapterId"` và các thuộc tính HTMX tương tự.
    *   Bên trong thẻ `<a>`, hiển thị `Model.ChapterTitle` và `Model.ChapterPublishedAt`.
6.  **Thêm phần hiển thị thời gian đọc lần cuối:**
    *   Tạo một thẻ `<div>` hoặc `<small>` để hiển thị `Model.LastReadAt` với định dạng mong muốn (ví dụ: "Đọc lần cuối: dd/MM/yyyy HH:mm"). Thêm icon `bi-clock-history`.

### 6. Frontend: Tạo CSS Mới cho Mục Lịch Sử Đọc

**File cần tạo:** `manga_reader_web\wwwroot\css\pages\history\reading-history-item.css`

**Công việc:**

1.  Tạo thư mục `history` trong `wwwroot/css/pages` nếu chưa có.
2.  Tạo file `reading-history-item.css`.
3.  Copy toàn bộ nội dung từ `wwwroot/css/pages/followed-item.css`.
4.  **Tìm và thay thế** tất cả các class CSS đã đổi tên ở bước 5 (ví dụ: thay `.custom-followed-manga-container` bằng `.custom-history-manga-container`).
5.  Thêm các style cần thiết cho phần hiển thị thông tin chapter đã đọc và thời gian đọc lần cuối (nếu cần tùy chỉnh thêm ngoài các class có sẵn).
6.  **Cập nhật `main.css`:** Thêm dòng `@import url("./pages/history/reading-history-item.css");` vào cuối phần import pages.

### 7. Frontend: Tạo Controller Action và View cho Trang Lịch Sử

**File cần sửa:** `manga_reader_web\Controllers\MangaController.cs`
**File cần tạo:**
*   `manga_reader_web\Views\Manga\History.cshtml`
*   `manga_reader_web\Views\Shared\_ReadingHistoryListPartial.cshtml`

**Công việc:**

1.  **Trong `MangaController.cs`:**
    *   Inject `IReadingHistoryService`.
    *   Tạo Action `public IActionResult History()`:
        *   Kiểm tra xác thực người dùng (`_userService.IsAuthenticated()`). Nếu chưa đăng nhập, chuyển hướng đến `Auth/Login` với `returnUrl`.
        *   Trả về `View()` (sẽ render `Views/Manga/History.cshtml`).
    *   Tạo Action `public async Task<IActionResult> GetReadingHistoryPartial()`:
        *   Kiểm tra xác thực người dùng. Nếu chưa, trả về `Unauthorized()`.
        *   Gọi `await _readingHistoryService.GetReadingHistoryAsync()` để lấy danh sách lịch sử.
        *   Trả về `PartialView("_ReadingHistoryListPartial", history)` với danh sách lịch sử lấy được.
        *   Bọc trong `try-catch` để xử lý lỗi và trả về partial view báo lỗi nếu cần.
2.  **Tạo View `History.cshtml`:**
    *   Đặt `ViewData["Title"] = "Lịch sử đọc";`.
    *   Tạo container chính (ví dụ: `<div class="container mt-4">`).
    *   Thêm tiêu đề trang (ví dụ: `<h1><i class="bi bi-clock-history me-2"></i>Lịch sử đọc</h1>`).
    *   Tạo một `div` với `id="reading-history-container"` và các thuộc tính HTMX:
        *   `hx-get="@Url.Action("GetReadingHistoryPartial", "Manga")"`
        *   `hx-trigger="load"`
        *   `hx-swap="innerHTML"`
    *   Bên trong `div` này, đặt một spinner hoặc thông báo loading ban đầu.
3.  **Tạo Partial View `_ReadingHistoryListPartial.cshtml`:**
    *   Đặt `@model List<manga_reader_web.Services.MangaServices.Models.LastReadMangaViewModel>`.
    *   Kiểm tra nếu `Model` rỗng hoặc `null`: Hiển thị thông báo "Lịch sử đọc trống" và nút "Tìm truyện".
    *   Nếu `Model` có dữ liệu:
        *   Tạo một `div` bao ngoài (ví dụ: `<div class="list-group reading-history-list">`).
        *   Dùng vòng lặp `@foreach (var item in Model)` để duyệt qua danh sách.
        *   Với mỗi `item`, gọi `@Html.Partial("_ReadingHistoryItemPartial", item)` để render partial view của từng mục.
        *   (Tùy chọn) Thêm logic phân trang nếu cần.

### 8. Frontend: Thêm Link vào Sidebar

**File cần sửa:** `manga_reader_web\Views\Shared\_Layout.cshtml`

**Công việc:**

1.  Tìm đến phần `ul.navbar-nav` trong `div#sidebarMenu`.
2.  Thêm một thẻ `<li>` mới với class `nav-item mb-2`.
3.  Bên trong `<li>`, thêm thẻ `<a>` với các thuộc tính:
    *   `class="nav-link sidebar-nav-link p-2"`
    *   `asp-area="" asp-controller="Manga" asp-action="History"`
    *   `hx-get="@Url.Action("History", "Manga")"`
    *   `hx-target="#main-content"`
    *   `hx-push-url="true"`
    *   Nội dung thẻ `<a>`: `<i class="bi bi-clock-history me-2"></i>Lịch sử đọc`

### 9. Kiểm Tra và Hoàn Thiện

1.  Chạy ứng dụng, đăng nhập.
2.  Đọc vài chương truyện khác nhau.
3.  Kiểm tra console log frontend và backend để xem việc lưu tiến độ.
4.  Truy cập trang "Lịch sử đọc" từ sidebar.
5.  Kiểm tra:
    *   Spinner loading ban đầu.
    *   Danh sách lịch sử hiển thị đúng thông tin (ảnh, tên truyện, tên chương, ngày đăng, ngày đọc).
    *   CSS hiển thị đúng, không bị lỗi layout.
    *   Các link trong item lịch sử hoạt động đúng với HTMX.
    *   Tốc độ tải trang (ảnh hưởng bởi rate limit).
    *   Hiển thị trên các kích thước màn hình khác nhau.