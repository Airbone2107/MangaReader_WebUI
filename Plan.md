### **II. Định nghĩa Enum cho Nguồn Truyện**

1.  **Tạo Enum `MangaSourceType.cs`:**
    Tạo một thư mục mới `Enums` trong `MangaReader.WebUI` và định nghĩa một enum mới để liệt kê các loại nguồn truyện mà ứng dụng sẽ hỗ trợ. Các giá trị ví dụ bao gồm `MangaDex` và `MangaReaderLib`.
    *   **Vị trí:** `MangaReader.WebUI\Enums\MangaSourceType.cs`
    *   **Mục đích:** Cung cấp một cách rõ ràng để phân biệt giữa các nguồn dữ liệu khác nhau.

### **III. Tạo các Mapper Chuyển đổi từ DTO của MangaReaderLib sang ViewModel của WebUI**

1.  **Tạo thư mục mới cho các Mapper của MangaReaderLib:**
    Tạo một thư mục mới `MangaReaderLibMappers` trong `MangaReader.WebUI\Services\MangaServices\DataProcessing\Services`. Tương tự, tạo thư mục `MangaReaderLibMappers` trong `MangaReader.WebUI\Services\MangaServices\DataProcessing\Interfaces`.
    *   **Mục đích:** Tổ chức các mapper mới một cách rõ ràng, tách biệt với các mapper của MangaDex.

2.  **Tạo các Interface Mapper cho MangaReaderLib:**
    Trong thư mục `MangaReader.WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaReaderLibMappers`, định nghĩa các interface tương ứng với mỗi cặp ánh xạ. Các interface này sẽ bao gồm:
    *   `IMangaReaderLibToMangaViewModelMapper`
    *   `IMangaReaderLibToMangaDetailViewModelMapper`
    *   `IMangaReaderLibToChapterViewModelMapper`
    *   `IMangaReaderLibToSimpleChapterInfoMapper`
    *   `IMangaReaderLibToMangaInfoViewModelMapper`
    *   `IMangaReaderLibToChapterInfoMapper`
    *   `IMangaReaderLibToTagListResponseMapper`
    *   `IMangaReaderLibToAtHomeServerResponseMapper`
    *   **Mục đích:** Đảm bảo tính trừu tượng và dễ dàng kiểm thử cho các mapper mới.

3.  **Triển khai các Mapper cho MangaReaderLib:**
    Trong thư mục `MangaReader.WebUI\Services\MangaServices\DataProcessing\Services\MangaReaderLibMappers`, triển khai các lớp ánh xạ cụ thể từ DTO của MangaReaderLib (ví dụ: `MangaAttributesDto`, `ChapterAttributesDto`, `TagAttributesDto` từ thư viện `MangaReaderLib`) sang các ViewModel/Models hiện có của WebUI (ví dụ: `MangaViewModel`, `ChapterViewModel`, `TagListResponse`).
    *   **Mục đích:** Cung cấp các công cụ ánh xạ chuyên biệt và hiệu quả cho dữ liệu từ MangaReaderLib, ánh xạ trực tiếp từ DTO của MangaReaderLib sang ViewModel của WebUI.
    *   **Lưu ý quan trọng:** Các mapper này sẽ cần inject các Client của MangaReaderLib (ví dụ: `IAuthorClient`, `ICoverArtClient`, `IChapterClient`) và các service dùng chung (ví dụ: `ILogger`, `LocalizationService`) để lấy thông tin liên quan hoặc xử lý dữ liệu trong quá trình ánh xạ. Ví dụ, để ánh xạ `MangaAttributesDto` sang `MangaViewModel`, mapper cần gọi `IAuthorClient` để lấy tên tác giả/họa sĩ và `ICoverArtClient` để tạo URL ảnh bìa, do các thông tin này chỉ tồn tại dưới dạng ID trong DTO của MangaReaderLib chứ không phải đối tượng lồng ghép. Tương tự, mapper cho Chapter Pages sẽ cần xây dựng URL đầy đủ dựa trên `publicId` và base URL của MangaReaderLib API.

### **IV. Cấu hình các Client API cho MangaReaderLib**

1.  **Cấu hình HttpClient cho MangaReaderLib Backend API:**
    Trong `Program.cs`, thêm cấu hình cho `HttpClient` dành riêng cho MangaReaderLib API. `HttpClient` này sẽ được đặt tên (ví dụ: "MangaReaderLibApiClient") và cấu hình `BaseAddress` từ `MangaReaderApiSettings:BaseUrl` trong `appsettings.json`.
    *   **Mục đích:** Cung cấp một `HttpClient` được đặt tên để giao tiếp với MangaReaderLib.

2.  **Đăng ký các Client từ `MangaReaderLib`:**
    Trong `Program.cs`, đăng ký tất cả các Client API được triển khai trong thư viện `MangaReaderLib` (ví dụ: `AuthorClient`, `ChapterClient`, `MangaClient`) với `HttpClient` đã cấu hình ở bước trên. Các service này sẽ được đăng ký dưới dạng `Scoped` (ví dụ: `builder.Services.AddScoped<IApiClient, ApiClient>();`).
    *   **Mục đích:** Đảm bảo các service này có thể được inject và sử dụng.

### **V. Tạo Service Quản lý Nguồn Truyện Trung tâm (`MangaSourceManagerService`)**

1.  **Tạo thư mục `MangaSourceManager`:**
    Tạo một thư mục mới `MangaReader.WebUI\Services\MangaServices\MangaSourceManager`.
    *   **Mục đích:** Chứa logic cốt lõi cho việc chuyển đổi nguồn.

2.  **Tạo Service `MangaSourceManagerService.cs`:**
    Đây là service trung gian duy nhất và là điểm kiểm soát cho tất cả các yêu cầu dữ liệu manga.
    *   **Vị trí:** `MangaReader.WebUI\Services\MangaServices\MangaSourceManager\MangaSourceManagerService.cs`.
    *   **Interfaces:** `MangaSourceManagerService` sẽ triển khai tất cả các interface của API service hiện có mà các Controller và service cấp cao hơn đang phụ thuộc: `IMangaApiService`, `IChapterApiService`, `ICoverApiService`, `ITagApiService`, và `IApiStatusService`.
    *   **Constructor:** Sẽ nhận vào các dependency cần thiết:
        *   Các triển khai cụ thể của MangaDex API Clients (ví dụ: `MangaApiService`, `ChapterApiService`, `CoverApiService`, `TagApiService`, `ApiStatusService` - các lớp này giờ đây sẽ được inject dưới dạng các kiểu cụ thể thay vì interface).
        *   Tất cả các Client từ thư viện `MangaReaderLib` (ví dụ: `IMangaClient`, `IChapterClient`).
        *   Tất cả các mapper từ `MangaReaderLibMappers` (ví dụ: `IMangaReaderLibToMangaViewModelMapper`).
        *   Tất cả các mapper từ MangaDex DTO sang ViewModel (ví dụ: `IMangaToMangaViewModelMapper`).
        *   `IMangaDataExtractor` (vì các mapper MangaDex vẫn sử dụng nó), `IConfiguration`, `ILogger`, `IHttpContextAccessor`.
    *   **Logic Lựa chọn Nguồn:**
        *   Sẽ có một phương thức nội bộ để đọc lựa chọn nguồn truyện của người dùng từ cookie (dùng key "MangaSource") thông qua `IHttpContextAccessor`. Nếu không có cookie, sẽ mặc định là `MangaSourceType.MangaDex`.
    *   **Triển khai các phương thức truy vấn dữ liệu:**
        *   Mỗi phương thức của các interface mà `MangaSourceManagerService` triển khai (ví dụ: `FetchMangaAsync`, `FetchMangaDetailsAsync`, `GetProxiedCoverUrl`, `FetchTagsAsync`...) sẽ chứa logic như sau:
            1.  Đọc nguồn truyện hiện tại (MangaDex hoặc MangaReaderLib) từ cookie.
            2.  Sử dụng câu lệnh `if/else` hoặc `switch` để xác định logic tiếp theo dựa trên nguồn.
            3.  **Nếu nguồn là MangaDex:** Gọi phương thức tương ứng từ các instance MangaDex API Client đã được inject. Sau đó, sử dụng các mapper MangaDex DTO -> ViewModel để ánh xạ kết quả và trả về.
            4.  **Nếu nguồn là MangaReaderLib:** Gọi phương thức tương ứng từ các instance MangaReaderLib Client đã được inject. Sau đó, sử dụng các mapper MangaReaderLib DTO -> ViewModel để ánh xạ kết quả và trả về.
            5.  Đối với `GetProxiedCoverUrl` và các phương thức liên quan đến URL ảnh chapter, nó sẽ tạo URL phù hợp với cấu trúc của MangaReaderLib (ví dụ: `/mangas/{mangaId}/covers/{publicId}` hoặc `/chapters/{chapterId}/pages/{publicId}` của MangaReaderLib API) hoặc MangaDex tùy theo nguồn đã chọn.
    *   **Mục đích:** Đóng vai trò là điểm truy cập duy nhất cho tất cả các yêu cầu dữ liệu liên quan đến manga, che giấu hoàn toàn sự phức tạp của việc lựa chọn nguồn và ánh xạ dữ liệu thành ViewModel.

### **VI. Cập nhật Dependency Injection (`Program.cs`)**

1.  **Thay đổi đăng ký các `*ApiService` của MangaDex:**
    Các triển khai cụ thể của MangaDex API Clients (ví dụ: `MangaApiService`, `ChapterApiService`, `CoverApiService`, `TagApiService`, `ApiStatusService` trong thư mục `MangaReader.WebUI\Services\APIServices\Services`) sẽ được đăng ký với chính kiểu của chúng (ví dụ: `builder.Services.AddScoped<MangaReader.WebUI.Services.APIServices.Services.MangaApiService>();`).
    *   **Quan trọng:** Chúng sẽ không còn được đăng ký dưới các interface (ví dụ: `IMangaApiService`) nữa, vì các interface đó sẽ được triển khai bởi `MangaSourceManagerService`.

2.  **Đăng ký `MangaSourceManagerService`:**
    Đăng ký `MangaSourceManagerService` với tất cả các interface API mà nó triển khai. Điều này đảm bảo khi một Controller hoặc Service yêu cầu `IMangaApiService`, `IChapterApiService`, v.v., thì `MangaSourceManagerService` sẽ được cung cấp.
    *   **Mục đích:** Đảm bảo `MangaSourceManagerService` là điểm truy cập duy nhất cho các yêu cầu dữ liệu manga ở các tầng cao hơn.

3.  **Đăng ký các Mapper của MangaReaderLib:**
    Đăng ký tất cả các mapper mới đã tạo trong `MangaReaderLibMappers` (ví dụ: `builder.Services.AddScoped<IMangaReaderLibToMangaViewModelMapper, MangaReaderLibToMangaViewModelMapper>();`).

4.  **Cập nhật các Service và Controller:**
    Các Controller và Service (ví dụ: `HomeController`, `MangaController`, `MangaDetailsService`, `ChapterService`, `ChapterReadingServices`, `MangaIdService`, `ChapterLanguageServices`, `MangaInfoService`, `ReadingHistoryService`) sẽ không cần thay đổi lớn về Dependency Injection trong constructor của chúng. Chúng vẫn sẽ nhận các interface API Service (ví dụ: `IMangaApiService`). Tuy nhiên, nhờ các cập nhật trong `Program.cs`, các interface này giờ đây sẽ được resolve thành `MangaSourceManagerService`, tự động chuyển đổi nguồn dữ liệu một cách minh bạch.

### **VII. Cập nhật Giao diện người dùng và JavaScript**

1.  **Cập nhật `_Layout.cshtml`:**
    Thêm một phần tử `div` có `id="customSourceSwitcherItem"` (có role button và tabindex) cho nút chuyển đổi nguồn truyện bên cạnh nút chuyển đổi sáng tối trong dropdown tài khoản. Bên trong nó sẽ có một `span` với `id="customSourceSwitcherText"` để hiển thị tên nguồn và một `i` với `id="customSourceIcon"` cho biểu tượng, cùng với một div cho phần chuyển đổi trực quan (`custom-source-toggle-switch`).
    *   **Mục đích:** Cung cấp phần tử HTML cho nút chuyển đổi nguồn trên giao diện người dùng.

2.  **Cập nhật `wwwroot/css/components/custom-dropdown.css`:**
    Thêm các CSS rules để định kiểu cho nút chuyển đổi nguồn truyện (`.custom-source-switcher`, `.custom-source-toggle-switch`, `.custom-source-toggle-slider`) tương tự như các style đã có của nút chuyển đổi theme, bao gồm các trạng thái `mangadex-source` và `mangareader-source`.
    *   **Mục đích:** Tạo giao diện trực quan cho nút chuyển đổi nguồn.

3.  **Đổi tên và cập nhật `wwwroot/js/modules/theme.js` thành `wwwroot/js/modules/ui-toggles.js`:**
    *   **Vị trí:** `MangaReader.WebUI\wwwroot\js\modules\ui-toggles.js` (tạo file này và di chuyển nội dung từ `theme.js`).
    *   File này sẽ chứa logic cho cả hai nút chuyển đổi (theme và nguồn).
    *   **Trong `ui-toggles.js`:**
        *   Thêm các hằng số cho key localStorage, tên cookie và các loại nguồn (ví dụ: `SOURCE_MANGADEX`, `SOURCE_MANGAREADERLIB`).
        *   Tạo các hàm `saveMangaSource`, `getSavedMangaSource`, `applyMangaSource` tương tự như các hàm cho theme.
        *   Tạo hàm `initCustomSourceSwitcherInternal()` để khởi tạo nút chuyển đổi nguồn. Hàm này sẽ đọc trạng thái nguồn truyện đã lưu từ localStorage/cookie, cập nhật giao diện của nút (biểu tượng, văn bản hiển thị tên nguồn).
        *   Khi nút được click, nó sẽ chuyển đổi giữa các nguồn, lưu lựa chọn mới vào localStorage và cookie. Sau đó, nó sẽ **kích hoạt một yêu cầu HTMX** (sử dụng `htmx.ajax` để GET lại trang hiện tại) để buộc `MangaSourceManagerService` ở backend nhận ra nguồn mới và tải lại dữ liệu.
        *   Hàm `initUIToggles()` sẽ được tạo (hoặc cập nhật nếu đã tồn tại) để gọi cả `initCustomThemeSwitcherInternal()` và `initCustomSourceSwitcherInternal()`.
    *   **Mục đích:** Gom nhóm logic UI toggle, giúp dễ quản lý và khởi tạo, đồng thời đảm bảo việc chuyển đổi nguồn được lưu trữ và kích hoạt tải lại dữ liệu.

4.  **Cập nhật `wwwroot/js/main.js` và `wwwroot/js/modules/htmx-handlers.js`:**
    Sửa đổi các câu lệnh `import` để nhập module `ui-toggles.js` mới thay cho `theme.js`. Đồng thời, cập nhật các hàm khởi tạo trong `main.js` và các hàm `reinitializeAfterHtmxSwap`, `reinitializeAfterHtmxLoad` trong `htmx-handlers.js` để gọi hàm `initUIToggles()` mới.
    *   **Mục đích:** Đảm bảo JavaScript chính của ứng dụng và HTMX khởi tạo đúng các chức năng chuyển đổi UI mới khi trang tải và sau khi nội dung được cập nhật động.

### **VIII. Kiểm tra và Test**

1.  **Chạy ứng dụng:** Khởi động dự án WebUI và MangaReaderLib API backend.
2.  **Kiểm tra chức năng chuyển đổi nguồn:**
    *   Kiểm tra nút chuyển đổi nguồn trên giao diện người dùng.
    *   Chuyển đổi giữa MangaDex và MangaReaderLib.
    *   Đảm bảo nội dung trang chủ (manga mới nhất) thay đổi tương ứng với nguồn đã chọn.
    *   Điều hướng đến trang tìm kiếm, trang chi tiết manga, trang đọc chapter, trang truyện đang theo dõi, lịch sử đọc và đảm bảo chúng hoạt động đúng với nguồn đã chọn.
3.  **Kiểm tra tính bền vững của lựa chọn:**
    *   Thay đổi nguồn, sau đó tải lại trang hoặc đóng/mở trình duyệt để xem lựa chọn nguồn có được duy trì không.
4.  **Kiểm tra các trường hợp lỗi:**
    *   Thử ngắt kết nối với một trong hai API backend và kiểm tra xem ứng dụng có hiển thị thông báo lỗi phù hợp không.
5.  **Kiểm tra hiệu suất:**
    *   So sánh tốc độ tải dữ liệu giữa hai nguồn.
```