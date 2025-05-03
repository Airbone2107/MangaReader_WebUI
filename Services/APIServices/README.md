# Services/APIServices

## Giới thiệu

Thư mục `Services/APIServices` chứa các lớp Service chịu trách nhiệm giao tiếp trực tiếp với **Backend API** của ứng dụng, đóng vai trò như một lớp proxy và trừu tượng hóa cho các lệnh gọi đến API MangaDex thực tế.

Mục tiêu chính của các service trong thư mục này là:

1.  **Gửi yêu cầu HTTP:** Thực hiện các lệnh gọi HTTP (GET, POST, PUT, DELETE) đến các endpoint tương ứng trên Backend API (ví dụ: `/api/mangadex/manga`, `/api/mangadex/chapter`,...). **Lưu ý:** Tất cả các lệnh gọi đều đi qua Backend API proxy, không gọi trực tiếp đến `api.mangadex.org`.
2.  **Xử lý tham số:** Xây dựng URL và các tham số truy vấn (query parameters) cần thiết dựa trên yêu cầu từ các service cấp cao hơn.
3.  **Deserialize Phản hồi:** Nhận phản hồi JSON từ Backend API và sử dụng `System.Text.Json` để deserialize dữ liệu đó thành các **Model C# mạnh mẽ về kiểu** được định nghĩa trong thư mục `Models/Mangadex/`.
4.  **Xử lý lỗi cơ bản:** Kiểm tra mã trạng thái HTTP, ghi log lỗi và trả về `null` hoặc đối tượng lỗi (nếu có) khi API gọi không thành công.
5.  **Xử lý giới hạn API:** Đối với các endpoint trả về danh sách (list), các service này có thể bao gồm logic để xử lý phân trang (pagination) tự động, đảm bảo lấy được toàn bộ dữ liệu cần thiết ngay cả khi API gốc có giới hạn số lượng kết quả mỗi lần gọi.

Việc tách lớp giao tiếp API này giúp giữ cho logic gọi API tập trung, dễ quản lý, dễ thay đổi (ví dụ: thay đổi URL backend, thêm header xác thực) và tách biệt khỏi logic nghiệp vụ xử lý dữ liệu ở các service cấp cao hơn (`Services/MangaServices`). Các service này đều kế thừa từ `BaseApiService` để tái sử dụng các chức năng chung.

## Các Nguyên tắc Cốt lõi

*   **Tách biệt Trách nhiệm (Separation of Concerns):** Mỗi service tập trung vào một nhóm tài nguyên cụ thể của API (Manga, Chapter, Cover, Tag, Status).
*   **An toàn Kiểu (Type Safety):** Sử dụng các Model C# (`Models/Mangadex/`) thay vì `dynamic` để đảm bảo kiểu dữ liệu rõ ràng, giảm lỗi runtime và cải thiện trải nghiệm phát triển (IntelliSense).
*   **Trừu tượng hóa (Abstraction):** Mỗi service implement một Interface (ví dụ: `IMangaApiService`), giúp dễ dàng thay thế hoặc mock trong quá trình kiểm thử.
*   **Xử lý Lỗi:** Cung cấp cơ chế ghi log lỗi chi tiết và trả về giá trị `null` để báo hiệu lỗi cho lớp gọi.
*   **Kết nối qua Backend:** Tất cả các lệnh gọi đều đi qua Backend API proxy (`/api/mangadex/...`).

## Danh sách Services

Dưới đây là mô tả chi tiết cho từng service:

---

### 1. `IApiStatusService` / `ApiStatusService`

*   **Mục đích:** Kiểm tra tình trạng kết nối và khả năng hoạt động của Backend API proxy đến MangaDex.
*   **Phương thức chính:**
    *   `Task<bool> TestConnectionAsync()`
        *   **Input:** Không có.
        *   **Output:** `Task<bool>` - Trả về `true` nếu kết nối thành công (Backend API trả về mã trạng thái thành công cho endpoint `/api/mangadex/status`), ngược lại trả về `false`.
        *   **Luồng xử lý:**
            1.  Gửi một yêu cầu HTTP GET đến endpoint `/api/mangadex/status` của Backend API proxy.
            2.  Sử dụng một timeout ngắn (ví dụ: 5 giây) để tránh chờ đợi quá lâu.
            3.  Kiểm tra mã trạng thái HTTP của phản hồi.
            4.  Ghi log kết quả.
            5.  Xử lý các ngoại lệ phổ biến như `TaskCanceledException` (timeout), `HttpRequestException`.

---

### 2. `IMangaApiService` / `MangaApiService`

*   **Mục đích:** Xử lý tất cả các lệnh gọi API liên quan đến tài nguyên Manga thông qua Backend API proxy.
*   **Phương thức chính:**
    *   `Task<MangaList?> FetchMangaAsync(int? limit, int? offset, SortManga? sortManga)`
        *   **Input:**
            *   `limit` (int?): Số lượng kết quả tối đa mỗi trang.
            *   `offset` (int?): Vị trí bắt đầu lấy dữ liệu.
            *   `sortManga` (SortManga?): Đối tượng chứa các tiêu chí lọc và sắp xếp.
        *   **Output:** `Task<MangaList?>` - Một đối tượng `MangaList` chứa danh sách các `Manga` cùng thông tin phân trang, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng `Dictionary<string, List<string>>` chứa các query parameters từ `limit`, `offset` và `sortManga`.
            2.  Tự động thêm các tham số `includes[]` cần thiết (ví dụ: `cover_art`, `author`, `artist`).
            3.  Gửi yêu cầu HTTP GET đến endpoint `/api/mangadex/manga` của Backend API proxy với các tham số đã xây dựng.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaList`.
            6.  Ghi log và xử lý lỗi nếu có.
    *   `Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của manga cần lấy chi tiết.
        *   **Output:** `Task<MangaResponse?>` - Một đối tượng `MangaResponse` chứa thông tin chi tiết của `Manga`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/api/mangadex/manga/{mangaId}` của Backend API proxy.
            2.  Thêm các tham số `includes[]` cần thiết (ví dụ: `author`, `artist`, `cover_art`, `tag`) vào URL.
            3.  Gửi yêu cầu HTTP GET.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaResponse`.
            6.  Ghi log và xử lý lỗi nếu có.
    *   `Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)`
        *   **Input:** `mangaIds` (List<string>): Danh sách các ID manga cần lấy thông tin.
        *   **Output:** `Task<MangaList?>` - Một đối tượng `MangaList` chứa danh sách các `Manga` tương ứng, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng `Dictionary<string, List<string>>` với tham số `ids[]`.
            2.  Thêm các tham số `includes[]` cần thiết (ví dụ: `cover_art`).
            3.  Gửi yêu cầu HTTP GET đến endpoint `/api/mangadex/manga` của Backend API proxy.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaList`.
            6.  Ghi log và xử lý lỗi nếu có.

---

### 3. `IChapterApiService` / `ChapterApiService`

*   **Mục đích:** Xử lý tất cả các lệnh gọi API liên quan đến tài nguyên Chapter và MangaDex@Home thông qua Backend API proxy.
*   **Phương thức chính:**
    *   `Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order, int? maxChapters)`
        *   **Input:**
            *   `mangaId` (string): ID của manga.
            *   `languages` (string): Chuỗi mã ngôn ngữ (vd: "vi,en").
            *   `order` (string): Thứ tự sắp xếp.
            *   `maxChapters` (int?): Số lượng tối đa cần lấy.
        *   **Output:** `Task<ChapterList?>` - Một đối tượng `ChapterList` chứa danh sách các `Chapter`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  **Xử lý Pagination Nội bộ:** Tự động gọi API nhiều lần nếu cần.
            2.  Gửi yêu cầu HTTP GET đến endpoint `/api/mangadex/manga/{mangaId}/feed` của Backend API proxy với các tham số `limit`, `offset`, `order`, `translatedLanguage[]`, `includes[]`.
            3.  Xử lý lỗi rate limit (429).
            4.  Tích lũy kết quả và trả về `ChapterList` tổng hợp.
    *   `Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)`
        *   **Input:** `chapterId` (string): ID của chapter.
        *   **Output:** `Task<ChapterResponse?>` - Một đối tượng `ChapterResponse` chứa thông tin chi tiết của `Chapter`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/api/mangadex/chapter/{chapterId}` của Backend API proxy.
            2.  Thêm các tham số `includes[]` (`scanlation_group`, `manga`, `user`).
            3.  Gửi yêu cầu HTTP GET.
            4.  Kiểm tra mã trạng thái và deserialize.
    *   `Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)`
        *   **Input:** `chapterId` (string): ID của chapter.
        *   **Output:** `Task<AtHomeServerResponse?>` - Một đối tượng `AtHomeServerResponse` chứa thông tin server ảnh, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/api/mangadex/at-home/server/{chapterId}` của Backend API proxy.
            2.  Gửi yêu cầu HTTP GET.
            3.  Kiểm tra mã trạng thái và deserialize.

---

### 4. `ICoverApiService` / `CoverApiService`

*   **Mục đích:** Xử lý các lệnh gọi API liên quan đến Cover Art thông qua Backend API proxy.
*   **Phương thức chính:**
    *   `Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của manga.
        *   **Output:** `Task<CoverList?>` - Một đối tượng `CoverList` chứa **tất cả** các `Cover` của manga, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  **Xử lý Pagination Nội bộ:** Gọi API `GET /api/mangadex/cover` lặp lại với `limit=100`, `offset` tăng dần và tham số `manga[]={mangaId}`.
            2.  Tích lũy kết quả và trả về `CoverList` tổng hợp.
    *   `string GetProxiedCoverUrl(string mangaId, string fileName, int size = 512)`
        *   **Input:**
            *   `mangaId` (string): ID của manga.
            *   `fileName` (string): Tên file của ảnh bìa.
            *   `size` (int): Kích thước ảnh mong muốn (mặc định 512).
        *   **Output:** `string` - URL của ảnh bìa đã được proxy bởi Backend API.
        *   **Luồng xử lý:** Tạo URL đến MangaDex uploads và bọc nó trong URL proxy của Backend API (`/mangadex/proxy-image?url=...`).

---

### 5. `ITagApiService` / `TagApiService`

*   **Mục đích:** Lấy danh sách tất cả các Tag có sẵn trên MangaDex thông qua Backend API proxy.
*   **Phương thức chính:**
    *   `Task<TagListResponse?> FetchTagsAsync()`
        *   **Input:** Không có.
        *   **Output:** `Task<TagListResponse?>` - Một đối tượng `TagListResponse` chứa danh sách tất cả các `Tag`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Gửi yêu cầu HTTP GET đến endpoint `/api/mangadex/manga/tag` của Backend API proxy.
            2.  Kiểm tra mã trạng thái và deserialize.

---

## Các Thành phần Chung

*   **`BaseApiService`:** Lớp cơ sở trừu tượng chứa các thành phần dùng chung:
    *   `HttpClient`: Instance được inject.
    *   `ILogger`: Instance để ghi log.
    *   `BaseUrl`: URL gốc của Backend API proxy (ví dụ: `https://.../api/mangadex`).
    *   `JsonSerializerOptions`: Cấu hình cho `System.Text.Json`.
    *   Các phương thức tiện ích: `BuildUrlWithParams()`, `AddOrUpdateParam()`, `LogApiError()`.
*   **`Models/Mangadex/`:** Thư mục chứa các lớp Model C# đại diện cho cấu trúc dữ liệu JSON trả về từ API MangaDex.

## Cách sử dụng

Các service trong thư mục này được đăng ký trong `Program.cs` sử dụng Dependency Injection (thường là `AddScoped`). Chúng sau đó được inject vào các service cấp cao hơn trong thư mục `Services/MangaServices` hoặc (ít phổ biến hơn) trực tiếp vào các Controller.