## Các Bước Lớn Thực Hiện Tái Cấu Trúc

1.  **Chuẩn bị Project Library cho MangaDex:**
    *   **Tạo Project Mới:** Tạo một Class Library project mới trong solution của bạn, ví dụ đặt tên là `MangaDexComponents.Lib`.
    *   **Di chuyển Mã Nguồn MangaDex:**
        *   **Models:** Chuyển toàn bộ thư mục `MangaReader_WebUI\Models\MangaDex\` sang `MangaDexComponents.Lib\Models\`. Cập nhật namespace tương ứng.
        *   **Services (API Callers):** Chuyển các service chịu trách nhiệm gọi API MangaDex (thông qua proxy backend) từ `MangaReader_WebUI\Services\APIServices\Services\` (ví dụ: `MangaApiService.cs`, `ChapterApiService.cs`, `CoverApiService.cs`, `TagApiService.cs`, `ApiStatusService.cs` của MangaDex) và các interface tương ứng từ `MangaReader_WebUI\Services\APIServices\Interfaces\` sang `MangaDexComponents.Lib\Services\`. Cập nhật namespace.
        *   **Mappers (MangaDex):** Chuyển các mappers dành riêng cho việc xử lý dữ liệu từ MangaDex (ví dụ: từ `MangaReader_WebUI\Services\MangaServices\DataProcessing\Interfaces\MangaMapper\` và `MangaReader_WebUI\Services\MangaServices\DataProcessing\Services\MangaMapper\`) sang `MangaDexComponents.Lib\DataProcessing\`. Cập nhật namespace.
        *   **OpenAPI Spec:** Di chuyển file `MangaReader_WebUI\api.yaml` (OpenAPI spec của MangaDex) vào project `MangaDexComponents.Lib` làm tài liệu tham khảo.
        *   **BaseApiService:** Sao chép và điều chỉnh lớp `BaseApiService` từ `MangaReader_WebUI\Services\APIServices\BaseApiService.cs` cho phù hợp với `MangaDexComponents.Lib` (ví dụ, base URL sẽ trỏ đến proxy MangaDex).
    *   **Dependencies:** Đảm bảo `MangaDexComponents.Lib` có đủ các nuget package cần thiết (ví dụ: `System.Text.Json`, `Microsoft.Extensions.Logging.Abstractions`).
    *   **Build Độc Lập:** Đảm bảo project `MangaDexComponents.Lib` có thể build thành công mà không phụ thuộc vào `MangaReader_WebUI`.

2.  **Dọn Dẹp Mã Nguồn MangaDex khỏi `MangaReader_WebUI`:**
    *   **Xóa Files/Folders:** Sau khi đã di chuyển thành công ở Bước 1, xóa các thư mục và file tương ứng đã được chuyển đi khỏi project `MangaReader_WebUI`.
    *   **Cấu Hình:**
        *   Trong `Program.cs`: Xóa bỏ đăng ký `HttpClient` có tên "MangaDexClient". Xem xét lại `HttpClient` có tên "BackendApiClient"; nếu nó chỉ dùng để proxy MangaDex thì có thể xóa hoặc điều chỉnh BaseUrl để không còn phần `/mangadex`.
        *   Trong `appsettings.json` và `appsettings.Development.json`: Gỡ bỏ các cấu hình không còn sử dụng liên quan đến MangaDex.
    *   **Services và Logic Đa Nguồn:**
        *   Xóa bỏ `MangaSourceManagerService` và toàn bộ các `Strategy` trong thư mục `MangaReader_WebUI\Services\MangaServices\MangaSourceManager\`.
        *   Xóa Enum `MangaReader_WebUI\Enums\MangaSource.cs`.
        *   Xóa bỏ các đăng ký service liên quan đến MangaDex và `MangaSourceManagerService` trong `Program.cs`.

3.  **Tái Cấu Trúc `MangaReader_WebUI` để Sử Dụng `MangaReaderLib` Làm Nguồn Duy Nhất:**
    *   **`Program.cs`:**
        *   Đảm bảo các client của `MangaReaderLib` (ví dụ: `IMangaReaderLibMangaClient`, `IMangaReaderLibChapterClient`, `IMangaReaderLibCoverApiService`...) được đăng ký đầy đủ và chính xác. `HttpClient` có tên "MangaReaderLibApiClient" sẽ được sử dụng cho các client này, trỏ đến `MangaReaderApiSettings.BaseUrl`.
        *   Các service API của WebUI (ví dụ: `IMangaApiService`, `IChapterApiService`...) sẽ được giữ lại interface, nhưng phần implementation của chúng (ví dụ: `MangaReader.WebUI.Services.APIServices.Services.MangaApiService`) sẽ bị thay thế bằng các lớp mới hoặc logic mới chỉ gọi đến các client của `MangaReaderLib`. Một cách tiếp cận là giữ nguyên các interface này và tạo các implementation mới (ví dụ: `MangaReaderLibApiAdapterService` cho từng loại) hoặc điều chỉnh trực tiếp các service hiện có. Tuy nhiên, vì đã có các client chuyên dụng cho `MangaReaderLib` (như `IMangaReaderLibMangaClient`), các service tầng cao hơn (trong `MangaServices`) nên sử dụng trực tiếp các client này.
    *   **Services (`MangaReader_WebUI\Services\MangaServices\`):**
        *   Các service như `MangaDetailsService`, `MangaSearchService`, `ChapterService`, `MangaInfoService`, `FollowedMangaService`, `ReadingHistoryService` sẽ được cập nhật để sử dụng trực tiếp các client của `MangaReaderLib` (ví dụ: `IMangaReaderLibMangaClient`, `IMangaReaderLibChapterClient`...).
        *   Logic gọi API sẽ được đơn giản hóa, không còn cần kiểm tra nguồn dữ liệu.
        *   Sử dụng các mappers trong `MangaReader_WebUI\Services\MangaServices\DataProcessing\MangaReaderLibMappers\` để chuyển đổi DTO từ `MangaReaderLib` thành ViewModels.
        *   Cập nhật `MangaDataExtractorService`: Đơn giản hóa logic, loại bỏ `GetCurrentMangaSource()`. Hàm `ExtractCoverUrl` sẽ chỉ cần xử lý URL từ Cloudinary dựa trên `PublicId` từ `MangaReaderLib`.
    *   **Controllers:**
        *   Cập nhật DI trong constructor của các controller để inject các service đã được tái cấu trúc hoặc trực tiếp các client của `MangaReaderLib`.
        *   Logic trong các action method sẽ gọi đến các service/client này.
    *   **Models/ViewModels:**
        *   Đảm bảo rằng các ViewModels được tạo ra hoàn toàn từ dữ liệu của `MangaReaderLib API`.
    *   **`appsettings.json`:**
        *   `MangaReaderApiSettings.BaseUrl` sẽ là URL chính cho dữ liệu manga.
        *   `MangaReaderApiSettings.CloudinaryBaseUrl` sẽ được sử dụng bởi `MangaDataExtractorService` hoặc các mappers để tạo URL ảnh bìa.
        *   `BackendApi.BaseUrl` (`https://manga-reader-app-backend.onrender.com/api`) sẽ vẫn được sử dụng cho các dịch vụ xác thực người dùng (User Auth).

4.  **Kiểm Tra và Hoàn Thiện:**
    *   **Rà Soát Code:** Kiểm tra toàn bộ dự án `MangaReader_WebUI` để đảm bảo không còn sót lại tham chiếu nào đến các thành phần của MangaDex đã bị loại bỏ.
    *   **Cookie `MangaSource`:** Logic liên quan đến cookie `MangaSource` trong JavaScript (ví dụ `ui-toggles.js`) và C# (ví dụ `MangaDataExtractorService`, `HomeController`) cần được loại bỏ hoặc vô hiệu hóa. Nút chuyển đổi nguồn trong UI cũng sẽ bị loại bỏ.
    *   **Kiểm Thử:** Thực hiện kiểm thử toàn diện các chức năng của ứng dụng (hiển thị danh sách manga, chi tiết manga, đọc chapter, tìm kiếm, theo dõi, lịch sử, ...) để đảm bảo mọi thứ hoạt động chính xác với nguồn `MangaReaderLib`.
    *   **Tối Ưu Hóa:** Xem xét và tối ưu hóa code sau khi đã loại bỏ các phần không cần thiết.

5.  **Cập Nhật Tài Liệu:**
    *   **README Files:** Cập nhật các file `README.md` trong `MangaReader_WebUI` và các thư mục con để phản ánh cấu trúc mới và nguồn dữ liệu duy nhất.
    *   **Tài liệu cho `MangaDexComponents.Lib`:** Tạo file `README.md` cho project library mới, mô tả mục đích và cách sử dụng (nếu cần).