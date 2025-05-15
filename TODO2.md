**Mục tiêu:** Tái cấu trúc `Services\MangaServices` để tổ chức các services theo "feature" (tính năng hoặc trang mà service đó phục vụ), giữ nguyên thư mục `DataProcessing` làm nơi chứa các service xử lý dữ liệu dùng chung.

**Các bước chính:**

1.  **Phân Tích và Xác Định Các "Features":**
    *   Xem xét các Controllers (`MangaController`, `ChapterController`, `HomeController` liên quan đến manga) và các trang chính của ứng dụng liên quan đến manga.
    *   Xác định các "features" hoặc "module chức năng" chính. Ví dụ:
        *   `MangaSearchFeature`
        *   `MangaDetailsFeature`
        *   `ChapterReadingFeature`
        *   `UserFollowedMangasFeature`
        *   `UserReadingHistoryFeature`
        *   `HomePageMangasFeature`
    *   Ghi lại danh sách các features này.

2.  **Tạo Cấu Trúc Thư Mục Mới:**
    *   Trong thư mục `Services\MangaServices\`, tạo một thư mục mới tên là `Features`.
    *   Thư mục `DataProcessing` và các service bên trong nó sẽ **giữ nguyên** ở `Services\MangaServices\DataProcessing\`.
        ```
        Services/
        └── MangaServices/
            ├── Features/      <-- Thư mục mới cho các feature services
            │   // ...
            
            ├── DataProcessing/
            │   ├── Interfaces/
            │   │   ├── MangaMapper/
            │   │   └── // ...
            │   └── Services/
            │       ├── MangaMapper/
            │       └── // ...
            ├── APIServices/     (Giữ nguyên)
            └── UtilityServices/ (Giữ nguyên)
        ```
    *   Bên trong `Services\MangaServices\Features\`, tạo các thư mục con tương ứng với mỗi "feature" đã xác định ở Bước 1. Ví dụ:
        ```
        Services/MangaServices/Features/
        ├── MangaSearchFeature/
        ├── MangaDetailsFeature/
        └── ChapterReadingFeature/
        ```
    *   Trong mỗi thư mục feature (ví dụ: `MangaSearchFeature/`), tạo hai thư mục con: `Interfaces` và `Services`.
        ```
        Services/MangaServices/Features/MangaSearchFeature/
        ├── Interfaces/
        └── Services/
        ```

3.  **Phân Loại và Di Chuyển Services Hiện Tại:**
    *   **Services đặc thù cho trang (Page-Specific Services):**
        *   Các services như `MangaDetailsService.cs`, `MangaSearchService.cs` (trong `Services\MangaServices\MangaPageService\`), `ChapterReadingServices.cs`, `FollowedMangaService.cs`, `ReadingHistoryService.cs`, `MangaInfoService.cs` sẽ được di chuyển vào thư mục `Services/` của feature tương ứng.
        *   Ví dụ:
            *   `MangaSearchService.cs` -> `Services/MangaServices/Features/MangaSearchFeature/Services/MangaSearchFeatureService.cs`
            *   `MangaDetailsService.cs` -> `Services/MangaServices/Features/MangaDetailsFeature/Services/MangaDetailsFeatureService.cs`
            *   `ChapterReadingServices.cs` -> `Services/MangaServices/Features/ChapterReadingFeature/Services/ChapterReadingFeatureService.cs`
                *   Logic từ MangaIdService.cs và ChapterLanguageServices.cs sẽ được tích hợp trực tiếp vào ChapterReadingFeatureService.cs.
            *   `FollowedMangaService.cs` -> `Services/MangaServices/Features/UserFollowedMangasFeature/Services/UserFollowedMangasFeatureService.cs`
            *   `ReadingHistoryService.cs` -> `Services/MangaServices/Features/UserReadingHistoryFeature/Services/UserReadingHistoryFeatureService.cs`
            *   `ChapterServices\ChapterService.cs` Sẽ được đổi tên và di chuyển thành một service dùng chung. `ChapterService.cs` -> Features\SharedServices\Services\ChapterInformationService.cs.
            *   `MangaInfoService.cs`: Xem xét tích hợp logic của nó vào các Feature Service cần thiết (ví dụ: `UserFollowedMangasFeatureService` và `UserReadingHistoryFeatureService` có thể tự gọi `IMangaApiService` và các mappers từ `DataProcessing` để lấy thông tin manga cơ bản).
    *   **Interfaces:**
        *   Di chuyển các interface của các service đặc thù cho trang (đã di chuyển ở trên) vào thư mục `Interfaces/` của feature tương ứng.
        *   Ví dụ: `IFollowedMangaService` -> `Services/MangaServices/Features/UserFollowedMangasFeature/Interfaces/IUserFollowedMangasFeatureService.cs`
    *   **Services dùng chung (Shared/Core Services):**
        *   **`DataProcessing/`**: **Giữ nguyên vị trí và cấu trúc hiện tại.** Các mappers (`IMangaToMangaViewModelMapper`, `IChapterToChapterViewModelMapper`, etc.) và `MangaDataExtractorService` sẽ được các "Feature Services" mới inject và sử dụng.
        *   **`APIServices/`**: Giữ nguyên vị trí. Chúng sẽ được inject vào các "Feature Services".
        *   **`UtilityServices/`**: Giữ nguyên ở `Services\UtilityServices\`.


4.  **Refactor và Điều Chỉnh:**
    *   Sau khi di chuyển các service đặc thù vào thư mục `Features`, cập nhật `namespace` trong mỗi file cho phù hợp.
    *   Các "Feature Service" mới (ví dụ: `MangaDetailsFeatureService`) sẽ inject các service dùng chung như `IMangaApiService`, `IChapterApiService` (từ `APIServices`), các mappers và extractors (từ `DataProcessing`), và các utility services.
    *   Ví dụ, `MangaDetailsFeatureService` sẽ không còn chứa logic trích xuất tiêu đề hay tags nữa, mà sẽ gọi `IMangaDataExtractor` hoặc các mappers tương ứng từ `DataProcessing`.

5.  **Cập Nhật Dependency Injection (`Program.cs`):**
    *   Gỡ bỏ đăng ký của các service cũ đã được thay thế hoặc tích hợp (ví dụ: `MangaPageService\MangaSearchService`, `MangaPageService\MangaDetailsService` cũ).
    *   Đăng ký các "Feature Service" mới với các interface tương ứng của chúng (ví dụ: `builder.Services.AddScoped<IMangaSearchFeatureService, MangaSearchFeatureService>();`).
    *   Đảm bảo tất cả các dependency được inject chính xác.

6.  **Cập Nhật Controllers:**
    *   Thay đổi DI trong các Controllers (`MangaController`, `ChapterController`, etc.) để inject các interface của "Feature Services" mới.
    *   Ví dụ: `MangaController` sẽ inject `IMangaDetailsFeatureService` để lấy chi tiết manga, `IMangaSearchFeatureService` để tìm kiếm manga.

7.  **Kiểm Tra và Test:** (Bước này giữ nguyên)
    *   Chạy ứng dụng và kiểm tra kỹ lưỡng tất cả các trang, chức năng liên quan đến manga để đảm bảo mọi thứ hoạt động đúng như trước khi refactor.

Bằng cách này, bạn sẽ đạt được mục tiêu tái cấu trúc theo feature trong khi vẫn giữ `DataProcessing` như một thư viện các service xử lý dữ liệu cốt lõi, dùng chung cho nhiều feature.