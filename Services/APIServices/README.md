# Services/APIServices

## Giới thiệu

Thư mục `Services/APIServices` chứa các lớp Service chịu trách nhiệm giao tiếp trực tiếp với **Backend API** của ứng dụng, đóng vai trò như một lớp proxy và trừu tượng hóa cho các lệnh gọi đến API MangaDex thực tế.

Mục tiêu chính của các service trong thư mục này là:

1.  **Gửi yêu cầu HTTP:** Thực hiện các lệnh gọi HTTP (GET, POST, PUT, DELETE) đến các endpoint tương ứng trên Backend API (ví dụ: `/api/mangadex/manga`, `/api/mangadex/chapter`,...).
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
*   **Kết nối qua Backend:** Tất cả các lệnh gọi đều đi qua Backend API (`/api/mangadex/...`), không gọi trực tiếp đến `api.mangadex.org`.

## Danh sách Services

Dưới đây là mô tả chi tiết cho từng service:

---

### 1. `IApiStatusService` / `ApiStatusService`

*   **Mục đích:** Kiểm tra tình trạng kết nối và khả năng hoạt động của Backend API proxy đến MangaDex.
*   **Phương thức chính:**
    *   `Task<bool> TestConnectionAsync()`
        *   **Input:** Không có.
        *   **Output:** `Task<bool>` - Trả về `true` nếu kết nối thành công (Backend API trả về mã trạng thái thành công cho endpoint `/status`), ngược lại trả về `false`.
        *   **Luồng xử lý:**
            1.  Gửi một yêu cầu HTTP GET đến endpoint `/status` của Backend API proxy.
            2.  Sử dụng một timeout ngắn (ví dụ: 5 giây) để tránh chờ đợi quá lâu.
            3.  Kiểm tra mã trạng thái HTTP của phản hồi.
            4.  Ghi log kết quả.
            5.  Xử lý các ngoại lệ phổ biến như `TaskCanceledException` (timeout), `HttpRequestException`.

---

### 2. `IMangaApiService` / `MangaApiService`

*   **Mục đích:** Xử lý tất cả các lệnh gọi API liên quan đến tài nguyên Manga.
*   **Phương thức chính:**
    *   `Task<MangaList?> FetchMangaAsync(int? limit, int? offset, SortManga? sortManga)`
        *   **Input:**
            *   `limit` (int?): Số lượng kết quả tối đa mỗi trang.
            *   `offset` (int?): Vị trí bắt đầu lấy dữ liệu.
            *   `sortManga` (SortManga?): Đối tượng chứa các tiêu chí lọc và sắp xếp (tên, trạng thái, tags, ngôn ngữ, thứ tự sắp xếp,...). Xem `Models/MangaDexModels.cs`.
        *   **Output:** `Task<MangaList?>` - Một đối tượng `MangaList` chứa danh sách các `Manga` cùng thông tin phân trang (`limit`, `offset`, `total`), hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng `Dictionary<string, List<string>>` chứa các query parameters từ `limit`, `offset` và `sortManga` (sử dụng `sortManga.ToParams()`).
            2.  **Quan trọng:** Tự động thêm các tham số `includes[]` cần thiết (ví dụ: `cover_art`, `author`, `artist`) để lấy kèm dữ liệu liên quan trong một lần gọi.
            3.  Gửi yêu cầu HTTP GET đến endpoint `/manga` của Backend API proxy với các tham số đã xây dựng.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaList`.
            6.  Ghi log và xử lý lỗi nếu có.
    *   `Task<MangaResponse?> FetchMangaDetailsAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của manga cần lấy chi tiết.
        *   **Output:** `Task<MangaResponse?>` - Một đối tượng `MangaResponse` chứa thông tin chi tiết của `Manga` trong thuộc tính `Data`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/manga/{mangaId}` của Backend API proxy.
            2.  Thêm các tham số `includes[]` cần thiết (ví dụ: `author`, `artist`, `cover_art`, `tag`) vào URL.
            3.  Gửi yêu cầu HTTP GET.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaResponse`.
            6.  Ghi log và xử lý lỗi nếu có.
    *   `Task<MangaList?> FetchMangaByIdsAsync(List<string> mangaIds)`
        *   **Input:** `mangaIds` (List<string>): Danh sách các ID manga cần lấy thông tin.
        *   **Output:** `Task<MangaList?>` - Một đối tượng `MangaList` chứa danh sách các `Manga` tương ứng với các ID đã cung cấp, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng `Dictionary<string, List<string>>` với tham số `ids[]` chứa tất cả các ID trong danh sách `mangaIds`.
            2.  Thêm các tham số `includes[]` cần thiết (ví dụ: `cover_art`).
            3.  Gửi yêu cầu HTTP GET đến endpoint `/manga` của Backend API proxy.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `MangaList`.
            6.  Ghi log và xử lý lỗi nếu có.

---

### 3. `IChapterApiService` / `ChapterApiService`

*   **Mục đích:** Xử lý tất cả các lệnh gọi API liên quan đến tài nguyên Chapter và MangaDex@Home.
*   **Phương thức chính:**
    *   `Task<ChapterList?> FetchChaptersAsync(string mangaId, string languages, string order, int? maxChapters)`
        *   **Input:**
            *   `mangaId` (string): ID của manga cần lấy chapter.
            *   `languages` (string): Chuỗi chứa các mã ngôn ngữ cần lọc, cách nhau bằng dấu phẩy (ví dụ: "vi,en").
            *   `order` (string): Thứ tự sắp xếp chapter (mặc định "desc").
            *   `maxChapters` (int?): Số lượng chapter tối đa cần lấy (nếu `null` sẽ lấy hết).
        *   **Output:** `Task<ChapterList?>` - Một đối tượng `ChapterList` chứa danh sách các `Chapter` đã lọc và sắp xếp, cùng thông tin phân trang tổng hợp, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  **Xử lý Pagination Nội bộ:** Do API `/feed` có giới hạn, phương thức này sẽ tự động thực hiện nhiều lệnh gọi API nếu cần thiết.
            2.  Bắt đầu với `offset = 0`, `limit = 100`.
            3.  Trong vòng lặp `do-while`:
                *   Xây dựng query parameters với `limit`, `offset`, `order`, `translatedLanguage[]` và các `includes[]` cần thiết (`scanlation_group`, `user`).
                *   Gửi yêu cầu HTTP GET đến endpoint `/manga/{mangaId}/feed` của Backend API proxy.
                *   Kiểm tra mã trạng thái, xử lý lỗi rate limit (429) nếu có bằng cách chờ và thử lại.
                *   Deserialize phản hồi thành `ChapterList`.
                *   Nếu thành công và có dữ liệu (`Data` không null và có phần tử):
                    *   Thêm các chapter vào danh sách tổng (`allChapters`).
                    *   Cập nhật `totalAvailable` từ phản hồi đầu tiên.
                    *   Tăng `offset` dựa trên số lượng chapter thực tế nhận được.
                    *   Nếu còn trang tiếp theo, thực hiện `Task.Delay` nhỏ để tránh rate limit.
                *   Nếu không có dữ liệu hoặc có lỗi, thoát vòng lặp.
            4.  Giới hạn số lượng chapter trả về nếu `maxChapters` được chỉ định.
            5.  Tạo và trả về một đối tượng `ChapterList` mới chứa toàn bộ `allChapters` đã lấy được và thông tin `totalAvailable`.
            6.  Ghi log và xử lý lỗi.
    *   `Task<ChapterResponse?> FetchChapterInfoAsync(string chapterId)`
        *   **Input:** `chapterId` (string): ID của chapter cần lấy thông tin.
        *   **Output:** `Task<ChapterResponse?>` - Một đối tượng `ChapterResponse` chứa thông tin chi tiết của `Chapter` trong thuộc tính `Data`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/chapter/{chapterId}` của Backend API proxy.
            2.  Thêm các tham số `includes[]` cần thiết (`scanlation_group`, `manga`, `user`).
            3.  Gửi yêu cầu HTTP GET.
            4.  Kiểm tra mã trạng thái phản hồi.
            5.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `ChapterResponse`.
            6.  Ghi log và xử lý lỗi nếu có.
    *   `Task<AtHomeServerResponse?> FetchChapterPagesAsync(string chapterId)`
        *   **Input:** `chapterId` (string): ID của chapter cần lấy thông tin server ảnh.
        *   **Output:** `Task<AtHomeServerResponse?>` - Một đối tượng `AtHomeServerResponse` chứa `baseUrl`, `hash` và danh sách tên file ảnh (`data`, `dataSaver`), hoặc `null` nếu có lỗi. **Lưu ý:** Service này *không* tự tạo URL ảnh đầy đủ.
        *   **Luồng xử lý:**
            1.  Xây dựng URL đến endpoint `/at-home/server/{chapterId}` của Backend API proxy.
            2.  Gửi yêu cầu HTTP GET.
            3.  Kiểm tra mã trạng thái phản hồi, xử lý rate limit (429).
            4.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `AtHomeServerResponse`.
            5.  Ghi log và xử lý lỗi nếu có.

---

### 4. `ICoverApiService` / `CoverApiService`

*   **Mục đích:** Xử lý các lệnh gọi API liên quan đến Cover Art, tối ưu cho các trường hợp sử dụng khác nhau.
*   **Phương thức chính:**
    *   `Task<CoverList?> GetAllCoversForMangaAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của manga cần lấy tất cả cover.
        *   **Output:** `Task<CoverList?>` - Một đối tượng `CoverList` chứa **tất cả** các `Cover` của manga đó, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  **Xử lý Pagination Nội bộ:** Tương tự `FetchChaptersAsync`, phương thức này gọi API `GET /cover` lặp lại với `limit=100` và tăng `offset` cho đến khi lấy hết các cover được báo cáo bởi `total`.
            2.  Sử dụng tham số `manga[]={mangaId}` để lọc.
            3.  Có thể thêm `order` (ví dụ: `order[volume]=asc`) để sắp xếp kết quả.
            4.  Tích lũy kết quả vào một danh sách `Cover`.
            5.  Thêm `Task.Delay` giữa các lần gọi để tránh rate limit.
            6.  Trả về đối tượng `CoverList` chứa toàn bộ danh sách đã lấy.
            7.  Ghi log và xử lý lỗi.
    *   `Task<Dictionary<string, string>?> FetchRepresentativeCoverUrlsAsync(List<string> mangaIds)`
        *   **Input:** `mangaIds` (List<string>): Danh sách các ID manga cần lấy ảnh bìa đại diện.
        *   **Output:** `Task<Dictionary<string, string>?>` - Một Dictionary map từ `MangaId` sang URL ảnh bìa thumbnail (`.512.jpg` qua proxy), hoặc `null` nếu có lỗi nghiêm trọng. Các manga không tìm thấy cover sẽ không có trong dictionary.
        *   **Luồng xử lý:**
            1.  **Tối ưu hóa:** Gọi API `GET /cover` một lần (hoặc chia thành các batch nếu `mangaIds` quá lớn > 100) với nhiều tham số `manga[]`.
            2.  **Sắp xếp ưu tiên:** Sử dụng `order[volume]=desc` và `order[createdAt]=desc` để API trả về cover của volume mới nhất trước tiên cho mỗi manga.
            3.  **Lọc kết quả:** Lặp qua danh sách `Cover` trả về. Với mỗi `Cover`, tìm `mangaId` từ `Relationships`. Nếu `mangaId` đó nằm trong danh sách yêu cầu và chưa có trong kết quả `resultCovers`, lấy `fileName`, tạo URL thumbnail qua proxy (`/mangadex/proxy-image`) và thêm vào `resultCovers`. Dừng lại khi đã tìm đủ cover cho tất cả manga trong batch.
            4.  Xử lý rate limit (429).
            5.  Ghi log và xử lý lỗi.
    *   `Task<string> FetchCoverUrlAsync(string mangaId)`
        *   **Input:** `mangaId` (string): ID của một manga duy nhất.
        *   **Output:** `Task<string>` - URL ảnh bìa thumbnail (`.512.jpg` qua proxy) hoặc chuỗi rỗng nếu không tìm thấy/lỗi.
        *   **Luồng xử lý:**
            1.  Gọi `FetchRepresentativeCoverUrlsAsync` với danh sách chỉ chứa `mangaId` này.
            2.  Lấy giá trị từ dictionary trả về.
            3.  Trả về URL hoặc chuỗi rỗng.

---

### 5. `ITagApiService` / `TagApiService`

*   **Mục đích:** Lấy danh sách tất cả các Tag có sẵn trên MangaDex.
*   **Phương thức chính:**
    *   `Task<TagListResponse?> FetchTagsAsync()`
        *   **Input:** Không có.
        *   **Output:** `Task<TagListResponse?>` - Một đối tượng `TagListResponse` chứa danh sách tất cả các `Tag`, hoặc `null` nếu có lỗi.
        *   **Luồng xử lý:**
            1.  Gửi yêu cầu HTTP GET đến endpoint `/manga/tag` của Backend API proxy.
            2.  Kiểm tra mã trạng thái phản hồi.
            3.  Nếu thành công, deserialize luồng phản hồi JSON thành đối tượng `TagListResponse`.
            4.  Ghi log và xử lý lỗi nếu có.

---

## Các Thành phần Chung

*   **`BaseApiService`:** Lớp cơ sở trừu tượng chứa các thành phần dùng chung:
    *   `HttpClient`: Instance được inject để thực hiện các lệnh gọi HTTP.
    *   `ILogger`: Instance để ghi log.
    *   `BaseUrl`: URL gốc của Backend API proxy (ví dụ: `https://.../api/mangadex`).
    *   `JsonSerializerOptions`: Cấu hình cho `System.Text.Json` (ví dụ: `PropertyNameCaseInsensitive = true`).
    *   `BuildUrlWithParams()`: Phương thức tiện ích để xây dựng URL với các query parameters.
    *   `AddOrUpdateParam()`: Phương thức tiện ích để thêm tham số vào dictionary.
    *   `LogApiError()`: Phương thức tiện ích để ghi log lỗi API chi tiết.
*   **`Models/Mangadex/`:** Thư mục chứa các lớp Model C# đại diện cho cấu trúc dữ liệu JSON trả về từ API MangaDex (ví dụ: `Manga`, `Chapter`, `Cover`, `Tag`, `MangaList`, `ChapterResponse`, `BaseListResponse<T>`, `BaseEntityResponse<T>`,...). Các service API sử dụng các model này làm kiểu dữ liệu trả về sau khi deserialize JSON.

## Cách sử dụng

Các service trong thư mục này được đăng ký trong `Program.cs` sử dụng Dependency Injection (thường là `AddScoped`). Chúng sau đó được inject vào các service cấp cao hơn trong thư mục `Services/MangaServices` hoặc (ít phổ biến hơn) trực tiếp vào các Controller.

Ví dụ: `MangaSearchService` sẽ inject `IMangaApiService` và `ICoverApiService` để lấy dữ liệu manga và ảnh bìa.