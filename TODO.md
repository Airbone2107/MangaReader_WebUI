# TODO List - Chức năng Lịch sử đọc

Đây là danh sách các công việc cần làm để triển khai tính năng theo dõi và hiển thị lịch sử đọc truyện của người dùng.

## 1. Backend (Giả định & Chuẩn bị)

*   **Quan trọng:** Đảm bảo rằng backend API đã có sẵn một endpoint (ví dụ: `POST /api/users/reading-progress`) để nhận và lưu trữ thông tin lịch sử đọc.
*   Endpoint này nên nhận `mangaId` và `chapterId` (chương cuối cùng người dùng đọc) trong request body.
*   Backend sẽ tự động cập nhật `lastReadAt` khi nhận được request.

## 2. Frontend - Service Lưu Lịch sử đọc

**Mục tiêu:** Tạo một service mới ở frontend để gửi thông tin chương đang đọc lên backend.

**Files cần tạo/sửa:**

1.  `manga_reader_web\Services\MangaServices\IReadingHistoryService.cs` (Tạo mới)
2.  `manga_reader_web\Services\MangaServices\ReadingHistoryService.cs` (Tạo mới)
3.  `manga_reader_web\Program.cs` (Sửa)

**Các bước thực hiện:**

1.  **Tạo Interface (`IReadingHistoryService.cs`):**
    *   Định nghĩa interface `IReadingHistoryService` với một phương thức:
        ```csharp
        Task UpdateReadingProgressAsync(string mangaId, string chapterId);
        ```

2.  **Tạo Implementation (`ReadingHistoryService.cs`):**
    *   Tạo class `ReadingHistoryService` implement `IReadingHistoryService`.
    *   Inject `IHttpClientFactory`, `IUserService`, `ILogger<ReadingHistoryService>` vào constructor.
    *   Triển khai phương thức `UpdateReadingProgressAsync`:
        *   Kiểm tra người dùng đã đăng nhập chưa (`_userService.IsAuthenticated()`). Nếu chưa, không làm gì cả.
        *   Lấy JWT token (`_userService.GetToken()`).
        *   Tạo `HttpClient` từ factory (`_httpClientFactory.CreateClient("BackendApiClient")`).
        *   Thêm `Authorization` header với token.
        *   Xác định URL endpoint của backend (ví dụ: `/api/users/reading-progress`).
        *   Tạo một object chứa `mangaId` và `lastChapter` (giá trị là `chapterId`). *Lưu ý tên thuộc tính `lastChapter` có thể cần khớp với backend*.
        *   Serialize object thành JSON payload.
        *   Tạo `StringContent` từ JSON payload.
        *   Thực hiện request `POST` đến backend endpoint.
        *   Ghi log kết quả (thành công hoặc lỗi). Xử lý các mã lỗi (ví dụ: 401 Unauthorized thì xóa token).

3.  **Đăng ký Service (`Program.cs`):**
    *   Thêm dòng sau vào phần cấu hình services:
        ```csharp
        builder.Services.AddScoped<manga_reader_web.Services.MangaServices.IReadingHistoryService, manga_reader_web.Services.MangaServices.ReadingHistoryService>();
        ```

## 3. Frontend - Service Lấy Thông tin Chapter

**Mục tiêu:** Tạo một service để lấy thông tin hiển thị cơ bản (số chương, tên chương) từ `chapterId`.

**Files cần tạo/sửa:**

1.  `manga_reader_web\Services\MangaServices\ChapterServices\IChapterDetailsService.cs` (Tạo mới)
2.  `manga_reader_web\Services\MangaServices\ChapterServices\ChapterDetailsService.cs` (Tạo mới)
3.  `manga_reader_web\Program.cs` (Sửa)

**Các bước thực hiện:**

1.  **Tạo Interface (`IChapterDetailsService.cs`):**
    *   Định nghĩa interface `IChapterDetailsService` với một phương thức:
        ```csharp
        // Trả về Tuple hoặc một class nhỏ chứa Number và Title
        Task<(string Number, string Title)> GetChapterDisplayInfoAsync(string chapterId);
        ```

2.  **Tạo Implementation (`ChapterDetailsService.cs`):**
    *   Tạo class `ChapterDetailsService` implement `IChapterDetailsService`.
    *   Inject `MangaDexService`, `ILogger<ChapterDetailsService>`, `JsonConversionService`.
    *   Triển khai phương thức `GetChapterDisplayInfoAsync`:
        *   Gọi `_mangaDexService.FetchChapterInfoAsync(chapterId)` để lấy dữ liệu chapter thô.
        *   Sử dụng `_jsonConversionService` để chuyển đổi `JsonElement` thành `Dictionary<string, object>`.
        *   Trích xuất `attributes.chapter` và `attributes.title`.
        *   Định dạng lại tiêu đề hiển thị (ví dụ: "Chương X: Tên chương" hoặc "Chương X" nếu không có tên).
        *   Trả về Tuple `(chapterNumber, displayTitle)`.
        *   Xử lý các trường hợp lỗi (không tìm thấy chapter, lỗi API).

3.  **Đăng ký Service (`Program.cs`):**
    *   Thêm dòng sau:
        ```csharp
        builder.Services.AddScoped<manga_reader_web.Services.MangaServices.ChapterServices.IChapterDetailsService, manga_reader_web.Services.MangaServices.ChapterServices.ChapterDetailsService>();
        ```

## 4. Frontend - Controller Trigger Lưu Lịch sử

**Mục tiêu:** Gọi service lưu lịch sử khi người dùng truy cập trang đọc truyện.

**Files cần sửa:**

1.  `manga_reader_web\Controllers\ChapterController.cs`

**Các bước thực hiện:**

1.  **Inject Service:** Thêm `IReadingHistoryService` vào constructor của `ChapterController`.
    ```csharp
    private readonly IReadingHistoryService _readingHistoryService;

    public ChapterController(
        // ... các service khác
        IReadingHistoryService readingHistoryService)
    {
        // ... gán các service khác
        _readingHistoryService = readingHistoryService;
    }
    ```
2.  **Gọi Service trong Action `Read`:** Trong phương thức `Read(string id)`, sau khi đã lấy được `viewModel` thành công và *trước khi* `return View(viewModel)` hoặc `PartialView(viewModel)`:
    ```csharp
    // ... lấy viewModel thành công ...

    // Gọi service để cập nhật lịch sử đọc (không cần đợi kết quả)
    // Sử dụng Task.Run để chạy ngầm, tránh làm chậm việc hiển thị trang
    _ = Task.Run(async () => {
        try
        {
            await _readingHistoryService.UpdateReadingProgressAsync(viewModel.MangaId, id);
            _logger.LogInformation($"Đã gửi yêu cầu cập nhật lịch sử đọc cho manga {viewModel.MangaId}, chapter {id}");
        }
        catch (Exception historyEx)
        {
            _logger.LogError(historyEx, $"Lỗi khi cập nhật lịch sử đọc cho manga {viewModel.MangaId}, chapter {id}");
        }
    });

    // Sử dụng ViewRenderService để trả về view phù hợp với loại request
    return _viewRenderService.RenderViewBasedOnRequest(this, viewModel);
    ```
    *   Sử dụng `_ = Task.Run(...)` để chạy tác vụ cập nhật lịch sử đọc ở chế độ "fire-and-forget", không làm chậm quá trình trả về view cho người dùng.
    *   Thêm `try-catch` bên trong `Task.Run` để ghi log lỗi nếu có vấn đề khi cập nhật lịch sử.

## 5. Frontend - Chuẩn bị Dữ liệu cho Trang Profile

**Mục tiêu:** Lấy và chuẩn bị dữ liệu lịch sử đọc để hiển thị trên trang Profile.

**Files cần tạo/sửa:**

1.  `manga_reader_web\Models\ReadingHistoryViewModel.cs` (Tạo mới)
2.  `manga_reader_web\Models\ProfileViewModel.cs` (Sửa)
3.  `manga_reader_web\Controllers\AuthController.cs` (Sửa)

**Các bước thực hiện:**

1.  **Tạo `ReadingHistoryViewModel.cs`:**
    *   Tạo một class mới để chứa thông tin cần hiển thị cho mỗi mục lịch sử:
        ```csharp name=ReadingHistoryViewModel.cs
        namespace manga_reader_web.Models
        {
            public class ReadingHistoryViewModel
            {
                public string MangaId { get; set; }
                public string MangaTitle { get; set; }
                public string MangaCoverUrl { get; set; }
                public string ChapterId { get; set; }
                public string ChapterNumber { get; set; }
                public string ChapterTitle { get; set; } // Tiêu đề đã format
                public DateTime LastReadAt { get; set; }
            }
        }
        ```

2.  **Cập nhật `ProfileViewModel.cs`:**
    *   Thêm một thuộc tính mới để chứa danh sách lịch sử đọc:
        ```csharp
        public List<ReadingHistoryViewModel> ReadingHistory { get; set; } = new List<ReadingHistoryViewModel>();
        ```

3.  **Cập nhật `AuthController.cs`:**
    *   **Inject Service:** Thêm `IChapterDetailsService` vào constructor.
    *   **Sửa Action `Profile`:**
        *   Sau khi lấy được `user` và trước khi `return View(viewModel)`.
        *   Kiểm tra `user.ReadingManga` có dữ liệu không.
        *   Nếu có, lặp qua từng `readingInfo` trong `user.ReadingManga`.
        *   Bên trong vòng lặp:
            *   Gọi `_mangaDetailsService.GetMangaDetailsAsync(readingInfo.MangaId)` để lấy thông tin manga (chỉ cần title và cover). *Tối ưu:* Có thể tạo một phương thức gọn hơn trong `MangaDetailsService` chỉ lấy title và cover nếu cần.
            *   Gọi `_chapterDetailsService.GetChapterDisplayInfoAsync(readingInfo.LastChapter)` để lấy thông tin chapter (number, title). *Lưu ý:* `readingInfo.LastChapter` thực chất là `chapterId`.
            *   Tạo một đối tượng `ReadingHistoryViewModel` mới và gán các giá trị đã lấy được.
            *   Thêm đối tượng này vào `viewModel.ReadingHistory`.
        *   Xử lý lỗi nếu không lấy được thông tin manga hoặc chapter (có thể bỏ qua mục đó hoặc hiển thị thông báo lỗi).

## 6. Frontend - Hiển thị Lịch sử đọc trên Trang Profile

**Mục tiêu:** Thêm tab "Lịch sử đọc" và hiển thị danh sách các truyện đã đọc gần đây.

**Files cần tạo/sửa:**

1.  `manga_reader_web\Views\Auth\Profile.cshtml` (Sửa)
2.  `manga_reader_web\Views\Shared\_ReadingHistoryPartial.cshtml` (Tạo mới - tùy chọn)
3.  CSS liên quan (nếu cần)

**Các bước thực hiện:**

1.  **Thêm Tab (`Profile.cshtml`):**
    *   Trong `<ul class="nav nav-tabs" id="profileTabs">`, thêm một `<li>` và `<button>` mới:
        ```html
        <li class="nav-item" role="presentation">
            <button class="nav-link" id="history-tab" data-bs-toggle="tab" data-bs-target="#history" type="button" role="tab" aria-controls="history" aria-selected="false">
                <i class="bi bi-clock-history me-1"></i> Lịch sử đọc
            </button>
        </li>
        ```

2.  **Thêm Tab Pane (`Profile.cshtml`):**
    *   Trong `<div class="tab-content" id="profileTabsContent">`, thêm một `<div>` mới:
        ```html
        <div class="tab-pane fade" id="history" role="tabpanel" aria-labelledby="history-tab">
            @* Nội dung lịch sử đọc sẽ được render ở đây *@
        </div>
        ```

3.  **Render Danh sách Lịch sử (trong `#history` tab pane):**
    *   **Cách 1: Render trực tiếp trong `Profile.cshtml`:**
        ```html
        @if (Model.ReadingHistory != null && Model.ReadingHistory.Any())
        {
            <div class="list-group">
                @foreach (var item in Model.ReadingHistory.OrderByDescending(h => h.LastReadAt)) // Sắp xếp theo thời gian đọc mới nhất
                {
                    <div class="list-group-item list-group-item-action d-flex align-items-center">
                        <img src="@item.MangaCoverUrl" alt="@item.MangaTitle" class="me-3 rounded" style="width: 50px; height: 70px; object-fit: cover;" onerror="this.onerror=null; this.src='/images/cover-placeholder.jpg';" />
                        <div class="flex-grow-1">
                            <h6 class="mb-1">
                                <a asp-controller="Manga" asp-action="Details" asp-route-id="@item.MangaId" class="text-decoration-none">
                                    @item.MangaTitle
                                </a>
                            </h6>
                            <p class="mb-1 small">
                                Đã đọc: <span class="fw-bold">@item.ChapterTitle</span>
                            </p>
                            <small class="text-muted">
                                <i class="bi bi-clock me-1"></i> @item.LastReadAt.ToString("dd/MM/yyyy HH:mm")
                            </small>
                        </div>
                        <a asp-controller="Chapter" asp-action="Read" asp-route-id="@item.ChapterId"
                           class="btn btn-sm btn-primary ms-3"
                           hx-get="@Url.Action("Read", "Chapter", new { id = item.ChapterId })"
                           hx-target="#main-content"
                           hx-push-url="true">
                            <i class="bi bi-book-fill me-1"></i> Đọc tiếp
                        </a>
                    </div>
                }
            </div>
        }
        else
        {
            <div class="text-center py-5">
                <i class="bi bi-book" style="font-size: 3rem;"></i>
                <h5 class="mt-3">Bạn chưa đọc manga nào</h5>
                <p class="text-muted">Bắt đầu đọc manga để ghi lại tiến độ.</p>
                <a href="@Url.Action("Search", "Manga")" class="btn btn-primary">
                    <i class="bi bi-search me-1"></i> Tìm manga
                </a>
            </div>
        }
        ```
    *   **Cách 2: Sử dụng Partial View (Khuyến nghị):**
        *   Tạo file `_ReadingHistoryPartial.cshtml`.
        *   Copy nội dung render danh sách (phần `@if ... @else ...`) vào file partial này. Đổi `@Model` thành `@model List<ReadingHistoryViewModel>`.
        *   Trong `Profile.cshtml`, tại vị trí render, gọi:
            ```html
            @Html.Partial("_ReadingHistoryPartial", Model.ReadingHistory)
            ```

4.  **CSS (Nếu cần):** Thêm các style cần thiết cho danh sách lịch sử đọc để đảm bảo giao diện đẹp mắt, có thể tái sử dụng các class từ danh sách theo dõi.

## 7. Kiểm tra và Hoàn thiện

*   **Kiểm tra chức năng:**
    *   Đăng nhập.
    *   Đọc một vài chapter của các manga khác nhau.
    *   Vào trang Profile, kiểm tra tab "Lịch sử đọc".
    *   Dữ liệu có hiển thị đúng (ảnh bìa, tên truyện, tên chương, thời gian)?
    *   Danh sách có được sắp xếp theo thời gian đọc mới nhất không?
    *   Nút "Đọc tiếp" có chuyển đúng đến chapter đã lưu không?
    *   Đọc tiếp một chapter khác của cùng một truyện, kiểm tra xem lịch sử có cập nhật đúng chapter mới nhất không?
    *   Đăng xuất và đăng nhập lại, kiểm tra lịch sử vẫn còn.
*   **Kiểm tra giao diện:** Đảm bảo hiển thị tốt trên các kích thước màn hình khác nhau.
*   **Xử lý lỗi:** Kiểm tra các trường hợp lỗi (không lấy được thông tin manga/chapter).
*   **Tối ưu:** Xem xét việc cache thông tin manga/chapter nếu cần thiết để giảm tải cho API.

Hoàn thành các bước trên sẽ giúp bạn triển khai thành công chức năng Lịch sử đọc cho ứng dụng.